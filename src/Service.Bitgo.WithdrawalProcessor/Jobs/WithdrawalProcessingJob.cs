using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.Service.Tools;
using Newtonsoft.Json;
using Service.BalanceHistory.Domain.Models;
using Service.BalanceHistory.Grpc.Models;
using Service.Bitgo.WithdrawalProcessor.Domain.Models;
using Service.Bitgo.WithdrawalProcessor.Postgres;
using Service.Bitgo.WithdrawalProcessor.Services;
using Service.VerificationCodes.Grpc;
using Service.VerificationCodes.Grpc.Models;

// ReSharper disable InconsistentLogPropertyNaming

namespace Service.Bitgo.WithdrawalProcessor.Jobs
{
    public class WithdrawalProcessingJob : IDisposable
    {
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;
        private readonly CryptoWithdrawalService _cryptoWithdrawalService;
        private readonly ILogger<WithdrawalProcessingJob> _logger;
        private readonly IWithdrawalVerificationService _verificationService;
        private readonly IPublisher<Withdrawal> _withdrawalPublisher;
        private readonly MyTaskTimer _timer;

        public WithdrawalProcessingJob(ILogger<WithdrawalProcessingJob> logger,
            DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder,
            CryptoWithdrawalService cryptoWithdrawalService, 
            ISubscriber<WithdrawalVerifiedMessage> verificationSubscriber, 
            IWithdrawalVerificationService verificationService, 
            IPublisher<Withdrawal> withdrawalPublisher)
        {
            verificationSubscriber.Subscribe(HandleVerifiedWithdrawals);
            _logger = logger;
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
            _cryptoWithdrawalService = cryptoWithdrawalService;
            _verificationService = verificationService;
            _withdrawalPublisher = withdrawalPublisher;
            _timer = new MyTaskTimer(typeof(WithdrawalProcessingJob),
                TimeSpan.FromSeconds(Program.ReloadedSettings(e => e.WithdrawalsProcessingIntervalSec).Invoke()),
                logger, DoTime);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private async Task DoTime()
        {
            await HandleNewWithdrawals();
            await HandleExpiredWithdrawals();
            await HandlePendingWithdrawals();
        }

        private async ValueTask HandleVerifiedWithdrawals(WithdrawalVerifiedMessage message)
        {
            try
            {
                await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

                var sw = new Stopwatch();
                sw.Start();

                var withdrawals = await context.Withdrawals
                    .Where(e => e.Id == long.Parse(message.WithdrawalProcessId))
                    .Where(e => e.Status == WithdrawalStatus.ApprovalPending)
                    .ToListAsync();

                foreach (var withdrawal in withdrawals)
                {
                    try
                    {
                        withdrawal.Status = WithdrawalStatus.Pending;
                        await _withdrawalPublisher.PublishAsync(new Withdrawal(withdrawal));
                        _logger.LogInformation("Withdrawal with Operation ID {operationId} is changed to status {status}. Operation: {operationJson}", withdrawal.TransactionId, withdrawal.Status, JsonConvert.SerializeObject(withdrawal));
                    }
                    catch (Exception ex)
                    {
                        withdrawal.Status = WithdrawalStatus.Error;
                        withdrawal.LastError = ex.Message.Length > 2048 ? ex.Message.Substring(0, 2048) : ex.Message;
                        withdrawal.RetriesCount++;
                    }
                }

                await context.UpdateAsync(withdrawals);

                withdrawals.Count.AddToActivityAsTag("withdrawals-count");

                sw.Stop();
                if (withdrawals.Count > 0)
                    _logger.LogInformation("Handled {countTrade} approved withdrawals. Time: {timeRangeText}", withdrawals.Count,
                        sw.Elapsed.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot Handle approved withdrawals");
                ex.FailActivity();

                throw;
            }

            _timer.ChangeInterval(
                TimeSpan.FromSeconds(Program.ReloadedSettings(e => e.WithdrawalsProcessingIntervalSec).Invoke()));
        }
        private async Task HandleNewWithdrawals(){
        using var activity = MyTelemetry.StartActivity("Handle new withdrawals");
            try
            {
                await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

                var sw = new Stopwatch();
                sw.Start();

                var withdrawals = await context.Withdrawals.Where(e =>
                    e.Status == WithdrawalStatus.New).ToListAsync();

                var whitelist = Program.ReloadedSettings(e => e.WhitelistedAddresses).Invoke().Split(';').ToList();
                
                foreach (var withdrawal in withdrawals)
                {
                    try
                    {
                        if (!whitelist.Contains(withdrawal.ToAddress))
                        {
                            var response = await _verificationService.SendWithdrawalVerificationCodeAsync(
                                new SendWithdrawalVerificationCodeRequest
                                {
                                    ClientId = withdrawal.ClientId,
                                    OperationId = withdrawal.Id.ToString(),
                                    Lang = withdrawal.ClientLang,
                                    AssetSymbol = withdrawal.AssetSymbol,
                                    Amount = withdrawal.Amount.ToString(CultureInfo.InvariantCulture),
                                    DestinationAddress = withdrawal.ToAddress,
                                    IpAddress = withdrawal.ClientIp,
                                });
                            if (response.IsSuccess)
                            {
                                withdrawal.Status = WithdrawalStatus.ApprovalPending;
                                withdrawal.NotificationTime = DateTime.UtcNow;
                            }
                        }
                        else
                        {
                            withdrawal.Status = WithdrawalStatus.Pending;
                        }
                        
                        await _withdrawalPublisher.PublishAsync(new Withdrawal(withdrawal));
                        _logger.LogInformation("Withdrawal with Operation ID {operationId} is changed to status {status}. Operation: {operationJson}", withdrawal.TransactionId, withdrawal.Status, JsonConvert.SerializeObject(withdrawal));
                    }
                    catch (Exception ex)
                    {
                        withdrawal.Status = WithdrawalStatus.Error;
                        withdrawal.LastError = ex.Message.Length > 2048 ? ex.Message.Substring(0, 2048) : ex.Message;
                        withdrawal.RetriesCount++;
                    }
                }

                await context.UpdateAsync(withdrawals);

                withdrawals.Count.AddToActivityAsTag("withdrawals-count");

                sw.Stop();
                if (withdrawals.Count > 0)
                    _logger.LogInformation("Handled {countTrade} new withdrawals. Time: {timeRangeText}", withdrawals.Count,
                        sw.Elapsed.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot Handle new withdrawals");
                ex.FailActivity();

                throw;
            }

            _timer.ChangeInterval(
                TimeSpan.FromSeconds(Program.ReloadedSettings(e => e.WithdrawalsProcessingIntervalSec).Invoke()));
        }
        
        private async Task HandleExpiredWithdrawals(){
        using var activity = MyTelemetry.StartActivity("Handle expired withdrawals");
            try
            {
                await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

                var sw = new Stopwatch();
                sw.Start();

                var withdrawals = await context.Withdrawals.Where(e =>
                    e.Status == WithdrawalStatus.ApprovalPending).ToListAsync();

                foreach (var withdrawal in withdrawals)
                {
                    try
                    {
                        if (DateTime.UtcNow - withdrawal.NotificationTime >=
                            TimeSpan.FromMinutes(Program.Settings.WithdrawalExpirationTimeInMin))
                        {
                            withdrawal.Status = WithdrawalStatus.Cancelled;
                            await _withdrawalPublisher.PublishAsync(new Withdrawal(withdrawal));
                            _logger.LogInformation("Withdrawal with Operation ID {operationId} is changed to status {status}. Operation: {operationJson}", withdrawal.TransactionId, withdrawal.Status, JsonConvert.SerializeObject(withdrawal));
                        }
                    }
                    catch (Exception ex)
                    {
                        withdrawal.Status = WithdrawalStatus.Error;
                        withdrawal.LastError = ex.Message.Length > 2048 ? ex.Message.Substring(0, 2048) : ex.Message;
                        withdrawal.RetriesCount++;
                    }
                }

                await context.UpdateAsync(withdrawals);

                withdrawals.Count.AddToActivityAsTag("withdrawals-count");

                sw.Stop();
                if (withdrawals.Count > 0)
                    _logger.LogInformation("Handled {countTrade} expired withdrawals. Time: {timeRangeText}", withdrawals.Count,
                        sw.Elapsed.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot Handle expired withdrawals");
                ex.FailActivity();

                throw;
            }

            _timer.ChangeInterval(
                TimeSpan.FromSeconds(Program.ReloadedSettings(e => e.WithdrawalsProcessingIntervalSec).Invoke()));
        }

        private async Task HandlePendingWithdrawals()
        {
            using var activity = MyTelemetry.StartActivity("Handle pending withdrawals");
            try
            {
                await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

                var sw = new Stopwatch();
                sw.Start();

                var withdrawals = await context.Withdrawals.Where(e =>
                    e.Status == WithdrawalStatus.Pending || e.Status == WithdrawalStatus.ErrorInMe ||
                    e.Status == WithdrawalStatus.Error).ToListAsync();

                foreach (var withdrawal in withdrawals)
                {
                    try
                    {
                        await _cryptoWithdrawalService.ExecuteWithdrawalAsync(withdrawal);
                        if (withdrawal.Status != WithdrawalStatus.Success)
                        {
                            if (withdrawal.RetriesCount >=
                                Program.ReloadedSettings(e => e.WithdrawalsRetriesLimit).Invoke())
                            {
                                withdrawal.Status = WithdrawalStatus.Stopped;
                            }
                        }
                        await _withdrawalPublisher.PublishAsync(new Withdrawal(withdrawal));
                        _logger.LogInformation("Withdrawal with Operation ID {operationId} is changed to status {status}. Operation: {operationJson}", withdrawal.TransactionId, withdrawal.Status, JsonConvert.SerializeObject(withdrawal));
                    }
                    catch (Exception ex)
                    {
                        withdrawal.Status = WithdrawalStatus.Error;
                        withdrawal.LastError = ex.Message.Length > 2048 ? ex.Message.Substring(0, 2048) : ex.Message;
                        withdrawal.RetriesCount++;
                    }
                }

                await context.UpdateAsync(withdrawals);

                withdrawals.Count.AddToActivityAsTag("withdrawals-count");

                sw.Stop();
                if (withdrawals.Count > 0)
                    _logger.LogInformation("Handled {countTrade} pending withdrawals. Time: {timeRangeText}", withdrawals.Count,
                        sw.Elapsed.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot Handle pending withdrawals");
                ex.FailActivity();

                throw;
            }

            _timer.ChangeInterval(
                TimeSpan.FromSeconds(Program.ReloadedSettings(e => e.WithdrawalsProcessingIntervalSec).Invoke()));
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }
    }
}
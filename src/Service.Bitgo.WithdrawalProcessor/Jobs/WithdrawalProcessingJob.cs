using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.Service.Tools;
using Service.Bitgo.WithdrawalProcessor.Domain.Models;
using Service.Bitgo.WithdrawalProcessor.Postgres;
using Service.Bitgo.WithdrawalProcessor.Services;

// ReSharper disable InconsistentLogPropertyNaming

namespace Service.Bitgo.WithdrawalProcessor.Jobs
{
    public class WithdrawalProcessingJob : IDisposable
    {
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;
        private readonly CryptoWithdrawalService _cryptoWithdrawalService;
        private readonly ILogger<WithdrawalProcessingJob> _logger;

        private readonly MyTaskTimer _timer;

        public WithdrawalProcessingJob(ILogger<WithdrawalProcessingJob> logger,
            DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder,
            CryptoWithdrawalService cryptoWithdrawalService)
        {
            _logger = logger;
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
            _cryptoWithdrawalService = cryptoWithdrawalService;
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
            using var activity = MyTelemetry.StartActivity("Handle withdrawals");
            try
            {
                await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

                var sw = new Stopwatch();
                sw.Start();

                var withdrawals = await context.Withdrawals.Where(e =>
                    e.Status == WithdrawalStatus.New || e.Status == WithdrawalStatus.ErrorInMe ||
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
                    }
                    catch (Exception ex)
                    {
                        withdrawal.Status = WithdrawalStatus.Error;
                        withdrawal.LastError = ex.Message;
                        withdrawal.RetriesCount++;
                    }
                }

                await context.UpdateAsync(withdrawals);

                withdrawals.Count.AddToActivityAsTag("withdrawals-count");

                sw.Stop();
                if (withdrawals.Count > 0)
                    _logger.LogInformation("Handled {countTrade} withdrawals. Time: {timeRangeText}", withdrawals.Count,
                        sw.Elapsed.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot Handle withdrawals");
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
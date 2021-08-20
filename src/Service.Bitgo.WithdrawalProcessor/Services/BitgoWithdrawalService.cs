using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Newtonsoft.Json;
using Service.Bitgo.WithdrawalProcessor.Domain.Models;
using Service.Bitgo.WithdrawalProcessor.Grpc;
using Service.Bitgo.WithdrawalProcessor.Grpc.Models;
using Service.Bitgo.WithdrawalProcessor.Postgres;
using Service.Bitgo.WithdrawalProcessor.Postgres.Models;
using Service.ChangeBalanceGateway.Grpc;
using Service.ChangeBalanceGateway.Grpc.Models;

// ReSharper disable InconsistentLogPropertyNaming

namespace Service.Bitgo.WithdrawalProcessor.Services
{
    public class BitgoWithdrawalService : IBitgoWithdrawalService
    {
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;
        private readonly CryptoWithdrawalService _cryptoWithdrawalService;
        private readonly ILogger<BitgoWithdrawalService> _logger;

        public BitgoWithdrawalService(ILogger<BitgoWithdrawalService> logger,
            DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder,
            CryptoWithdrawalService cryptoWithdrawalService)
        {
            _logger = logger;
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
            _cryptoWithdrawalService = cryptoWithdrawalService;
        }

        
        public async Task<GetWithdrawalResponse> GetWithdrawalById(GetWithdrawalRequest request)
        {
            request.AddToActivityAsJsonTag("request-data");
            _logger.LogInformation("Receive GetWithdrawal request: {JsonRequest}", JsonConvert.SerializeObject(request));
            
            try
            {
                await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);
                var withdrawal = await context.Withdrawals
                    .Where(e => e.Id == request.Id)
                    .FirstOrDefaultAsync();

                if (withdrawal == null)
                {
                    return new GetWithdrawalResponse()
                    {
                        Success = false,
                        ErrorMessage = $"Withdrawal with Id {request.Id} was not found"
                    };
                }

                var response = new GetWithdrawalResponse
                {
                    Success = true,
                    Withdrawal = new Withdrawal(withdrawal),
                };

                _logger.LogInformation("Return GetWithdrawal response for Id: {id}",
                   withdrawal.Id);
                return response;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception,
                    "Cannot get GetWithdrawal Id: {Id}",
                     request.Id);
                return new GetWithdrawalResponse {Success = false, ErrorMessage = exception.Message};
            }
        }

        
        public async Task<GetWithdrawalsResponse> GetWithdrawals(GetWithdrawalsRequest request)
        {
            request.AddToActivityAsJsonTag("request-data");
            _logger.LogInformation("Receive GetWithdrawals request: {JsonRequest}", JsonConvert.SerializeObject(request));

            if (request.BatchSize % 2 != 0)
                return new GetWithdrawalsResponse
                {
                    Success = false,
                    ErrorMessage = "Butch size must be even"
                };

            try
            {
                await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);
                var withdrawals = await context.Withdrawals
                    .Where(e => e.Id > request.LastId)
                    .OrderByDescending(e => e.Id)
                    .Take(request.BatchSize)
                    .ToListAsync();

                var response = new GetWithdrawalsResponse
                {
                    Success = true,
                    WithdrawalCollection = withdrawals.Select(e => new Withdrawal(e)).ToList(),
                    IdForNextQuery = withdrawals.Count > 0 ? withdrawals.Select(e => e.Id).Max() : 0
                };

                response.WithdrawalCollection.Count.AddToActivityAsTag("response-count-items");
                _logger.LogInformation("Return GetWithdrawals response count items: {count}",
                    response.WithdrawalCollection.Count);
                return response;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception,
                    "Cannot get GetWithdrawals take: {takeValue}, LastId: {LastId}",
                    request.BatchSize, request.LastId);
                return new GetWithdrawalsResponse {Success = false, ErrorMessage = exception.Message};
            }
        }

        public async Task<RetryWithdrawalResponse> RetryWithdrawal(RetryWithdrawalRequest request)
        {
            using var activity = MyTelemetry.StartActivity("Handle withdrawal manual retry")
                .AddTag("WithdrawalId", request.WithdrawalId);
            _logger.LogInformation("Handle withdrawal manual retry: {withdrawalId}", request.WithdrawalId);
            try
            {
                await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

                var withdrawal = await context.Withdrawals.FindAsync(request.WithdrawalId);

                if (withdrawal == null)
                {
                    _logger.LogInformation("Unable to find withdrawal with id {withdrawalId}", request.WithdrawalId);
                    return new RetryWithdrawalResponse
                    {
                        Success = false,
                        ErrorMessage = "Unable to find withdrawal",
                        WithdrawalId = request.WithdrawalId
                    };
                }

                if (withdrawal.Status == WithdrawalStatus.Success)
                {
                    _logger.LogInformation("Withdrawal {withdrawalId} already processed", request.WithdrawalId);
                    return new RetryWithdrawalResponse
                    {
                        Success = true,
                        WithdrawalId = request.WithdrawalId
                    };
                }

                try
                {
                    await _cryptoWithdrawalService.ExecuteWithdrawalAsync(withdrawal);
                }
                catch (Exception ex)
                {
                    withdrawal.Status = WithdrawalStatus.Error;
                    withdrawal.LastError = ex.Message;
                }

                await context.UpdateAsync(withdrawal);

                _logger.LogInformation("Handled withdrawal manual retry: {withdrawalId}", request.WithdrawalId);
                return new RetryWithdrawalResponse
                {
                    Success = true,
                    WithdrawalId = request.WithdrawalId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot Handle withdrawals");
                ex.FailActivity();

                return new RetryWithdrawalResponse
                {
                    Success = false,
                    ErrorMessage = $"Internal error {ex.Message}",
                    WithdrawalId = request.WithdrawalId
                };
            }
        }

        public async Task<CancelWithdrawalResponse> CancelWithdrawal(CancelWithdrawalRequest request)
        {
            using var activity = MyTelemetry.StartActivity("Handle withdrawal manual cancel")
                .AddTag("WithdrawalId", request.WithdrawalId);
            _logger.LogInformation("Handle withdrawal manual cancel: {withdrawalId}", request.WithdrawalId);
            try
            {
                await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

                var withdrawal = await context.Withdrawals.FindAsync(request.WithdrawalId);

                if (withdrawal == null)
                {
                    _logger.LogInformation("Unable to find withdrawal with id {withdrawalId}", request.WithdrawalId);
                    return new CancelWithdrawalResponse
                    {
                        Success = false,
                        ErrorMessage = "Unable to find withdrawal",
                        WithdrawalId = request.WithdrawalId
                    };
                }

                if (withdrawal.Status != WithdrawalStatus.New 
                    && withdrawal.Status != WithdrawalStatus.ApprovalPending
                    && withdrawal.Status != WithdrawalStatus.Pending)
                {
                    _logger.LogInformation("Incorrect status {status} for {withdrawalId}", withdrawal.Status,
                        request.WithdrawalId);
                    return new CancelWithdrawalResponse
                    {
                        Success = false,
                        ErrorMessage = $"Wrong status withdrawal {withdrawal.Status}",
                        WithdrawalId = request.WithdrawalId
                    };
                }

                withdrawal.Status = WithdrawalStatus.Cancelled;

                await context.UpdateAsync(new List<WithdrawalEntity> {withdrawal});

                _logger.LogInformation("Handled withdrawal manual cancel: {withdrawalId}", request.WithdrawalId);
                return new CancelWithdrawalResponse
                {
                    Success = true,
                    WithdrawalId = request.WithdrawalId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot Handle withdrawals");
                ex.FailActivity();

                return new CancelWithdrawalResponse
                {
                    Success = false,
                    ErrorMessage = $"Internal error {ex.Message}",
                    WithdrawalId = request.WithdrawalId
                };
            }
        }
    }
}
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyJetWallet.BitGo;
using MyJetWallet.BitGo.Settings.Services;
using MyJetWallet.Domain.Transactions;
using MyJetWallet.Sdk.Service;
using Newtonsoft.Json;
using OpenTelemetry.Trace;
using Service.BalanceHistory.Domain.Models;
using Service.BitGo.SignTransaction.Grpc;
using Service.BitGo.SignTransaction.Grpc.Models;
using Service.Bitgo.WithdrawalProcessor.Domain.Models;
using Service.Bitgo.WithdrawalProcessor.Grpc;
using Service.Bitgo.WithdrawalProcessor.Grpc.Models;
using Service.Bitgo.WithdrawalProcessor.Postgres;
using Service.Bitgo.WithdrawalProcessor.Postgres.Models;
using Service.ChangeBalanceGateway.Grpc;
using Service.ChangeBalanceGateway.Grpc.Models;

// ReSharper disable IdentifierTypo
// ReSharper disable TemplateIsNotCompileTimeConstantProblem
// ReSharper disable InconsistentLogPropertyNaming

namespace Service.Bitgo.WithdrawalProcessor.Services
{
    public class CryptoWithdrawalService : ICryptoWithdrawalService
    {
        private readonly ILogger<CryptoWithdrawalService> _logger;
        private readonly IAssetMapper _assetMapper;
        private readonly IBitGoClient _bitGoClient;
        private readonly ISpotChangeBalanceService _changeBalanceService;
        private readonly IPublishTransactionService _publishTransactionService;
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;
        private readonly IPublisher<WalletBalanceUpdateOperationInfo> _balanceUpdateOperationInfoPublisher;

        public CryptoWithdrawalService(ILogger<CryptoWithdrawalService> logger,
            IAssetMapper assetMapper,
            IBitGoClient bitGoClient,
            ISpotChangeBalanceService changeBalanceService,
            IPublishTransactionService publishTransactionService,
            DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder, 
            IPublisher<WalletBalanceUpdateOperationInfo> balanceUpdateOperationInfoPublisher)
        {
            _logger = logger;
            _assetMapper = assetMapper;
            _bitGoClient = bitGoClient;
            _changeBalanceService = changeBalanceService;
            _publishTransactionService = publishTransactionService;
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
            _balanceUpdateOperationInfoPublisher = balanceUpdateOperationInfoPublisher;
        }

        public async Task<ValidateAddressResponse> ValidateAddressAsync(ValidateAddressRequest request)
        {
            _logger.LogDebug("Receive ValidateAddressRequest: {jsonText}", JsonConvert.SerializeObject(request));

            request.Address.AddToActivityAsTag("blockchain-address");

            try
            {
                var (coin, _) = _assetMapper.AssetToBitgoCoinAndWallet(request.BrokerId, request.AssetSymbol);

                if (string.IsNullOrEmpty(coin))
                {
                    _logger.LogInformation(
                        $"[ValidateAddressRequest] Cannot found bitgo coin association for asset {request.AssetSymbol}, broker {request.BrokerId}");

                    return new ValidateAddressResponse()
                    {
                        Error = new BitgoErrorType()
                        {
                            Message =
                                $"Cannot found bitgo coin association for asset {request.AssetSymbol}, broker {request.BrokerId}",
                            Code = BitgoErrorType.ErrorCode.AssetIsNotFoundInBitGo
                        }
                    };
                }

                var res = await _bitGoClient.VerifyAddressAsync(coin, request.Address);

                if (!res.Success)
                {
                    throw new Exception(
                        $"[ValidateAddressRequest] Cannot receive data from bitgo: {res.Error.Message}");
                }

                _logger.LogDebug("Address {address} ({coin}) verification result: {resultText}", request.Address, coin,
                    res.Data.IsValid.ToString());

                return new ValidateAddressResponse()
                {
                    IsValid = res.Data.IsValid
                };
            }
            catch (Exception ex)
            {
                ex.FailActivity();

                _logger.LogError(ex, "Cannot handle ValidateAddressRequest {jsonText}",
                    JsonConvert.SerializeObject(request));

                return new ValidateAddressResponse()
                {
                    Error = new BitgoErrorType()
                    {
                        Code = BitgoErrorType.ErrorCode.InternalError,
                        Message = ex.Message
                    }
                };
            }
        }

        public async Task<CryptoWithdrawalResponse> CryptoWithdrawalAsync(CryptoWithdrawalRequest request)
        {
            _logger.LogDebug("Receive CryptoWithdrawalRequest: {jsonText}", JsonConvert.SerializeObject(request));
            request.WalletId.AddToActivityAsTag("walletId");
            request.ClientId.AddToActivityAsTag("clientId");
            request.BrokerId.AddToActivityAsTag("brokerId");
            request.ToAddress.AddToActivityAsTag("blockchain-address");
            
            try
            {
                var (coin, bitgoWallet) = _assetMapper.AssetToBitgoCoinAndWallet(request.BrokerId, request.AssetSymbol);

                if (string.IsNullOrEmpty(coin) || string.IsNullOrEmpty(bitgoWallet))
                {
                    _logger.LogInformation(
                        $"[CryptoWithdrawalRequest] Cannot found bitgo coin association for asset {request.AssetSymbol}, broker {request.BrokerId}");

                    return new CryptoWithdrawalResponse()
                    {
                        Error = new BitgoErrorType()
                        {
                            Message =
                                $"Cannot found bitgo coin association for asset {request.AssetSymbol}, broker {request.BrokerId}",
                            Code = BitgoErrorType.ErrorCode.AssetIsNotFoundInBitGo
                        }
                    };
                }

                var res = await _bitGoClient.VerifyAddressAsync(coin, request.ToAddress);

                if (!res.Success)
                {
                    throw new Exception($"Cannot receive data from bitgo: {res.Error.Message}");
                }

                if (!res.Data.IsValid)
                {
                    _logger.LogDebug("Address {address} ({coin}) is not valid", request.ToAddress, coin);

                    return new CryptoWithdrawalResponse()
                    {
                        Error = new BitgoErrorType()
                        {
                            Code = BitgoErrorType.ErrorCode.AddressIsNotValid,
                            Message = "Address is not valid"
                        }
                    };
                }

                var requestId = request.RequestId ?? Guid.NewGuid().ToString("N");
                var transactionId = OperationIdGenerator.GenerateOperationId(requestId, request.WalletId);


                await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
                WithdrawalEntity withdrawalEntity = new WithdrawalEntity()
                {
                    BrokerId = request.BrokerId,
                    ClientId = request.ClientId,
                    WalletId = request.WalletId,
                    TransactionId = transactionId,
                    Amount = request.Amount,
                    AssetSymbol = request.AssetSymbol,
                    Comment =
                        $"Bitgo withdrawal [{request.AssetSymbol}:{request.Amount}:{request.WalletId}]",
                    Integration = "BitGo",
                    Status = WithdrawalStatus.New,
                    EventDate = DateTime.UtcNow,
                    ToAddress = request.ToAddress,
                    ClientIp = request.ClientIp,
                    ClientLang = request.ClientLang
                };
                await ctx.InsertAsync(withdrawalEntity);

                return new CryptoWithdrawalResponse()
                {
                    OperationId = transactionId
                };
            }
            catch (Exception ex)
            {
                ex.FailActivity();
                _logger.LogError(ex, "Cannot handle CryptoWithdrawalRequest {jsonText}",
                    JsonConvert.SerializeObject(request));
                return new CryptoWithdrawalResponse()
                {
                    Error = new BitgoErrorType()
                    {
                        Code = BitgoErrorType.ErrorCode.InternalError,
                        Message = ex.Message
                    }
                };
            }
        }

        public Task RetryWithdrawalAsync(WithdrawalEntity withdrawalEntity)
        {
            withdrawalEntity.WorkflowState = WithdrawalWorkflowState.Retrying;
            _logger.LogInformation("Manual retry withdrawal with Operation Id {operationId} and status {status}", withdrawalEntity.TransactionId, withdrawalEntity.Status);
            return Task.CompletedTask;
        }

        public async Task ExecuteWithdrawalAsync(WithdrawalEntity withdrawalEntity)
        {
            var request = new CryptoWithdrawalRequest
            {
                Amount = withdrawalEntity.Amount,
                AssetSymbol = withdrawalEntity.AssetSymbol,
                BrokerId = withdrawalEntity.BrokerId,
                ClientId = withdrawalEntity.ClientId,
                WalletId = withdrawalEntity.WalletId,
                RequestId = withdrawalEntity.TransactionId,
                ToAddress = withdrawalEntity.ToAddress
            };

            var executeResult = await ChangeBalanceAsync(withdrawalEntity, request);
            if (executeResult != null)
            {
                throw new Exception($"[CryptoWithdrawalRequest] Cannot execute withdrawal in ME: {JsonConvert.SerializeObject(executeResult)}");
            }

            var (coin, bitgoWallet) = _assetMapper.AssetToBitgoCoinAndWallet(withdrawalEntity.BrokerId, withdrawalEntity.AssetSymbol);

            if (string.IsNullOrEmpty(coin) || string.IsNullOrEmpty(bitgoWallet))
            {
                throw new Exception($"[CryptoWithdrawalRequest] Cannot found bitgo coin association for asset {withdrawalEntity.AssetSymbol}, broker {withdrawalEntity.BrokerId}");
            }

            var coinAmount = _assetMapper.ConvertAmountToBitgo(coin, withdrawalEntity.Amount);
            var sendTransferRequest = new SendTransactionRequest
            {
                BitgoWalletId = bitgoWallet,
                BitgoCoin = coin,
                SequenceId = withdrawalEntity.TransactionId,
                Address = withdrawalEntity.ToAddress,
                Amount = coinAmount.ToString()
            };

            sendTransferRequest.AddToActivityAsJsonTag("transfer-request");

            var transferResult = await _publishTransactionService.SignAndSendTransactionAsync(sendTransferRequest);

            _logger.LogDebug("[CryptoWithdrawalRequest] Withdrawal in BitGo ({operationIdText}): {jsonText}",
                withdrawalEntity.TransactionId, JsonConvert.SerializeObject(transferResult));

            transferResult.AddToActivityAsJsonTag("transfer-result");

            if (transferResult.Error != null)
            {
                throw new Exception($"[CryptoWithdrawalRequest] Cannot execute withdrawal in BitGo ({withdrawalEntity.TransactionId}): {JsonConvert.SerializeObject(transferResult)}, request: {JsonConvert.SerializeObject(request)}");
            }

            var txid = transferResult.Result?.Txid ?? transferResult.DuplicateTransaction?.TxId;

            withdrawalEntity.Status = WithdrawalStatus.Success;
            withdrawalEntity.Txid = txid;
            withdrawalEntity.MatchingEngineId = withdrawalEntity.TransactionId;
        }

        private async Task<CryptoWithdrawalResponse> ChangeBalanceAsync(Withdrawal withdrawalEntity,
            CryptoWithdrawalRequest request)
        {
            var changeBalanceResult = await _changeBalanceService.BlockchainWithdrawalAsync(
                new BlockchainWithdrawalGrpcRequest(
                    withdrawalEntity.TransactionId,
                    withdrawalEntity.ClientId,
                    withdrawalEntity.WalletId,
                    -withdrawalEntity.Amount,
                    withdrawalEntity.AssetSymbol,
                    $"request: {JsonConvert.SerializeObject(request)}",
                    withdrawalEntity.BrokerId,
                    withdrawalEntity.Integration,
                    string.Empty,
                    TransactionStatus.New,
                    withdrawalEntity.ToAddress));

            if (changeBalanceResult.ErrorCode == ChangeBalanceGrpcResponse.ErrorCodeEnum.LowBalance ||
                changeBalanceResult.ErrorCode == ChangeBalanceGrpcResponse.ErrorCodeEnum.WalletDoNotFound)
            {
                return new CryptoWithdrawalResponse
                {
                    Error = new BitgoErrorType
                    {
                        Code = BitgoErrorType.ErrorCode.LowBalance,
                        Message = changeBalanceResult.ErrorMessage
                    }
                };
            }

            if (changeBalanceResult.ErrorCode != ChangeBalanceGrpcResponse.ErrorCodeEnum.Ok &&
                changeBalanceResult.ErrorCode != ChangeBalanceGrpcResponse.ErrorCodeEnum.Duplicate)
            {
                return new CryptoWithdrawalResponse()
                {
                    Error = new BitgoErrorType()
                    {
                        Code = BitgoErrorType.ErrorCode.InternalError,
                        Message = changeBalanceResult.ErrorMessage
                    }
                };
            }

            return null;
        }

    }
}
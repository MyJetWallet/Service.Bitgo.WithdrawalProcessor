using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.BitGo;
using MyJetWallet.BitGo.Settings.Services;
using MyJetWallet.Domain.Transactions;
using Newtonsoft.Json;
using Service.BitGo.SignTransaction.Grpc;
using Service.BitGo.SignTransaction.Grpc.Models;
using Service.Bitgo.WithdrawalProcessor.Grpc;
using Service.Bitgo.WithdrawalProcessor.Grpc.Models;
using Service.ChangeBalanceGateway.Grpc;
using Service.ChangeBalanceGateway.Grpc.Models;

namespace Service.Bitgo.WithdrawalProcessor.Services
{
    public class CryptoWithdrawalService: ICryptoWithdrawalService
    {
        private readonly ILogger<CryptoWithdrawalService> _logger;
        private readonly IAssetMapper _assetMapper;
        private readonly IBitGoClient _bitGoClient;
        private readonly ISpotChangeBalanceService _changeBalanceService;
        private readonly IPublishTransactionService _publishTransactionService;

        public CryptoWithdrawalService(ILogger<CryptoWithdrawalService> logger, IAssetMapper assetMapper, IBitGoClient bitGoClient,
            ISpotChangeBalanceService changeBalanceService, IPublishTransactionService publishTransactionService)
        {
            _logger = logger;
            _assetMapper = assetMapper;
            _bitGoClient = bitGoClient;
            _changeBalanceService = changeBalanceService;
            _publishTransactionService = publishTransactionService;
        }

        public async Task<ValidateAddressResponse> ValidateAddressAsync(ValidateAddressRequest request)
        {
            _logger.LogDebug("Receive ValidateAddressRequest: {jsonText}", JsonConvert.SerializeObject(request));

            try
            {
                var (coin, _) = _assetMapper.AssetToBitgoCoinAndWallet(request.BrokerId, request.AssetSymbol);

                if (string.IsNullOrEmpty(coin))
                {
                    _logger.LogInformation($"[ValidateAddressRequest] Cannot found bitgo coin association for asset {request.AssetSymbol}, broker {request.BrokerId}");
                    
                    return new ValidateAddressResponse()
                    {
                        Error = new BitgoErrorType()
                        {
                            Message = $"Cannot found bitgo coin association for asset {request.AssetSymbol}, broker {request.BrokerId}",
                            Code = BitgoErrorType.ErrorCode.AssetIsNotFoundInBitGo
                        }
                    };
                }

                var res = await _bitGoClient.VerifyAddressAsync(coin, request.Address);

                if (!res.Success)
                {
                    throw new Exception($"[ValidateAddressRequest] Cannot receive data from bitgo: {res.Error.Message}");
                }

                _logger.LogDebug("Address {address} ({coin}) verification result: {resultText}", request.Address, coin, res.Data.IsValid.ToString());

                return new ValidateAddressResponse()
                {
                    IsValid = res.Data.IsValid
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot handle ValidateAddressRequest {jsonText}", JsonConvert.SerializeObject(request));

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

            try
            {
                var (coin, bitgoWallet) = _assetMapper.AssetToBitgoCoinAndWallet(request.BrokerId, request.AssetSymbol);

                if (string.IsNullOrEmpty(coin) || string.IsNullOrEmpty(bitgoWallet))
                {
                    _logger.LogInformation($"[ValidateAddressRequest] Cannot found bitgo coin association for asset {request.AssetSymbol}, broker {request.BrokerId}");

                    return new CryptoWithdrawalResponse()
                    {
                        Error = new BitgoErrorType()
                        {
                            Message = $"Cannot found bitgo coin association for asset {request.AssetSymbol}, broker {request.BrokerId}",
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

                var coinAmount = _assetMapper.ConvertAmountToBitgo(coin, request.Amount);

                var requestId = request.RequestId ?? Guid.NewGuid().ToString("N");
                var transactionId = $"{requestId}:{request.WalletId}";

                var executeResult = await ExecuteWithdrawalAsync(request, transactionId);
                if (executeResult != null)
                {
                    _logger.LogError($"[ValidateAddressRequest] Cannot execute withdrawal in ME: {JsonConvert.SerializeObject(executeResult)}");
                    return executeResult;
                }


                var transferResult = await _publishTransactionService.SignAndSendTransactionAsync(new SendTransactionRequest()
                {
                    BitgoWalletId = bitgoWallet,
                    BitgoCoin = coin,
                    SequenceId = transactionId,
                    Address = request.ToAddress,
                    Amount = coinAmount.ToString()
                });

                _logger.LogDebug("[ValidateAddressRequest] Withdrawal in BitGo ({operationIdText}): {jsonText}", transactionId, JsonConvert.SerializeObject(transferResult));

                if (transferResult.Error != null)
                {
                    _logger.LogError("[ValidateAddressRequest] Cannot execute withdrawal in BitGo ({operationIdText}): {resultText}, request: {requestText}",
                        transactionId,
                        JsonConvert.SerializeObject(transferResult),
                        JsonConvert.SerializeObject(request));

                    return new CryptoWithdrawalResponse()
                    {
                        Error = new BitgoErrorType()
                        {
                            Code = BitgoErrorType.ErrorCode.InternalError,
                            Message = transferResult.Error.Message
                        }
                    };
                }

                var txid = transferResult.Result?.Txid ?? transferResult.DuplicateTransaction?.TxId;

                return new CryptoWithdrawalResponse()
                {
                    OperationId = transactionId,
                    TxId = txid
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot handle CryptoWithdrawalRequest {jsonText}", JsonConvert.SerializeObject(request));

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

        private async Task<CryptoWithdrawalResponse> ExecuteWithdrawalAsync(CryptoWithdrawalRequest request, string transactionId)
        {
            var changeBalanceResult = await _changeBalanceService.BlockchainWithdrawalAsync(
                new BlockchainWithdrawalGrpcRequest(
                    transactionId,
                    request.ClientId,
                    request.WalletId,
                    -request.Amount,
                    request.AssetSymbol,
                    $"request: {JsonConvert.SerializeObject(request)}",
                    request.BrokerId,
                    "BitGo",
                    string.Empty,
                    TransactionStatus.New,
                    request.ToAddress));

            if (changeBalanceResult.ErrorCode == ChangeBalanceGrpcResponse.ErrorCodeEnum.LowBalance)
            {
                {
                    return new CryptoWithdrawalResponse()
                    {
                        Error = new BitgoErrorType()
                        {
                            Code = BitgoErrorType.ErrorCode.LowBalance,
                            Message = changeBalanceResult.ErrorMessage
                        }
                    };
                }
            }

            if (changeBalanceResult.ErrorCode == ChangeBalanceGrpcResponse.ErrorCodeEnum.WalletDoNotFound)
            {
                {
                    return new CryptoWithdrawalResponse()
                    {
                        Error = new BitgoErrorType()
                        {
                            Code = BitgoErrorType.ErrorCode.LowBalance,
                            Message = changeBalanceResult.ErrorMessage
                        }
                    };
                }
            }

            if (changeBalanceResult.ErrorCode != ChangeBalanceGrpcResponse.ErrorCodeEnum.Ok && changeBalanceResult.ErrorCode != ChangeBalanceGrpcResponse.ErrorCodeEnum.Duplicate)
            {
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
            }

            return null;
        }
    }
}

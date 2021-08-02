using System.Diagnostics;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.BitGo;
using MyJetWallet.BitGo.Models.Transfer;
using MyJetWallet.BitGo.Settings.Services;
using MyJetWallet.Domain.Transactions;
using MyJetWallet.Sdk.Service;
using Newtonsoft.Json;
using OpenTelemetry.Trace;
using Service.BalanceHistory.Domain.Models;
using Service.Bitgo.Webhooks.Domain.Models;
using Service.Bitgo.WithdrawalProcessor.Services;
using Service.ChangeBalanceGateway.Grpc;
using Service.ChangeBalanceGateway.Grpc.Models;

namespace Service.Bitgo.WithdrawalProcessor.Jobs
{
    public class SignalBitGoTransferJob
    {
        private readonly ILogger<SignalBitGoTransferJob> _logger;
        private readonly IBitGoClient _bitGoClient;
        private readonly IAssetMapper _assetMapper;
        private readonly ISpotChangeBalanceService _changeBalanceService;
        private readonly IPublisher<WalletBalanceUpdateOperationInfo> _balanceUpdateOperationInfoPublisher;

        public SignalBitGoTransferJob(ISubscriber<SignalBitGoTransfer> subscriber, 
            ILogger<SignalBitGoTransferJob> logger,
            IBitGoClient bitGoClient,
            IAssetMapper assetMapper,
            ISpotChangeBalanceService changeBalanceService, 
            IPublisher<WalletBalanceUpdateOperationInfo> balanceUpdateOperationInfoPublisher)
        {
            _logger = logger;
            _bitGoClient = bitGoClient;
            _assetMapper = assetMapper;
            _changeBalanceService = changeBalanceService;
            _balanceUpdateOperationInfoPublisher = balanceUpdateOperationInfoPublisher;
            subscriber.Subscribe(HandleSignal);
        }

        private async ValueTask HandleSignal(SignalBitGoTransfer signal)
        {
            using var activity = MyTelemetry.StartActivity("Handle event SignalBitGoTransfer");
            signal.AddToActivityAsJsonTag("bitgo-signal");

            _logger.LogInformation("SignalBitGoTransfer is received: {jsonText}", JsonConvert.SerializeObject(signal));

            var transferResp = await _bitGoClient.GetTransferAsync(signal.Coin, signal.WalletId, signal.TransferId);
            var transfer = transferResp.Data;

            if (transfer == null)
            {
                _logger.LogError("Cannot handle BitGo signal, transfer do not found {jsonText}", JsonConvert.SerializeObject(signal));
                Activity.Current?.SetStatus(Status.Error);
                return;
            }

            transfer.AddToActivityAsJsonTag("bitgo-transfer");

            if (string.IsNullOrEmpty(transfer.SequenceId) || transfer.State != "confirmed")
            {
                _logger.LogInformation("SignalBitGoTransfer is skipped");
                return;
            }

            var sequenceId = transfer.SequenceId;

            _logger.LogInformation("Transfer fromm BitGo SequenceId: {SequenceId}, transfer: {jsonText}", sequenceId, JsonConvert.SerializeObject(transfer));

            await _balanceUpdateOperationInfoPublisher.PublishAsync(
                new WalletBalanceUpdateOperationInfo
                {
                OperationId = sequenceId,
                Status = TransactionStatus.Confirmed,
                TxId = transfer.TxId
                });

            await HandleTransactionFee(transfer);

            _logger.LogInformation("SignalBitGoTransfer is handled");

        }

        private async Task HandleTransactionFee(TransferInfo transfer)
        {
            using var activity = MyTelemetry.StartActivity("Handle event SignalBitGoTransfer");
            transfer.AddToActivityAsTag("bitgo-transfer");

            var (broker, symbol) = _assetMapper.BitgoCoinToAsset(transfer.Coin, transfer.WalletId);

            if (!string.IsNullOrEmpty(broker) && !string.IsNullOrEmpty(symbol))
            {
                var feestr = transfer.FeeString;

                if (!long.TryParse(feestr, out var fee))
                {
                    _logger.LogError("Cannot read fee from bitgo transaction. FeeString {feeString}, coin: {coin}, bitgo wallet: {wallet}", feestr, transfer.Coin, transfer.WalletId);
                    activity?.SetStatus(Status.Error);
                    return;
                }

                var wallet = OperationIdGenerator.GetWalletFromOperationId(transfer.SequenceId);

                if (wallet == null)
                {
                    _logger.LogWarning("Cannot parse walletId from bitgo transaction with SequenceId={sequenceId}", transfer.SequenceId);
                    activity?.SetStatus(Status.Error);
                    return;
                }

                wallet.AddToActivityAsTag("walletId");

                var request = new BlockchainFeeApplyGrpcRequest()
                {
                    WalletId = wallet,
                    BrokerId = broker,
                    TransactionId = transfer.SequenceId,
                    AssetSymbol = symbol,
                    FeeAmount = _assetMapper.ConvertAmountFromBitgo(transfer.Coin, fee)
                };

                request.AddToActivityAsJsonTag("fee-apply-request");
                var result = await _changeBalanceService.BlockchainFeeApplyAsync(request);

                if (result.ErrorCode != ChangeBalanceGrpcResponse.ErrorCodeEnum.Ok || !result.Result)
                {
                    _logger.LogError("Cannot apply fee. Request: {requestText}. Error: {jsonText}",
                        JsonConvert.SerializeObject(request), JsonConvert.SerializeObject(result));
                    activity?.SetStatus(Status.Error);
                }
                else
                {
                    _logger.LogInformation("Success apply fee. Request: {requestText}", JsonConvert.SerializeObject(request));
                }
            }
        }
    }
}
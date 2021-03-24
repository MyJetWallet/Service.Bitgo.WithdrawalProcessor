using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.BitGo;
using MyJetWallet.Domain.Transactions;
using Newtonsoft.Json;
using Service.BalanceHistory.Grpc;
using Service.BalanceHistory.Grpc.Models;
using Service.Bitgo.Webhooks.Domain.Models;

namespace Service.Bitgo.WithdrawalProcessor.Jobs
{
    public class SignalBitGoTransferJob
    {
        private readonly ILogger<SignalBitGoTransferJob> _logger;
        private readonly IBitGoClient _bitGoClient;
        private readonly IWalletBalanceUpdateOperationInfoService _balanceUpdateOperationInfoService;

        public SignalBitGoTransferJob(ISubscriber<SignalBitGoTransfer> subscriber, 
            ILogger<SignalBitGoTransferJob> logger,
            IBitGoClient bitGoClient,
            IWalletBalanceUpdateOperationInfoService balanceUpdateOperationInfoService)
        {
            _logger = logger;
            _bitGoClient = bitGoClient;
            _balanceUpdateOperationInfoService = balanceUpdateOperationInfoService;

            subscriber.Subscribe(HandleSignal);
        }

        private async ValueTask HandleSignal(SignalBitGoTransfer signal)
        {
            _logger.LogInformation("SignalBitGoTransfer is received: {jsonText}", JsonConvert.SerializeObject(signal));

            var transferResp = await _bitGoClient.GetTransferAsync(signal.Coin, signal.WalletId, signal.TransferId);
            var transfer = transferResp.Data;

            if (transfer == null)
            {
                _logger.LogError("Cannot handle BitGo signal, transfer do not found {jsonText}", JsonConvert.SerializeObject(signal));
                return;
            }

            if (string.IsNullOrEmpty(transfer.SequenceId) || transfer.State != "confirmed")
            {
                _logger.LogInformation("SignalBitGoTransfer is skipped");
                return;
            }

            var sequenceId = transfer.SequenceId;

            _logger.LogInformation("Transfer fromm BitGo SequenceId: {SequenceId}, transfer: {jsonText}", sequenceId, JsonConvert.SerializeObject(transfer));

            await _balanceUpdateOperationInfoService.UpdateTransactionOperationInfoAsync(new UpdateTransactionOperationInfoRequest()
            {
                OperationId = sequenceId,
                Status = TransactionStatus.Confirmed,
                TxId = transfer.TxId
            });

            _logger.LogInformation("SignalBitGoTransfer is handled");

        }
    }
}
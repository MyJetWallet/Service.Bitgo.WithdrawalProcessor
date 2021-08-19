using System;
using System.Runtime.Serialization;
// ReSharper disable IdentifierTypo

namespace Service.Bitgo.WithdrawalProcessor.Domain.Models
{
    [DataContract]
    public class Withdrawal
    {
        
        public Withdrawal(long id, string brokerId, string clientId, string walletId, string transactionId, double amount,
            string assetSymbol, string comment, string integration, string txid, WithdrawalStatus status,
            string matchingEngineId, string lastError, int retriesCount, DateTime eventDate, string toAddress, string clientIp, string clientLang)
        {
            Id = id;
            BrokerId = brokerId;
            ClientId = clientId;
            WalletId = walletId;
            TransactionId = transactionId;
            Amount = amount;
            AssetSymbol = assetSymbol;
            Comment = comment;
            Integration = integration;
            Txid = txid;
            Status = status;
            MatchingEngineId = matchingEngineId;
            LastError = lastError;
            RetriesCount = retriesCount;
            EventDate = eventDate;
            ToAddress = toAddress;
            ClientIp = clientIp;
            ClientLang = clientLang;
        }

        public Withdrawal(Withdrawal withdrawal) : this(withdrawal.Id, withdrawal.BrokerId, withdrawal.ClientId, withdrawal.WalletId,
            withdrawal.TransactionId, withdrawal.Amount, withdrawal.AssetSymbol, withdrawal.Comment,
            withdrawal.Integration, withdrawal.Txid, withdrawal.Status, withdrawal.MatchingEngineId, withdrawal.LastError,
            withdrawal.RetriesCount, withdrawal.EventDate, withdrawal.ToAddress, withdrawal.ClientIp, withdrawal.ClientLang)
        {
        }

        public Withdrawal()
        {
        }
        
        [DataMember(Order = 1)] public long Id { get; set; }

        [DataMember(Order = 2)] public string BrokerId { get; set; }

        [DataMember(Order = 3)] public string ClientId { get; set; }

        [DataMember(Order = 4)] public string WalletId { get; set; }    

        [DataMember(Order = 5)] public string TransactionId { get; set; }

        [DataMember(Order = 6)] public double Amount { get; set; }

        [DataMember(Order = 7)] public string AssetSymbol { get; set; }

        [DataMember(Order = 8)] public string Comment { get; set; }

        [DataMember(Order = 9)] public string Integration { get; set; }

        [DataMember(Order = 10)] public string Txid { get; set; }

        [DataMember(Order = 11)] public WithdrawalStatus Status { get; set; }

        [DataMember(Order = 12)] public string MatchingEngineId { get; set; }

        [DataMember(Order = 13)] public string LastError { get; set; }

        [DataMember(Order = 14)] public int RetriesCount { get; set; }

        [DataMember(Order = 15)] public DateTime EventDate { get; set; }

        [DataMember(Order = 16)] public string ToAddress { get; set; }
        
        [DataMember(Order = 17)] public string ClientLang { get; set; }
        [DataMember(Order = 18)] public string ClientIp { get; set; }
        [DataMember(Order = 19)] public DateTime NotificationTime { get; set; }
    }
}
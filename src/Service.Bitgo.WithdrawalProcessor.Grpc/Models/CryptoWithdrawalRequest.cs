using System.Runtime.Serialization;

namespace Service.Bitgo.WithdrawalProcessor.Grpc.Models
{
    [DataContract]
    public class CryptoWithdrawalRequest
    {
        [DataMember(Order = 1)] public string RequestId { get; set; }
        [DataMember(Order = 2)] public string BrokerId { get; set; }
        [DataMember(Order = 3)] public string ClientId { get; set; }
        [DataMember(Order = 4)] public string WalletId { get; set; }
        [DataMember(Order = 5)] public string AssetSymbol { get; set; }
        [DataMember(Order = 6)] public double Amount { get; set; }
        [DataMember(Order = 7)] public string ToAddress { get; set; }
        [DataMember(Order = 8)] public string ClientLang { get; set; }
        [DataMember(Order = 9)] public string ClientIp { get; set; }
    }
}
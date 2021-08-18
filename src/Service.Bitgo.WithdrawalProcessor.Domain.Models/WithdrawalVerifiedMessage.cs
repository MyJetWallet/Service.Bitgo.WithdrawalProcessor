using System.Runtime.Serialization;

namespace Service.Bitgo.WithdrawalProcessor.Domain.Models
{
    [DataContract]
    public class WithdrawalVerifiedMessage
    {
        public const string TopicName = "jet-wallet-crypto-withdrawal-verification";
        
        [DataMember(Order = 1)] public string WithdrawalProcessId { get; set; }
        [DataMember(Order = 2)] public string ClientIp { get; set; }
    }
}
using System.Runtime.Serialization;

namespace Service.Bitgo.WithdrawalProcessor.Grpc.Models
{
    [DataContract]
    public class GetWithdrawalRequest
    {
        [DataMember(Order = 1)] public long Id { get; set; }
    }
}
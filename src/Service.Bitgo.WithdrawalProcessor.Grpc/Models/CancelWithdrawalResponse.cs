using System.Runtime.Serialization;

namespace Service.Bitgo.WithdrawalProcessor.Grpc.Models
{
    [DataContract]
    public class CancelWithdrawalResponse
    {
        [DataMember(Order = 1)] public bool Success { get; set; }
        [DataMember(Order = 2)] public string ErrorMessage { get; set; }
        [DataMember(Order = 3)] public long WithdrawalId { get; set; }
    }
}
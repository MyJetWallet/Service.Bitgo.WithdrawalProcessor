using System.Collections.Generic;
using System.Runtime.Serialization;
using Service.Bitgo.WithdrawalProcessor.Domain.Models;

namespace Service.Bitgo.WithdrawalProcessor.Grpc.Models
{
    [DataContract]
    public class GetWithdrawalsResponse
    {
        [DataMember(Order = 1)] public bool Success { get; set; }
        [DataMember(Order = 2)] public string ErrorMessage { get; set; }
        [DataMember(Order = 3)] public long IdForNextQuery { get; set; }
        [DataMember(Order = 4)] public List<Withdrawal> WithdrawalCollection { get; set; }
    }
}
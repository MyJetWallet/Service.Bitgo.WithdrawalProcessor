using System.Collections.Generic;
using System.Runtime.Serialization;
using Service.Bitgo.WithdrawalProcessor.Domain.Models;

namespace Service.Bitgo.WithdrawalProcessor.Grpc.Models
{
    [DataContract]
    public class GetWithdrawalResponse
    {
        [DataMember(Order = 1)] public bool Success { get; set; }
        [DataMember(Order = 2)] public string ErrorMessage { get; set; }
        [DataMember(Order = 3)] public Withdrawal Withdrawal { get; set; }
    }
}
﻿using System.Runtime.Serialization;

namespace Service.Bitgo.WithdrawalProcessor.Grpc.Models
{
    [DataContract]
    public class RetryWithdrawalRequest
    {
        [DataMember(Order = 1)] public long WithdrawalId { get; set; }
    }
}
using System.Runtime.Serialization;

namespace Service.Bitgo.WithdrawalProcessor.Grpc.Models
{
    [DataContract]
    public class CryptoWithdrawalResponse
    {
        [DataMember(Order = 1)] public BitgoErrorType Error { get; set; }
        [DataMember(Order = 2)] public string OperationId { get; set; }
        [DataMember(Order = 3)] public string TxId { get; set; }

        

        
    }
}
using System.Runtime.Serialization;

namespace Service.Bitgo.WithdrawalProcessor.Grpc.Models
{
    [DataContract]
    public class ValidateAddressResponse
    {
        [DataMember(Order = 1)] public bool IsValid { get; set; }
        [DataMember(Order = 2)] public BitgoErrorType Error { get; set; }
    }
}
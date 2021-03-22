using System.Runtime.Serialization;

namespace Service.Bitgo.WithdrawalProcessor.Grpc.Models
{
    [DataContract]
    public class ValidateAddressRequest
    {
        [DataMember(Order = 1)] public string BrokerId { get; set; }
        [DataMember(Order = 2)] public string AssetSymbol { get; set; }
        [DataMember(Order = 3)] public string Address { get; set; }
    }
}
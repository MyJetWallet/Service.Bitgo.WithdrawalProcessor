using System.Runtime.Serialization;

namespace Service.Bitgo.WithdrawalProcessor.Grpc.Models
{
    [DataContract]
    public class BitgoErrorType
    {
        [DataMember(Order = 1)] public ErrorCode Code { get; set; }
        [DataMember(Order = 2)] public string Message { get; set; }

        public enum ErrorCode
        {
            Ok,
            AssetIsNotFoundInBitGo,
            BalanceNotEnough,
            InternalError,
            AddressIsNotValid,
            AssetDoNotFound,
            AssetIsDisabled,
            LowBalance
        }
    }
}
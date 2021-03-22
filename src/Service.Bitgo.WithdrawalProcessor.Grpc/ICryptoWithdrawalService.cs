using System.ServiceModel;
using System.Threading.Tasks;
using Service.Bitgo.WithdrawalProcessor.Grpc.Models;

namespace Service.Bitgo.WithdrawalProcessor.Grpc
{
    [ServiceContract]
    public interface ICryptoWithdrawalService
    {
        [OperationContract]
        Task<ValidateAddressResponse> ValidateAddressAsync(ValidateAddressRequest request);

        [OperationContract]
        Task<CryptoWithdrawalResponse> CryptoWithdrawalAsync(CryptoWithdrawalRequest request);
    }
}
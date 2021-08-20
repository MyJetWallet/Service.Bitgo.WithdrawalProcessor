using System.ServiceModel;
using System.Threading.Tasks;
using Service.Bitgo.WithdrawalProcessor.Grpc.Models;

namespace Service.Bitgo.WithdrawalProcessor.Grpc
{
    [ServiceContract]
    public interface IBitgoWithdrawalService
    {
        [OperationContract]
        Task<GetWithdrawalsResponse> GetWithdrawals(GetWithdrawalsRequest request);

        [OperationContract]
        Task<RetryWithdrawalResponse> RetryWithdrawal(RetryWithdrawalRequest request);

        [OperationContract]
        Task<CancelWithdrawalResponse> CancelWithdrawal(CancelWithdrawalRequest request);

        [OperationContract]
        Task<GetWithdrawalResponse> GetWithdrawalById(GetWithdrawalRequest request);
    }
}
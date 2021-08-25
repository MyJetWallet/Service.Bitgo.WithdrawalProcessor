namespace Service.Bitgo.WithdrawalProcessor.Domain.Models
{
    public enum WithdrawalWorkflowState
    {
        OK,
        Retrying,
        Failed
    }
}
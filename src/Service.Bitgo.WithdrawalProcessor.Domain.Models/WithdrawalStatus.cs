namespace Service.Bitgo.WithdrawalProcessor.Domain.Models
{
    public enum WithdrawalStatus
    {
        New,
        Success,
        Cancelled,
        Pending,
        ApprovalPending
    }
}
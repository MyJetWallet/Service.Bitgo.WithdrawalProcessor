namespace Service.Bitgo.WithdrawalProcessor.Domain.Models
{
    public enum WithdrawalStatus
    {
        New,
        ErrorInMe,
        Error,
        Success,
        Cancelled,
        Stopped,
        Pending,
        ApprovalPending
    }
}
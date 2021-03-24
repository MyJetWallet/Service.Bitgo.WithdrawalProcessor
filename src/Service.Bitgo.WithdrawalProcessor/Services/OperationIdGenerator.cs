namespace Service.Bitgo.WithdrawalProcessor.Services
{
    public static class OperationIdGenerator
    {
        private const string Separator = "|:|";

        public static string GenerateOperationId(string requestId, string walletId)
        {
            return $"{requestId}{Separator}{walletId}";
        }

        public static string GetWalletFromOperationId(string operationId)
        {
            if (string.IsNullOrEmpty(operationId))
                return string.Empty;

            var prms = operationId.Split(Separator);

            if (prms.Length != 2)
                return string.Empty;

            return prms[1];
        }
    }
}
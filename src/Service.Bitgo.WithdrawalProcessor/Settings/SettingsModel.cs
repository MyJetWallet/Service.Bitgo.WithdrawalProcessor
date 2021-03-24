using SimpleTrading.SettingsReader;

namespace Service.Bitgo.WithdrawalProcessor.Settings
{
    [YamlAttributesOnly]
    public class SettingsModel
    {
        [YamlProperty("BitgoWithdrawalProcessor.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("BitgoWithdrawalProcessor.MyNoSqlReaderHostPort")]
        public string MyNoSqlReaderHostPort { get; set; }

        [YamlProperty("BitgoWithdrawalProcessor.ChangeBalanceGatewayGrpcServiceUrl")]
        public string ChangeBalanceGatewayGrpcServiceUrl { get; set; }

        [YamlProperty("BitgoWithdrawalProcessor.BitgoAccessTokenReadOnly")]
        public string BitgoAccessTokenReadOnly { get; set; }

        [YamlProperty("BitgoWithdrawalProcessor.BitgoExpressUrl")]
        public string BitgoExpressUrl { get; set; }

        [YamlProperty("BitgoWithdrawalProcessor.BitgoSignTransactionGrpcServiceUrl")]
        public string BitgoSignTransactionGrpcServiceUrl { get; set; }

        [YamlProperty("BitgoWithdrawalProcessor.BalanceHistoryWriterGrpcServiceUrl")]
        public string BalanceHistoryWriterGrpcServiceUrl { get; set; }

        [YamlProperty("BitgoWithdrawalProcessor.SpotServiceBusHostPort")]
        public string SpotServiceBusHostPort { get; set; }
    }
}
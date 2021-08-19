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

        [YamlProperty("BitgoWithdrawalProcessor.SpotServiceBusHostPort")]
        public string SpotServiceBusHostPort { get; set; }

        [YamlProperty("BitgoWithdrawalProcessor.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("BitgoWithdrawalProcessor.PostgresConnectionString")]
        public string PostgresConnectionString { get; set; }

        [YamlProperty("BitgoWithdrawalProcessor.WithdrawalsProcessingIntervalSec")]
        public int WithdrawalsProcessingIntervalSec { get; set; }

        [YamlProperty("BitgoWithdrawalProcessor.WithdrawalsRetriesLimit")]
        public int WithdrawalsRetriesLimit { get; set; }
        
        [YamlProperty("BitgoWithdrawalProcessor.WithdrawalExpirationTimeInMin")]
        public int WithdrawalExpirationTimeInMin { get; set; }
        
        [YamlProperty("BitgoWithdrawalProcessor.VerificationCodesGrpcUrl")]
        public string VerificationCodesGrpcUrl { get; set; }
        
    }
}
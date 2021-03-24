using Autofac;
using MyJetWallet.BitGo;
using MyJetWallet.BitGo.Settings.Ioc;
using MyJetWallet.BitGo.Settings.NoSql;
using MyJetWallet.BitGo.Settings.Services;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataReader;
using Service.AssetsDictionary.Client;
using Service.BalanceHistory.Client;
using Service.BitGo.SignTransaction.Client;
using Service.ChangeBalanceGateway.Client;

namespace Service.Bitgo.WithdrawalProcessor.Modules
{
    public class ServiceModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            MyNoSqlTcpClient myNoSqlClient = new MyNoSqlTcpClient(Program.ReloadedSettings(e => e.MyNoSqlReaderHostPort),
                ApplicationEnvironment.HostName ?? $"{ApplicationEnvironment.AppName}:{ApplicationEnvironment.AppVersion}");

            builder
                .RegisterInstance(myNoSqlClient)
                .AsSelf()
                .SingleInstance();

            builder
                .RegisterAssetsDictionaryClients(myNoSqlClient);

            builder
                .RegisterSpotChangeBalanceGatewayClient(Program.Settings.ChangeBalanceGatewayGrpcServiceUrl);

            var bitgoClient = new BitGoClient(Program.Settings.BitgoAccessTokenReadOnly, Program.Settings.BitgoExpressUrl);
            bitgoClient.ThrowThenErrorResponse = false;

            builder
                .RegisterInstance(bitgoClient)
                .As<IBitGoClient>()
                .SingleInstance();

            builder.RegisterBitgoSettingsReader(myNoSqlClient);

            builder.RegisterBitGoSignTransactionClient(Program.Settings.BitgoSignTransactionGrpcServiceUrl);

            builder.RegisterBalanceHistoryOperationInfoClient(Program.Settings.BalanceHistoryWriterGrpcServiceUrl);

        }
    }
}
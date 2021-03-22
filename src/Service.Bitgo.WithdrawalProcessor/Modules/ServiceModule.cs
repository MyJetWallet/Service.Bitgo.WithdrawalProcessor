using Autofac;
using MyJetWallet.BitGo;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataReader;
using Service.AssetsDictionary.Client;
using Service.BitGo.SignTransaction.Client;
using Service.Bitgo.WithdrawalProcessor.NoSql;
using Service.Bitgo.WithdrawalProcessor.Services;
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

            builder
                .RegisterType<WalletMapper>()
                .As<IWalletMapper>()
                .SingleInstance();

            builder
                .RegisterType<AssetMapper>()
                .As<IAssetMapper>()
                .SingleInstance();

            builder
                .RegisterInstance(new MyNoSqlReadRepository<BitgoAssetMapEntity>(myNoSqlClient, BitgoAssetMapEntity.TableName))
                .As<IMyNoSqlServerDataReader<BitgoAssetMapEntity>>()
                .SingleInstance();

            builder
                .RegisterInstance(new MyNoSqlReadRepository<BitgoCoinEntity>(myNoSqlClient, BitgoCoinEntity.TableName))
                .As<IMyNoSqlServerDataReader<BitgoCoinEntity>>()
                .SingleInstance();

            builder.RegisterBitGoSignTransactionClient(Program.Settings.BitgoSignTransactionGrpcServiceUrl);

        }
    }
}
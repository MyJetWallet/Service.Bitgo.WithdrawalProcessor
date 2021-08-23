using Autofac;
using Microsoft.Extensions.Logging;
using MyJetWallet.BitGo;
using MyJetWallet.BitGo.Settings.Ioc;
using MyJetWallet.BitGo.Settings.NoSql;
using MyJetWallet.BitGo.Settings.Services;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.ServiceBus;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataReader;
using MyServiceBus.Abstractions;
using MyServiceBus.TcpClient;
using Service.AssetsDictionary.Client;
using Service.BalanceHistory.Client;
using Service.BitGo.SignTransaction.Client;
using Service.Bitgo.Webhooks.Client;
using Service.Bitgo.WithdrawalProcessor.Domain.Models;
using Service.Bitgo.WithdrawalProcessor.Jobs;
using Service.Bitgo.WithdrawalProcessor.Services;
using Service.ChangeBalanceGateway.Client;
using Service.VerificationCodes.Client;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

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

            ServiceBusLogger = Program.LogFactory.CreateLogger(nameof(MyServiceBusTcpClient));

            var serviceBusClient = new MyServiceBusTcpClient(Program.ReloadedSettings(e => e.SpotServiceBusHostPort), ApplicationEnvironment.HostName);
            serviceBusClient.Log.AddLogException(ex => ServiceBusLogger.LogInformation(ex, "Exception in MyServiceBusTcpClient"));
            serviceBusClient.Log.AddLogInfo(info => ServiceBusLogger.LogDebug($"MyServiceBusTcpClient[info]: {info}"));
            serviceBusClient.SocketLogs.AddLogInfo((context, msg) => ServiceBusLogger.LogInformation($"MyServiceBusTcpClient[Socket {context?.Id}|{context?.ContextName}|{context?.Inited}][Info] {msg}"));
            serviceBusClient.SocketLogs.AddLogException((context, exception) => ServiceBusLogger.LogInformation(exception, $"MyServiceBusTcpClient[Socket {context?.Id}|{context?.ContextName}|{context?.Inited}][Exception] {exception.Message}"));
            builder.RegisterInstance(serviceBusClient).AsSelf().SingleInstance();
            
            builder.RegisterMyServiceBusSubscriberSingle<WithdrawalVerifiedMessage>(serviceBusClient, WithdrawalVerifiedMessage.TopicName, "Bitgo-WithdrawalProcessor-Verification", TopicQueueType.Permanent);
            
            builder.RegisterSignalBitGoTransferSubscriber(serviceBusClient, "Bitgo-WithdrawalProcessor", TopicQueueType.Permanent);
            builder.BalanceHistoryOperationInfoPublisher(serviceBusClient);
            builder.RegisterWithdrawalOperationHistoryPublisher(serviceBusClient);

            builder.RegisterVerificationCodesClient(Program.Settings.VerificationCodesGrpcUrl);
            
            builder
                .RegisterType<SignalBitGoTransferJob>()
                .AutoActivate()
                .SingleInstance();

            builder
                .RegisterType<WithdrawalProcessingJob>()
                .AutoActivate()
                .SingleInstance()
                .AsSelf();

            builder
                .RegisterType<CryptoWithdrawalService>()
                .AutoActivate()
                .SingleInstance()
                .AsSelf();
        }

        public static ILogger ServiceBusLogger { get; set; }
    }
}
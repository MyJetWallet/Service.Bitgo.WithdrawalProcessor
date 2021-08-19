using Autofac;
using MyJetWallet.Sdk.ServiceBus;
using MyServiceBus.TcpClient;
using Service.Bitgo.WithdrawalProcessor.Domain.Models;
using Service.Bitgo.WithdrawalProcessor.Grpc;
// ReSharper disable UnusedMember.Global

namespace Service.Bitgo.WithdrawalProcessor.Client
{
    public static class AutofacHelper
    {
        public static void RegisterBitgoCryptoWithdrawalClient(this ContainerBuilder builder, string bitgoCryptoWithdrawalGrpcServiceUrl)
        {
            var factory = new BitgoWithdrawalProcessorClientFactory(bitgoCryptoWithdrawalGrpcServiceUrl);

            builder.RegisterInstance(factory.GetCryptoWithdrawalService()).As<ICryptoWithdrawalService>().SingleInstance();
        }
        public static void RegisterBitgoWithdrawalClient(this ContainerBuilder builder, string bitgoWithdrawalGrpcServiceUrl)
        {
            var factory = new BitgoWithdrawalServiceClientFactory(bitgoWithdrawalGrpcServiceUrl);

            builder.RegisterInstance(factory.GetBitgoWithdrawalService()).As<IBitgoWithdrawalService>().SingleInstance();
        }

        public static void RegisterWithdrawalVerificationPublisher(this ContainerBuilder builder, MyServiceBusTcpClient serviceBusClient)
        {
            builder.RegisterMyServiceBusPublisher<WithdrawalVerifiedMessage>(serviceBusClient, WithdrawalVerifiedMessage.TopicName, true);
        }
    }
}
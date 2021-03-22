using Autofac;
using Service.Bitgo.WithdrawalProcessor.Grpc;
// ReSharper disable UnusedMember.Global

namespace Service.Bitgo.WithdrawalProcessor.Client
{
    public static class AutofacHelper
    {
        public static void RegisterBitgoCryptoWithdrawalClient(this ContainerBuilder builder, string balanceHistoryGrpcServiceUrl)
        {
            var factory = new BitgoWithdrawalProcessorClientFactory(balanceHistoryGrpcServiceUrl);

            builder.RegisterInstance(factory.GetCryptoWithdrawalService()).As<ICryptoWithdrawalService>().SingleInstance();
        }
    }
}
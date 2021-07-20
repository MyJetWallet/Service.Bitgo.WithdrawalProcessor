using System;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using JetBrains.Annotations;
using MyJetWallet.Sdk.GrpcMetrics;
using ProtoBuf.Grpc.Client;
using Service.Bitgo.WithdrawalProcessor.Grpc;

namespace Service.Bitgo.WithdrawalProcessor.Client
{
    [UsedImplicitly]
    public class BitgoWithdrawalServiceClientFactory
    {
        private readonly CallInvoker _channel;

        public BitgoWithdrawalServiceClientFactory(string bitgoWithdrawalServiceGrpcUrl)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel = GrpcChannel.ForAddress(bitgoWithdrawalServiceGrpcUrl);
            _channel = channel.Intercept(new PrometheusMetricsInterceptor());
        }

        public IBitgoWithdrawalService GetBitgoWithdrawalService()
        {
            return _channel.CreateGrpcService<IBitgoWithdrawalService>();
        }
    }
}
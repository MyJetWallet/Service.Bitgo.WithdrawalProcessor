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
    public class BitgoWithdrawalProcessorClientFactory
    {
        private readonly CallInvoker _channel;

        public BitgoWithdrawalProcessorClientFactory(string assetsDictionaryGrpcServiceUrl)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel = GrpcChannel.ForAddress(assetsDictionaryGrpcServiceUrl);
            _channel = channel.Intercept(new PrometheusMetricsInterceptor());
        }

        public ICryptoWithdrawalService GetCryptoWithdrawalService() => _channel.CreateGrpcService<ICryptoWithdrawalService>();
    }
}

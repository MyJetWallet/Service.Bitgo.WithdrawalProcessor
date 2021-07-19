using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.DataReader;
using MyServiceBus.TcpClient;
using Service.Bitgo.WithdrawalProcessor.Jobs;

namespace Service.Bitgo.WithdrawalProcessor
{
    public class ApplicationLifetimeManager : ApplicationLifetimeManagerBase
    {
        private readonly ILogger<ApplicationLifetimeManager> _logger;
        private readonly MyNoSqlTcpClient _myNoSqlTcpClient;
        private readonly MyServiceBusTcpClient _myServiceBusTcpClient;
        private readonly WithdrawalProcessingJob _withdrawalProcessingJob;

        public ApplicationLifetimeManager(IHostApplicationLifetime appLifetime, ILogger<ApplicationLifetimeManager> logger, MyNoSqlTcpClient myNoSqlTcpClient,
            MyServiceBusTcpClient myServiceBusTcpClient, WithdrawalProcessingJob withdrawalProcessingJob)
            : base(appLifetime)
        {
            _logger = logger;
            _myNoSqlTcpClient = myNoSqlTcpClient;
            _myServiceBusTcpClient = myServiceBusTcpClient;
            _withdrawalProcessingJob = withdrawalProcessingJob;
        }

        protected override void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called");
            _myNoSqlTcpClient.Start();
            _logger.LogInformation("MyNoSqlTcpClient is started");
            _myServiceBusTcpClient.Start();
            _logger.LogInformation("MyServiceBusTcpClient is started");
            _withdrawalProcessingJob.Start();
            _logger.LogInformation("WithdrawalProcessingJob is started");
        }

        protected override void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called");
            _myNoSqlTcpClient.Stop();
            _logger.LogInformation("MyNoSqlTcpClient is stopped");
            _myServiceBusTcpClient.Stop();
            _logger.LogInformation("MyServiceBusTcpClient is stopped");
            _withdrawalProcessingJob.Stop();
            _logger.LogInformation("WithdrawalProcessingJob is stopped");
        }

        protected override void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called");
        }
    }
}

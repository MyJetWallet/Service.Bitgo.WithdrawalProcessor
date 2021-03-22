using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.DataReader;

namespace Service.Bitgo.WithdrawalProcessor
{
    public class ApplicationLifetimeManager : ApplicationLifetimeManagerBase
    {
        private readonly ILogger<ApplicationLifetimeManager> _logger;
        private readonly MyNoSqlTcpClient _myNoSqlTcpClient;

        public ApplicationLifetimeManager(IHostApplicationLifetime appLifetime, ILogger<ApplicationLifetimeManager> logger, MyNoSqlTcpClient myNoSqlTcpClient)
            : base(appLifetime)
        {
            _logger = logger;
            _myNoSqlTcpClient = myNoSqlTcpClient;
        }

        protected override void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called.");
            _myNoSqlTcpClient.Start();
            _logger.LogInformation("MyNoSqlTcpClient is started.");
        }

        protected override void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called.");
            _myNoSqlTcpClient.Stop();
            _logger.LogInformation("MyNoSqlTcpClient is stopped.");
        }

        protected override void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called.");
        }
    }
}

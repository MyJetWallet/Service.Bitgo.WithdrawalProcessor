using MyJetWallet.Sdk.Postgres;

namespace Service.Bitgo.WithdrawalProcessor.Postgres.DesignTime
{
    public class ContextFactory : MyDesignTimeContextFactory<DatabaseContext>
    {
        public ContextFactory() : base(options => new DatabaseContext(options))
        {
        }
    }
}
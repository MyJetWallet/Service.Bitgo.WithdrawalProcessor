using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.Bitgo.WithdrawalProcessor.Domain.Models;
using Service.Bitgo.WithdrawalProcessor.Postgres.Models;

namespace Service.Bitgo.WithdrawalProcessor.Postgres
{
    public class DatabaseContext : DbContext
    {
        public const string Schema = "withdrawals";

        private const string DepositsTableName = "withdrawals";

        private Activity _activity;

        public DatabaseContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<WithdrawalEntity> Withdrawals { get; set; }
        public static ILoggerFactory LoggerFactory { get; set; }

        public static DatabaseContext Create(DbContextOptionsBuilder<DatabaseContext> options)
        {
            var activity = MyTelemetry.StartActivity($"Database context {Schema}")?.AddTag("db-schema", Schema);

            var ctx = new DatabaseContext(options.Options) {_activity = activity};

            return ctx;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (LoggerFactory != null) optionsBuilder.UseLoggerFactory(LoggerFactory).EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);

            SetWithdrawalEntry(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private void SetWithdrawalEntry(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WithdrawalEntity>().ToTable(DepositsTableName);
            modelBuilder.Entity<WithdrawalEntity>().Property(e => e.Id).UseIdentityColumn();
            modelBuilder.Entity<WithdrawalEntity>().HasKey(e => e.Id);
            modelBuilder.Entity<WithdrawalEntity>().Property(e => e.BrokerId).HasMaxLength(128);
            modelBuilder.Entity<WithdrawalEntity>().Property(e => e.ClientId).HasMaxLength(128);
            modelBuilder.Entity<WithdrawalEntity>().Property(e => e.WalletId).HasMaxLength(128);
            modelBuilder.Entity<WithdrawalEntity>().Property(e => e.TransactionId).HasMaxLength(128);
            modelBuilder.Entity<WithdrawalEntity>().Property(e => e.Amount);
            modelBuilder.Entity<WithdrawalEntity>().Property(e => e.AssetSymbol).HasMaxLength(64);
            modelBuilder.Entity<WithdrawalEntity>().Property(e => e.Comment).HasMaxLength(512);
            modelBuilder.Entity<WithdrawalEntity>().Property(e => e.Integration).HasMaxLength(64);
            modelBuilder.Entity<WithdrawalEntity>().Property(e => e.Txid).HasMaxLength(256);
            modelBuilder.Entity<WithdrawalEntity>().Property(e => e.Status).HasDefaultValue(WithdrawalStatus.New);
            modelBuilder.Entity<WithdrawalEntity>().Property(e => e.MatchingEngineId).HasMaxLength(64).IsRequired(false);
            modelBuilder.Entity<WithdrawalEntity>().Property(e => e.LastError).HasMaxLength(2048).IsRequired(false);
            modelBuilder.Entity<WithdrawalEntity>().Property(e => e.RetriesCount).HasDefaultValue(0);
            modelBuilder.Entity<WithdrawalEntity>().Property(e => e.EventDate);
            modelBuilder.Entity<WithdrawalEntity>().Property(e => e.ToAddress).HasMaxLength(512);
            modelBuilder.Entity<WithdrawalEntity>().Property(e => e.ClientIp).HasMaxLength(64).IsRequired(false);
            modelBuilder.Entity<WithdrawalEntity>().Property(e => e.ClientLang).HasMaxLength(64).IsRequired(false);
            modelBuilder.Entity<WithdrawalEntity>().Property(e => e.NotificationTime);

        
            modelBuilder.Entity<WithdrawalEntity>().HasIndex(e => e.Status);
            modelBuilder.Entity<WithdrawalEntity>().HasIndex(e => e.TransactionId).IsUnique();
        }

        public async Task<int> InsertAsync(WithdrawalEntity entity)
        {
            var result = await Withdrawals.Upsert(entity).On(e => e.TransactionId).NoUpdate().RunAsync();
            return result;
        }

        public async Task UpdateAsync(WithdrawalEntity entity)
        {
            await UpdateAsync(new List<WithdrawalEntity>{entity});
        }

        public async Task UpdateAsync(IEnumerable<WithdrawalEntity> entities)
        {
            Withdrawals.UpdateRange(entities);
            await SaveChangesAsync();
        }
    }
}
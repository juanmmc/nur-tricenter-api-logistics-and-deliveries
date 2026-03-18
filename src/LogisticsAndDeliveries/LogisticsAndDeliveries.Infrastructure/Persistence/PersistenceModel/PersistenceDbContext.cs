using LogisticsAndDeliveries.Infrastructure.Outbox;
using LogisticsAndDeliveries.Infrastructure.Persistence.PersistenceModel.EFCoreEntities;
using Microsoft.EntityFrameworkCore;

namespace LogisticsAndDeliveries.Infrastructure.Persistence.PersistenceModel
{
    public class PersistenceDbContext : DbContext, IDatabase
    {
        public DbSet<PackagePersistenceModel> Package
        {
            get; set;
        }
        public DbSet<DriverPersistenceModel> Driver
        {
            get; set;
        }
        public DbSet<OutboxMessage> OutboxMessage
        {
            get; set;
        }
        public PersistenceDbContext(DbContextOptions<PersistenceDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxMessage>(builder =>
            {
                builder.ToTable("outbox_message");
                builder.HasKey(message => message.Id);

                builder.Property(message => message.Id).HasColumnName("id");
                builder.Property(message => message.EventName).HasColumnName("eventname");
                builder.Property(message => message.Type).HasColumnName("type");
                builder.Property(message => message.Content).HasColumnName("content");
                builder.Property(message => message.OccurredOnUtc).HasColumnName("occurredonutc");
                builder.Property(message => message.ProcessedOnUtc).HasColumnName("processedonutc");
                builder.Property(message => message.Error).HasColumnName("error");

                builder.HasIndex(message => new { message.ProcessedOnUtc, message.OccurredOnUtc })
                    .HasDatabaseName("IX_outbox_message_processedOnUtc_occurredOnUtc");
            });

            base.OnModelCreating(modelBuilder);
        }

        public void Migrate()
        {
            Database.Migrate();
        }
    }
}
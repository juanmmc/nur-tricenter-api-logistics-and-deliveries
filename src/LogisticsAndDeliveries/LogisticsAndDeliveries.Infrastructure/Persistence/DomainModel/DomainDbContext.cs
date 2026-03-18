using LogisticsAndDeliveries.Domain.Drivers;
using LogisticsAndDeliveries.Domain.Packages;
using LogisticsAndDeliveries.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace LogisticsAndDeliveries.Infrastructure.Persistence.DomainModel
{
    public class DomainDbContext : DbContext
    {
        public DbSet<Package> Package
        {
            get; set;
        }
        public DbSet<Driver> Driver
        {
            get; set;
        }
        public DbSet<OutboxMessage> OutboxMessage
        {
            get; set;
        }

        public DomainDbContext(DbContextOptions<DomainDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

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

            modelBuilder.Ignore<Core.Abstractions.DomainEvent>();
        }
    }
}
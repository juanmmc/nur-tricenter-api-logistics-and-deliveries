using System.Text.Json;
using LogisticsAndDeliveries.Domain.Packages.DomainEvents;
using LogisticsAndDeliveries.Infrastructure.Outbox;
using LogisticsAndDeliveries.Infrastructure.Persistence.DomainModel;
using MediatR;

namespace LogisticsAndDeliveries.Infrastructure.DomainEvents
{
    internal sealed class PackageDeliveryStatusChangedDomainEventToOutboxHandler
        : INotificationHandler<PackageDeliveryStatusChanged>
    {
        private const string PackageDispatchStatusUpdatedEventName = "logistica.paquete.estado-actualizado";

        private readonly DomainDbContext _dbContext;

        public PackageDeliveryStatusChangedDomainEventToOutboxHandler(DomainDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task Handle(PackageDeliveryStatusChanged notification, CancellationToken cancellationToken)
        {
            var payload = new
            {
                packageId = notification.PackageId,
                driverId = notification.DriverId,
                number = notification.Number,
                deliveryStatus = notification.DeliveryStatus,
                incidentType = notification.IncidentType,
                incidentDescription = notification.IncidentDescription,
                deliveryEvidence = notification.DeliveryEvidence,
                occurredOn = notification.OccurredOn,
                updatedAt = notification.UpdatedAt
            };

            var outboxMessage = new OutboxMessage
            {
                Id = notification.Id,
                EventName = PackageDispatchStatusUpdatedEventName,
                Type = notification.GetType().FullName ?? nameof(PackageDeliveryStatusChanged),
                Content = JsonSerializer.Serialize(payload),
                OccurredOnUtc = notification.OccurredOn.ToUniversalTime()
            };

            _dbContext.OutboxMessage.Add(outboxMessage);

            return Task.CompletedTask;
        }
    }
}
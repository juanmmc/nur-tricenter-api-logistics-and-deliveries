using LogisticsAndDeliveries.Core.Abstractions;

namespace LogisticsAndDeliveries.Domain.Packages.DomainEvents;

public sealed record PackageDeliveryStatusChanged(
    Guid PackageId,
    Guid DriverId,
    string Number,
    string DeliveryStatus,
    string? IncidentType,
    string? IncidentDescription,
    string? DeliveryEvidence,
    DateTime? UpdatedAt) : DomainEvent;


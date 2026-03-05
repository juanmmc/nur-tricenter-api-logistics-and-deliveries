using LogisticsAndDeliveries.Core.Abstractions;
using LogisticsAndDeliveries.Core.Results;
using LogisticsAndDeliveries.Domain.Drivers;
using LogisticsAndDeliveries.Domain.Packages.DomainEvents;
using System.Xml.Linq;

namespace LogisticsAndDeliveries.Domain.Packages
{
    public class Package : AggregateRoot
    {
        public Guid DriverId { get; private set; }
        public string Number { get; private set; }
        public Guid PatientId { get; private set; }
        public string PatientName { get; private set; }
        public string PatientPhone { get; private set; }
        public string DeliveryAddress { get; private set; }
        public double DeliveryLatitude { get; private set; }
        public double DeliveryLongitude { get; private set; }
        public DateOnly DeliveryDate { get; private set; }
        public string? DeliveryEvidence { get; private set; }
        public int DeliveryOrder { get; private set; }
        public DeliveryStatus DeliveryStatus { get; private set; }
        public IncidentType? IncidentType { get; private set; }
        public string? IncidentDescription { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        // Constructor para EF Core
        private Package() { }

        // Constructor de dominio
        public Package(Guid id, string number, Guid patientId, string patientName, string patientPhone, string deliveryAddress, double deliveryLatitude, double deliveryLongitude, DateOnly deliveryDate, Guid driverId) : base(id)
        {
            if (string.IsNullOrWhiteSpace(number))
            {
                throw new DomainException(PackageErrors.NumberIsRequired());
            }
            if (patientId == Guid.Empty)
            {
                throw new DomainException(PackageErrors.PatientIdIsRequired());
            }
            if (string.IsNullOrWhiteSpace(patientName))
            {
                throw new DomainException(PackageErrors.PatientNameIsRequired());
            }
            if (string.IsNullOrWhiteSpace(patientPhone))
            {
                throw new DomainException(PackageErrors.PatientPhoneIsRequired());
            }
            if (string.IsNullOrWhiteSpace(deliveryAddress))
            {
                throw new DomainException(PackageErrors.DeliveryAddressIsRequired());
            }
            if (deliveryLatitude < -90 || deliveryLatitude > 90)
            {
                throw new DomainException(PackageErrors.InvalidDeliveryLatitude());
            }
            if (deliveryLongitude < -180 || deliveryLongitude > 180)
            {
                throw new DomainException(PackageErrors.InvalidDeliveryLongitude());
            }
            if (deliveryDate == DateOnly.FromDateTime(DateTime.UtcNow))
            {
                throw new DomainException(PackageErrors.InvalidDeliveryDate());
            }
            if (driverId == Guid.Empty)
            {
                throw new DomainException(PackageErrors.DriverIdIsRequired());
            }
            this.Number = number;
            this.PatientId = patientId;
            this.PatientName = patientName;
            this.PatientPhone = patientPhone;
            this.DeliveryAddress = deliveryAddress;
            this.DeliveryLatitude = deliveryLatitude;
            this.DeliveryLongitude = deliveryLongitude;
            this.DeliveryDate = deliveryDate;
            this.DriverId = driverId;
            this.DeliveryOrder = 0;
            this.DeliveryStatus = DeliveryStatus.Pending;
        }

        public void SetDeliveryOrder(int deliveryOrder)
        {
            if (deliveryOrder <= 0)
                throw new DomainException(DeliveryErrors.InvalidOrderValue);

            if (this.DeliveryStatus != DeliveryStatus.Pending)
                throw new DomainException(DeliveryErrors.InvalidStatusTransition);

            this.DeliveryOrder = deliveryOrder;
            this.UpdatedAt = DateTime.UtcNow;
        }

        public void MarkDeliveryInTransit()
        {
            if (this.DeliveryOrder <= 0)
                throw new DomainException(DeliveryErrors.InvalidOrderValue);

            if (this.DeliveryStatus != DeliveryStatus.Pending)
                throw new DomainException(DeliveryErrors.InvalidStatusTransition);

            this.DeliveryStatus = DeliveryStatus.InTransit;
            this.UpdatedAt = DateTime.UtcNow;
            AddStatusChangedDomainEvent();
        }

        public void MarkDeliveryCompleted(string deliveryEvidence)
        {
            if (string.IsNullOrWhiteSpace(deliveryEvidence))
                throw new DomainException(DeliveryErrors.DeliveryEvidenceIsRequired);

            if (this.DeliveryStatus != DeliveryStatus.InTransit)
                throw new DomainException(DeliveryErrors.InvalidStatusTransition);

            this.DeliveryEvidence = deliveryEvidence;
            this.DeliveryStatus = DeliveryStatus.Completed;
            this.UpdatedAt = DateTime.UtcNow;
            AddStatusChangedDomainEvent();
        }

        public void MarkDeliveryFailed()
        {
            if (this.DeliveryStatus != DeliveryStatus.InTransit)
                throw new DomainException(DeliveryErrors.InvalidStatusTransition);
            this.DeliveryStatus = DeliveryStatus.Failed;
            this.UpdatedAt = DateTime.UtcNow;
            AddStatusChangedDomainEvent();
        }

        public void CancelDelivery()
        {
            if (this.DeliveryStatus == DeliveryStatus.Completed)
                throw new DomainException(DeliveryErrors.CannotCancelCompletedDelivery);
            this.DeliveryStatus = DeliveryStatus.Cancelled;
            this.UpdatedAt = DateTime.UtcNow;
            AddStatusChangedDomainEvent();
        }

        public void RegisterDeliveryIncident(IncidentType incidentType, string incidentDescription)
        {
            if (string.IsNullOrWhiteSpace(incidentDescription))
                throw new DomainException(DeliveryErrors.IncidentDescriptionIsRequired);

            if (this.DeliveryStatus != DeliveryStatus.Failed)
                throw new DomainException(DeliveryErrors.CannotRegisterIncidentInCurrentStatus);

            this.IncidentType = incidentType;
            this.IncidentDescription = incidentDescription;
            this.UpdatedAt = DateTime.UtcNow;
            AddStatusChangedDomainEvent();
        }

        private void AddStatusChangedDomainEvent()
        {
            AddDomainEvent(new PackageDeliveryStatusChanged(
                Id,
                DriverId,
                Number,
                DeliveryStatus.ToString(),
                IncidentType?.ToString(),
                IncidentDescription,
                DeliveryEvidence,
                UpdatedAt));
        }
    }
}

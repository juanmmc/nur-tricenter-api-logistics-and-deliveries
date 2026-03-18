using LogisticsAndDeliveries.Core.Abstractions;
using LogisticsAndDeliveries.Core.Results;
using LogisticsAndDeliveries.Domain.Packages;

namespace LogisticsAndDeliveries.Domain.Drivers
{
    public class Driver : AggregateRoot
    {
        public string Name
        {
            get; private set;
        }
        public double? Longitude
        {
            get; private set;
        }
        public double? Latitude
        {
            get; private set;
        }
        public DateTime? LastLocationUpdate
        {
            get; private set;
        }


        // Constructor para EF Core
        private Driver()
        {
        }

        // Constructor de dominio
        public Driver(Guid id, string name) : base(id)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new DomainException(DriverErrors.NameIsRequired());
            }

            Name = name;
        }

        public void UpdateLocation(double longitude, double latitude)
        {
            if (latitude < -90 || latitude > 90)
            {
                throw new DomainException(DriverErrors.InvalidLatitude());
            }
            if (longitude < -180 || longitude > 180)
            {
                throw new DomainException(DriverErrors.InvalidLongitude());
            }
            Longitude = longitude;
            Latitude = latitude;
            LastLocationUpdate = DateTime.UtcNow;
        }
    }
}
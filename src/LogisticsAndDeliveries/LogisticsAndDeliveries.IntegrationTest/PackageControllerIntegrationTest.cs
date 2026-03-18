using FluentAssertions;
using LogisticsAndDeliveries.Application.Drivers.Dto;
using LogisticsAndDeliveries.Application.Packages.Dto;
using LogisticsAndDeliveries.IntegrationTest.Factories;
using System.Net;
using System.Net.Http.Json;

namespace LogisticsAndDeliveries.IntegrationTest;

public class PackageControllerIntegrationTest
{
    private readonly HttpClient _httpClient;

    public PackageControllerIntegrationTest()
    {
        this._httpClient = HttpClientFactory.CreateClient();
    }

    [Fact]
    public async Task CreatePackage_CreateValidPackage()
    {
        // Arrange
        var id = Guid.NewGuid();
        var number = "PKG-01";
        var patientId = Guid.NewGuid();
        var patientName = "Ana Perez";
        var patientPhone = "77912345";
        var deliveryAddress = "Calle Falsa 123";
        var deliveryLatitude = -16.5000;
        var deliveryLongitude = -68.1500;
        var deliveryDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var driverId = await GetRandomDriverIdAsync();
        var payload = new
        {
            id,
            number,
            patientId,
            patientName,
            patientPhone,
            deliveryAddress,
            deliveryLatitude,
            deliveryLongitude,
            deliveryDate,
            driverId
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/Package/createPackage", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var returnedId = await response.Content.ReadFromJsonAsync<Guid>();
        returnedId.Should().Be(id);
    }

    [Fact]
    public async Task GetPackage_AfterCreate()
    {
        // Arrange
        var id = Guid.NewGuid();
        var number = "PKG-02";
        var patientId = Guid.NewGuid();
        var patientName = "Luis Gomez";
        var patientPhone = "77987654";
        var deliveryAddress = "Av. San Martin #123";
        var deliveryLatitude = -16.5000;
        var deliveryLongitude = -68.1500;
        var deliveryDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var driverId = await GetRandomDriverIdAsync();
        var createPayload = new
        {
            id,
            number,
            patientId,
            patientName,
            patientPhone,
            deliveryAddress,
            deliveryLatitude,
            deliveryLongitude,
            deliveryDate,
            driverId
        };
        var createResp = await _httpClient.PostAsJsonAsync("/api/Package/createPackage", createPayload);
        createResp.EnsureSuccessStatusCode();

        // Act
        var getResp = await _httpClient.GetAsync($"/api/Package/getPackage?packageId={id}");

        // Assert
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await getResp.Content.ReadFromJsonAsync<PackageDto>();
        dto.Should().NotBeNull();
        dto.Id.Should().Be(id);
        dto.Number.Should().Be(number);
        dto.PatientId.Should().Be(patientId);
        dto.PatientName.Should().Be(patientName);
        dto.PatientPhone.Should().Be(patientPhone);
        dto.DeliveryAddress.Should().Be(deliveryAddress);
        dto.DeliveryLatitude.Should().Be(deliveryLatitude);
        dto.DeliveryLongitude.Should().Be(deliveryLongitude);
        dto.DeliveryDate.Should().Be(deliveryDate);
        dto.DriverId.Should().Be(driverId);
    }

    [Fact]
    public async Task GetPackagesByDriverAndDeliveryDate_ReturnsOnlyDriverPackages()
    {
        // Arrange
        var driverId = await GetRandomDriverIdAsync();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        // create two packages for this driver on 'date'
        var pkg1 = await CreatePackageAsync(driverId, date);
        var pkg2 = await CreatePackageAsync(driverId, date);

        // create a package for another driver or different date
        var otherDriverId = await GetRandomDriverIdAsync();
        if (otherDriverId == driverId)
        {
            // ensure different driver
            otherDriverId = Guid.NewGuid();
        }
        await CreatePackageAsync(otherDriverId, date.AddDays(1));

        // Act
        var resp = await _httpClient.GetAsync($"/api/Package/getPackagesByDriverAndDeliveryDate?driverId={driverId}&deliveryDate={date:yyyy-MM-dd}");
        resp.EnsureSuccessStatusCode();

        var listResult = await resp.Content.ReadFromJsonAsync<List<PackageDto>>();
        listResult.Should().NotBeNull();
        listResult!.Should().OnlyHaveUniqueItems(p => p.Id);
        listResult.Should().OnlyContain(p => p.DriverId == driverId && p.DeliveryDate == date);
        listResult.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task SetDeliveryOrder_SetsOrderAndPersists()
    {
        // Arrange
        var driverId = await GetRandomDriverIdAsync();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var pkgId = await CreatePackageAsync(driverId, date);

        // Act - set order
        var setPayload = new { PackageId = pkgId, DeliveryOrder = 5 };
        var setResp = await _httpClient.PostAsJsonAsync("/api/Package/setDeliveryOrder", setPayload);
        setResp.EnsureSuccessStatusCode();

        // Assert - read back
        var dto = await GetPackageDtoAsync(pkgId);
        dto.DeliveryOrder.Should().Be(5);
        dto.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkDeliveryInTransit_Then_Complete_WorksAndSetsEvidence()
    {
        // Arrange
        var driverId = await GetRandomDriverIdAsync();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var pkgId = await CreatePackageAsync(driverId, date);

        // Need to set a positive delivery order first
        var setPayload = new { PackageId = pkgId, DeliveryOrder = 1 };
        var setResp = await _httpClient.PostAsJsonAsync("/api/Package/setDeliveryOrder", setPayload);
        setResp.EnsureSuccessStatusCode();

        // Act - mark in transit
        var inTransitResp = await _httpClient.PostAsJsonAsync("/api/Package/markDeliveryInTransit", new { PackageId = pkgId });
        inTransitResp.EnsureSuccessStatusCode();

        // Assert status InTransit
        var dtoInTransit = await GetPackageDtoAsync(pkgId);
        dtoInTransit.DeliveryStatus.Should().Be("InTransit");
        dtoInTransit.UpdatedAt.Should().NotBeNull();

        // Act - mark completed
        var evidence = "evidence-base64";
        var completeResp = await _httpClient.PostAsJsonAsync("/api/Package/markDeliveryCompleted", new { PackageId = pkgId, DeliveryEvidence = evidence });
        completeResp.EnsureSuccessStatusCode();

        // Assert completed
        var dtoCompleted = await GetPackageDtoAsync(pkgId);
        dtoCompleted.DeliveryStatus.Should().Be("Completed");
        dtoCompleted.DeliveryEvidence.Should().Be(evidence);
        dtoCompleted.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelDelivery_SetsCancelled()
    {
        // Arrange
        var driverId = await GetRandomDriverIdAsync();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var pkgId = await CreatePackageAsync(driverId, date);

        // Act - cancel
        var cancelResp = await _httpClient.PostAsJsonAsync("/api/Package/cancelDelivery", new { PackageId = pkgId });
        cancelResp.EnsureSuccessStatusCode();

        // Assert
        var dto = await GetPackageDtoAsync(pkgId);
        dto.DeliveryStatus.Should().Be("Cancelled");
        dto.UpdatedAt.Should().NotBeNull();
    }

    // Helpers

    private async Task<Guid> CreatePackageAsync(Guid driverId, DateOnly deliveryDate)
    {
        var id = Guid.NewGuid();
        var payload = new
        {
            id,
            number = $"PKG-{id.ToString().Substring(0, 8)}",
            patientId = Guid.NewGuid(),
            patientName = "Test Patient",
            patientPhone = "77000000",
            deliveryAddress = "Test Address",
            deliveryLatitude = -16.5,
            deliveryLongitude = -68.15,
            deliveryDate,
            driverId
        };
        var resp = await _httpClient.PostAsJsonAsync("/api/Package/createPackage", payload);
        resp.EnsureSuccessStatusCode();
        var returned = await resp.Content.ReadFromJsonAsync<Guid>();
        returned.Should().Be(id);
        return id;
    }

    private async Task<PackageDto> GetPackageDtoAsync(Guid packageId)
    {
        var resp = await _httpClient.GetAsync($"/api/Package/getPackage?packageId={packageId}");
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<PackageDto>();
        if (dto is null) throw new InvalidOperationException("PackageDto null");
        return dto;
    }

    private async Task<Guid> GetRandomDriverIdAsync()
    {
        var getResp = await _httpClient.GetAsync(("/api/Driver/getDrivers"));
        getResp.EnsureSuccessStatusCode();
        var dtos = await getResp.Content.ReadFromJsonAsync<List<DriverDto>>();
        if (dtos == null || dtos.Count == 0)
        {
            throw new InvalidOperationException("No drivers available for testing.");
        }
        var randomIndex = Random.Shared.Next(dtos.Count);
        return dtos[randomIndex].Id;
    }
}
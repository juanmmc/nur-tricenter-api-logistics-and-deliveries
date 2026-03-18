using LogisticsAndDeliveries.Application.Packages.CreatePackage;
using LogisticsAndDeliveries.Core.Abstractions;
using LogisticsAndDeliveries.Core.Results;
using LogisticsAndDeliveries.Domain.Packages;
using Moq;

namespace LogisticsAndDeliveries.Test.Application.Packages;

public class CreatePackageHandlerTest
{
    private readonly Mock<IPackageRepository> _packageRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreatePackageHandler _handler;

    public CreatePackageHandlerTest()
    {
        _packageRepositoryMock = new Mock<IPackageRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CreatePackageHandler(_packageRepositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesPackageSuccessfully()
    {
        // Arrange
        var command = new CreatePackageCommand
        {
            Id = Guid.NewGuid(),
            Number = "PKG-001",
            PatientId = Guid.NewGuid(),
            PatientName = "Juan Miguel",
            PatientPhone = "72193153",
            DeliveryAddress = "Urb. Palmas del Norte",
            DeliveryLatitude = 10.5,
            DeliveryLongitude = 20.5,
            DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            DriverId = Guid.NewGuid()
        };

        _packageRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Package>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(command.Id, result.Value);
        _packageRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Package>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidPackageData_ReturnsFailure()
    {
        // Arrange
        var command = new CreatePackageCommand
        {
            Id = Guid.NewGuid(),
            Number = "", // Número inválido
            PatientId = Guid.NewGuid(),
            PatientName = "Juan Miguel",
            PatientPhone = "72193153",
            DeliveryAddress = "Urb. Palmas del Norte",
            DeliveryLatitude = 10.5,
            DeliveryLongitude = 20.5,
            DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            DriverId = Guid.NewGuid()
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Package.NumberIsRequired", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);

        _packageRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Package>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
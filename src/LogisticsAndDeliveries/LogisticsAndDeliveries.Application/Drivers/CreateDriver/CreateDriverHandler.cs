using LogisticsAndDeliveries.Core.Abstractions;
using LogisticsAndDeliveries.Core.Results;
using LogisticsAndDeliveries.Domain.Drivers;
using MediatR;

namespace LogisticsAndDeliveries.Application.Drivers.CreateDriver
{
    public class CreateDriverHandler : IRequestHandler<CreateDriverCommand, Result<Guid>>
    {
        private readonly IDriverRepository _driverRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateDriverHandler(IDriverRepository driverRepository, IUnitOfWork unitOfWork)
        {
            _driverRepository = driverRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(CreateDriverCommand request, CancellationToken cancellationToken)
        {
            Guid newDriverId = Guid.NewGuid();

            // Crear el agregado de dominio
            var driver = new Driver(newDriverId, request.Name);

            // Persistir el agregado
            await _driverRepository.AddAsync(driver);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success(driver.Id);
        }
    }
}
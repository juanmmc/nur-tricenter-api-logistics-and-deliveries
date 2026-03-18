using LogisticsAndDeliveries.Application.Packages.CancelDelivery;
using LogisticsAndDeliveries.Application.Packages.CreatePackage;
using LogisticsAndDeliveries.Application.Packages.GetPackage;
using LogisticsAndDeliveries.Application.Packages.GetPackagesByDriverAndDeliveryDate;
using LogisticsAndDeliveries.Application.Packages.MarkDeliveryCompleted;
using LogisticsAndDeliveries.Application.Packages.MarkDeliveryFailed;
using LogisticsAndDeliveries.Application.Packages.MarkDeliveryInTransit;
using LogisticsAndDeliveries.Application.Packages.RegisterDeliveryIncident;
using LogisticsAndDeliveries.Application.Packages.SetDeliveryOrder;
using LogisticsAndDeliveries.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsAndDeliveries.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PackageController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PackageController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("createPackage")]
        public async Task<IActionResult> CreatePackage([FromBody] CreatePackageCommand request)
        {
            var result = await _mediator.Send(request);
            return result.ToActionResult(this);
        }

        [HttpGet("getPackage")]
        public async Task<IActionResult> GetPackage([FromQuery] Guid packageId)
        {
            var result = await _mediator.Send(new GetPackageQuery(packageId));
            return result.ToActionResult(this);
        }

        [HttpGet("getPackagesByDriverAndDeliveryDate")]
        public async Task<IActionResult> getPackagesByDriverAndDeliveryDate([FromQuery] Guid driverId, [FromQuery] DateOnly deliveryDate)
        {
            var result = await _mediator.Send(new GetPackagesByDriverAndDeliveryDateQuery(driverId, deliveryDate));
            return result.ToActionResult(this);
        }

        [HttpPost("setDeliveryOrder")]
        public async Task<IActionResult> SetDeliveryOrder([FromBody] SetDeliveryOrderCommand request){
            var result = await _mediator.Send(request);
            return result.ToActionResult(this);
        }

        [HttpPost("cancelDelivery")]
        public async Task<IActionResult> CancelDelivery([FromBody] CancelDeliveryCommand request){
            var result = await _mediator.Send(request);
            return result.ToActionResult(this);
        }

        [HttpPost("markDeliveryFailed")]
        public async Task<IActionResult> MarkDeliveryFailed([FromBody] MarkDeliveryFailedCommand request){
            var result = await _mediator.Send(request);
            return result.ToActionResult(this);
        }

        [HttpPost("markDeliveryInTransit")]
        public async Task<IActionResult> MarkDeliveryInTransit([FromBody] MarkDeliveryInTransitCommand request)
        {
            var result = await _mediator.Send(request);
            return result.ToActionResult(this);
        }

        [HttpPost("markDeliveryCompleted")]
        public async Task<IActionResult> MarkDeliveryCompleted([FromBody] MarkDeliveryCompletedCommand request)
        {
            var result = await _mediator.Send(request);
            return result.ToActionResult(this);
        }

        [HttpPost("registerDeliveryIncident")]
        public async Task<IActionResult> RegisterDeliveryIncident([FromBody] RegisterDeliveryIncidentCommand request)
        {
            var result = await _mediator.Send(request);
            return result.ToActionResult(this);
        }
    }
}
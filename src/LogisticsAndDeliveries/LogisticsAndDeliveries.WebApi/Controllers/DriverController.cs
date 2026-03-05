using LogisticsAndDeliveries.Application.Drivers.CreateDriver;
using LogisticsAndDeliveries.Application.Drivers.GetDriver;
using LogisticsAndDeliveries.Application.Drivers.GetDrivers;
using LogisticsAndDeliveries.Application.Drivers.UpdateDriverLocation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using LogisticsAndDeliveries.WebApi.Extensions;

namespace LogisticsAndDeliveries.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DriverController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DriverController(IMediator mediator)
        {
            _mediator = mediator;
        }
        
        [HttpPost("createDriver")]
        public async Task<IActionResult> CreateDriver([FromBody] CreateDriverCommand request)
        {
            // rol admin
            var result = await _mediator.Send(request);

            return result.ToActionResult(this);
        }

        [HttpGet("getDriver")]
        public async Task<IActionResult> GetDriver([FromQuery] Guid driverId)
        {
            // admin , driver
            var result = await _mediator.Send(new GetDriverQuery(driverId));
            return result.ToActionResult(this);
        }

        [HttpPost("updateDriverLocation")]
        public async Task<IActionResult> UpdateDriverLocation([FromBody] UpdateDriverLocationCommand request)
        {
            var result = await _mediator.Send(request);
            return result.ToActionResult(this);
        }

        [HttpGet("getDrivers")]
        public async Task<IActionResult> GetDrivers()
        {
            var result = await _mediator.Send(new GetDriversQuery());
            return result.ToActionResult(this);
        }
    }
}

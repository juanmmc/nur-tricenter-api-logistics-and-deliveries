using LogisticsAndDeliveries.Core.Results;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsAndDeliveries.WebApi.Extensions
{
    internal static class ResultExtensions
    {
        public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
        {
            if (result is null)
                return controller.StatusCode(500);

            if (result.IsSuccess)
                return controller.Ok(result.Value);

            var error = result.Error;
            return error.Type switch
            {
                ErrorType.NotFound => controller.NotFound(error),
                ErrorType.Conflict => controller.Conflict(error),
                ErrorType.Failure => controller.BadRequest(error),
                ErrorType.Problem => controller.StatusCode(500, error),
                _ => controller.BadRequest(error)
            };
        }

        public static IActionResult ToActionResult(this Result result, ControllerBase controller)
        {
            if (result is null)
                return controller.StatusCode(500);

            if (result.IsSuccess)
                return controller.Ok();

            var error = result.Error;
            return error.Type switch
            {
                ErrorType.NotFound => controller.NotFound(error),
                ErrorType.Conflict => controller.Conflict(error),
                ErrorType.Failure => controller.BadRequest(error),
                ErrorType.Problem => controller.StatusCode(500, error),
                _ => controller.BadRequest(error)
            };
        }
    }
}
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Common;

namespace Api.Extensions
{
    public static class ApiResultExtensions
    {
        public static IActionResult OkResult<T>(this ControllerBase controller, T data)
        {
            return controller.Ok(ApiResult<T>.FromData(data));
        }

        public static IActionResult CreatedResult<T>(this ControllerBase controller, T data)
        {
            return controller.StatusCode(StatusCodes.Status201Created, ApiResult<T>.FromData(data));
        }

        public static IActionResult ErrorResult(this ControllerBase controller, ErrorCode code, string message, string? field, int statusCode)
        {
            var error = new ErrorDetail
            {
                Code = code,
                Message = message,
                Field = field
            };

            var result = new ApiResult<object?>
            {
                Error = error
            };

            return controller.StatusCode(statusCode, result);
        }

        public static IActionResult BadRequestResult(this ControllerBase controller, string message, string? field = null)
        {
            return controller.ErrorResult(ErrorCode.Validation, message, field, StatusCodes.Status400BadRequest);
        }

        public static IActionResult UnauthorizedResult(this ControllerBase controller, string message)
        {
            return controller.ErrorResult(ErrorCode.Unauthorized, message, null, StatusCodes.Status401Unauthorized);
        }

        public static IActionResult ForbiddenResult(this ControllerBase controller, string message)
        {
            return controller.ErrorResult(ErrorCode.Forbidden, message, null, StatusCodes.Status403Forbidden);
        }

        public static IActionResult NotFoundResult(this ControllerBase controller, string message, string? field = null)
        {
            return controller.ErrorResult(ErrorCode.NotFound, message, field, StatusCodes.Status404NotFound);
        }

        public static IActionResult ConflictResult(this ControllerBase controller, string message, string? field = null)
        {
            return controller.ErrorResult(ErrorCode.Conflict, message, field, StatusCodes.Status409Conflict);
        }

        public static IActionResult ServerErrorResult(this ControllerBase controller, string message)
        {
            return controller.ErrorResult(ErrorCode.ServerError, message, null, StatusCodes.Status500InternalServerError);
        }
    }
}

using System.Security.Claims;
using Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    public abstract class AppControllerBase : ControllerBase
    {
        protected TimeZoneInfo GetTimeZone()
        {
            var timeZoneId = HttpContext.Request.Headers["X-TimeZoneId"].ToString();
            return string.IsNullOrWhiteSpace(timeZoneId)
                ? TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
                : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }

        protected static DateTimeOffset ConvertFromUtc(DateTimeOffset utcDateTime, TimeZoneInfo timeZone)
        {
            var utc = utcDateTime.UtcDateTime;
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
            var offset = timeZone.GetUtcOffset(utc);
            return new DateTimeOffset(local, offset);
        }

        protected static DateTimeOffset? ConvertFromUtc(DateTimeOffset? utcDateTime, TimeZoneInfo timeZone)
        {
            if (utcDateTime == null)
            {
                return null;
            }

            return ConvertFromUtc(utcDateTime.Value, timeZone);
        }

        protected static DateTime ConvertFromUtc(DateTime utcDateTime, TimeZoneInfo timeZone)
        {
            var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
        }

        protected static DateTime? ConvertFromUtc(DateTime? utcDateTime, TimeZoneInfo timeZone)
        {
            if (utcDateTime == null)
            {
                return null;
            }

            return ConvertFromUtc(utcDateTime.Value, timeZone);
        }

        protected bool TryGetUserId(out Guid userId)
        {
            var currentUserIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(currentUserIdValue, out userId);
        }

        protected bool IsAdmin()
        {
            return User.IsInRole("Admin") || User.IsInRole("ADMIN");
        }

        protected bool IsBusinessOwner()
        {
            return User.IsInRole("BusinessOwner") || User.IsInRole("BUSINESSOWNER");
        }
    }
}

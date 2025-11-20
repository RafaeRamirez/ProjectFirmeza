using System;

namespace Firmeza.Web.Utils
{
    public static class DateTimeExtensions
    {
        public static DateTime ToLocalFromUtc(this DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Local => value,
                DateTimeKind.Utc => value.ToLocalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime()
            };
        }

        public static DateTime? ToLocalFromUtc(this DateTime? value)
        {
            return value.HasValue ? value.Value.ToLocalFromUtc() : null;
        }
    }
}

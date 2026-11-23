namespace TaskApi.Api.Mapping;

public static class RequestDateTimeMapping
{
    public static DateTime? ToUtc(this DateTime? value)
    {
        if (value is null)
            return null;

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,

            DateTimeKind.Local => value.Value.ToUniversalTime(),

            // API contract treats timestamps without an offset as UTC.
            DateTimeKind.Unspecified =>
                DateTime.SpecifyKind(
                    value.Value,
                    DateTimeKind.Utc),

            _ => value.Value
        };
    }
}
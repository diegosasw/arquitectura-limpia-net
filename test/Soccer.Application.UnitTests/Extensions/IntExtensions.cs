using System;

namespace Soccer.Application.UnitTests.Extensions;

public static class IntExtensions
{
    public static Guid ToGuid(this int value)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        var result = new Guid(bytes);
        return result;
    }

    public static DateTime ToDateTime(this int secondsOffset, DateTimeKind dateTimeKind = DateTimeKind.Utc)
    {
        var dateTime = new DateTime(2021, 1, 1, 0, 0, 0, dateTimeKind);
        dateTime = dateTime.AddSeconds(secondsOffset);
        return dateTime;
    }
}

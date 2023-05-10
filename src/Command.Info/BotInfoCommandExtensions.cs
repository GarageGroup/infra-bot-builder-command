using System;
using System.Globalization;
using System.Threading;

namespace GarageGroup.Infra.Bot.Builder;

public static class BotInfoCommandExtensions
{
    static BotInfoCommandExtensions()
        =>
        lazyRussianStandardTimeZone = new(GetRussianStandardTimeZone, LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Lazy<TimeZoneInfo> lazyRussianStandardTimeZone;

    public static string? ToRussianStandardTimeZoneString(this DateTimeOffset? dateTime)
    {
        if (dateTime is null)
        {
            return default;
        }

        var russianStandardTime = TimeZoneInfo.ConvertTime(dateTime.Value, lazyRussianStandardTimeZone.Value);
        return russianStandardTime.ToString("dd.MM.yyyy HH:mm:ss ('GMT'z)", CultureInfo.InvariantCulture);
    }

    private static TimeZoneInfo GetRussianStandardTimeZone()
        =>
        TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
}
using System;
using System.Collections.Generic;

namespace GarageGroup.Infra.Bot.Builder;

public readonly record struct BotInfoData
{
    public required FlatArray<KeyValuePair<string, string?>> Values { get; init; }
}
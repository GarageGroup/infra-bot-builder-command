using System;
using System.Collections.Generic;

namespace GarageGroup.Infra.Bot.Builder;

public sealed record class BotInfoData
{
    public BotInfoData(FlatArray<KeyValuePair<string, string?>> values)
        =>
        Values = values;

    public FlatArray<KeyValuePair<string, string?>> Values { get; }
}
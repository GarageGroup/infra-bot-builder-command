using System;
using System.Collections.Generic;

namespace GGroupp.Infra.Bot.Builder;

public sealed record class BotInfoData
{
    public BotInfoData(IReadOnlyCollection<KeyValuePair<string, string?>> values)
        =>
        Values = values ?? Array.Empty<KeyValuePair<string, string?>>();

    public IReadOnlyCollection<KeyValuePair<string, string?>> Values { get; }
}
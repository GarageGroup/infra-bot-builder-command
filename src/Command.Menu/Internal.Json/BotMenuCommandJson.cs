using System;
using Newtonsoft.Json;

namespace GarageGroup.Infra.Bot.Builder;

internal sealed record class BotMenuCommandJson
{
    [JsonProperty("commandId")]
    public Guid? Id { get; init; }

    [JsonProperty("commandName")]
    public string? Name { get; init; }
}
using System;
using Microsoft.Bot.Builder;
using Newtonsoft.Json.Linq;

namespace GarageGroup.Infra.Bot.Builder;

partial class BotMenuActivity
{
    internal static Optional<string> GetCommandNameOrAbsent(this ITurnContext turnContext)
    {
        var value = turnContext.Activity.Value;
        if (value is not JObject jObject || jObject.HasValues is false)
        {
            return default;
        }

        var commandJson = jObject.ToObject<BotMenuCommandJson>();
        if (commandJson is null || commandJson.Id.HasValue is false || string.IsNullOrEmpty(commandJson.Name))
        {
            return default;
        }

        return Optional.Present(commandJson.Name);
    }
}
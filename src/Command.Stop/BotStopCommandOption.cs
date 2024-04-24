using System;
using System.Diagnostics.CodeAnalysis;

namespace GarageGroup.Infra.Bot.Builder;

public sealed record class BotStopCommandOption
{
    private const string DefaultSuccessText = "Operation was stopped";

    public BotStopCommandOption([AllowNull] string successText = DefaultSuccessText)
        =>
        SuccessText = successText.OrNullIfWhiteSpace() ?? DefaultSuccessText;

    public string SuccessText { get; }
}
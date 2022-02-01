using System.Diagnostics.CodeAnalysis;

namespace GGroupp.Infra.Bot.Builder;

public sealed record class BotStopCommandOption
{
    private const string DefaultSuccessText = "Operation was stopped";

    public BotStopCommandOption([AllowNull] string successText = DefaultSuccessText)
        =>
        SuccessText = string.IsNullOrEmpty(successText) ? DefaultSuccessText : successText;

    public string SuccessText { get; }
}
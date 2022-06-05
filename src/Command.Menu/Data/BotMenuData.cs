using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace GGroupp.Infra.Bot.Builder;

public sealed record class BotMenuData
{
    public BotMenuData([AllowNull] string text, [AllowNull] IReadOnlyCollection<BotMenuCommand> commands)
    {
        Text = text ?? string.Empty;
        Commands = commands ?? Array.Empty<BotMenuCommand>();
    }

    public string Text { get; }

    public IReadOnlyCollection<BotMenuCommand> Commands { get; }
}
using System;
using System.Diagnostics.CodeAnalysis;

namespace GarageGroup.Infra.Bot.Builder;

public sealed record class BotMenuData
{
    public BotMenuData([AllowNull] string text, FlatArray<BotMenuCommand> commands)
    {
        Text = text.OrEmpty();
        Commands = commands;
    }

    public string Text { get; }

    public FlatArray<BotMenuCommand> Commands { get; }
}
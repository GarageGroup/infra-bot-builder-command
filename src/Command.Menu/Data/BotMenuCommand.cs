using System;

namespace GGroupp.Infra.Bot.Builder;

public sealed record class BotMenuCommand
{
    public BotMenuCommand(Guid id, string name, string description)
    {
        Id = id;
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Description { get; }
}
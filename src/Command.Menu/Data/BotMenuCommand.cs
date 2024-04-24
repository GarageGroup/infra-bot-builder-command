using System;

namespace GarageGroup.Infra.Bot.Builder;

public sealed record class BotMenuCommand
{
    public BotMenuCommand(Guid id, string name, string description)
    {
        Id = id;
        Name = name.OrEmpty();
        Description = description.OrEmpty();
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Description { get; }
}
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace GGroupp.Infra.Bot.Builder;

partial class BotMenuActivity
{
    internal static IActivity CreateMenuActivity(this ITurnContext turnContext, BotMenuData menuData)
    {
        if (turnContext.IsCardSupported())
        {
            return CreateAdaptiveCardActivity(turnContext, menuData);
        }

        if (turnContext.IsTelegramChannel())
        {
            var text = BuildTelegramText(turnContext, menuData);
            return MessageFactory.Text(text);
        }

        return CreateHeroCardActivity(menuData);
    }

    private static string BuildTelegramText(ITurnContext turnContext, BotMenuData menuData)
    {
        var encodedText = turnContext.EncodeText(menuData.Text);
        if (menuData.Commands.Any() is false)
        {
            return encodedText;
        }

        var textBuilder = new StringBuilder().Append("**").Append(encodedText).Append("**");

        foreach (var command in menuData.Commands)
        {
            if (textBuilder.Length is not 0)
            {
                textBuilder.Append("\n\r").Append(LineSeparator).Append("\n\r");
            }

            var encodedCommandName = turnContext.EncodeText(command.Name);
            var encodedCommandDescription = turnContext.EncodeText(command.Description);

            if (string.IsNullOrEmpty(encodedCommandName) is false)
            {
                textBuilder.Append('/').Append(encodedCommandName);

                if (string.IsNullOrEmpty(encodedCommandDescription) is false)
                {
                    textBuilder.Append(" - ");
                }
            }

            textBuilder.Append(encodedCommandDescription);
        }

        return textBuilder.ToString();
    }

    private static IActivity CreateAdaptiveCardActivity(ITurnContext context, BotMenuData menuData)
        =>
        new Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = new AdaptiveCard(context.GetAdaptiveSchemaVersion())
            {
                Body = CreateBody(menuData),
                Actions = menuData.Commands.Where(HasDescription).Select(CreateAdaptiveSubmitAction).ToList<AdaptiveAction>()
            }
        }
        .ToActivity();

    private static IActivity CreateHeroCardActivity(BotMenuData menuData)
        =>
        new HeroCard
        {
            Title = menuData.Text,
            Buttons = menuData.Commands.Where(HasDescription).Select(CreateCommandAction).ToArray()
        }
        .ToAttachment()
        .ToActivity();

    private static List<AdaptiveElement> CreateBody(BotMenuData menuData)
    {
        if (string.IsNullOrEmpty(menuData.Text))
        {
            return Enumerable.Empty<AdaptiveElement>().ToList();
        }

        return new()
        {
            new AdaptiveTextBlock
            {
                Text = menuData.Text,
                Weight = AdaptiveTextWeight.Bolder,
                Wrap = true
            }
        };
    }

    private static AdaptiveSubmitAction CreateAdaptiveSubmitAction(BotMenuCommand command)
        =>
        new()
        {
            Title = command.Description,
            Data = ToActionValue(command)
        };

    private static CardAction CreateCommandAction(BotMenuCommand command)
        =>
        new(ActionTypes.PostBack)
        {
            Title = command.Description,
            Text = command.Description,
            Value = ToActionValue(command)
        };

    private static BotMenuCommandJson ToActionValue(BotMenuCommand command)
        =>
        new()
        {
            Id = command.Id,
            Name = command.Name
        };

    private static AdaptiveSchemaVersion GetAdaptiveSchemaVersion(this ITurnContext turnContext)
        =>
        turnContext.IsMsteamsChannel() ? AdaptiveCard.KnownSchemaVersion : new(1, 0);

    private static bool HasDescription(BotMenuCommand menuCommand)
        =>
        string.IsNullOrEmpty(menuCommand.Description) is false;
}
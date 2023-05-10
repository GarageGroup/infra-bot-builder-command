using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace GarageGroup.Infra.Bot.Builder;

partial class BotMenuActivity
{
    internal static IActivity CreateMenuActivity(this ITurnContext turnContext, BotMenuData menuData)
    {
        if (turnContext.IsCardSupported())
        {
            return CreateAdaptiveCardActivity(turnContext, menuData);
        }

        if (turnContext.IsNotTelegramChannel())
        {
            return CreateHeroCardActivity(menuData);
        }

        return CreateTelegramActivity(menuData);
    }

    private static IActivity CreateAdaptiveCardActivity(ITurnContext context, BotMenuData menuData)
        =>
        new Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = new AdaptiveCard(context.GetAdaptiveSchemaVersion())
            {
                Body = CreateBody(menuData),
                Actions = menuData.Commands.AsEnumerable().Where(HasDescription).Select(CreateAdaptiveSubmitAction).ToList<AdaptiveAction>()
            }
        }
        .ToActivity();

    private static IActivity CreateHeroCardActivity(BotMenuData menuData)
        =>
        new HeroCard
        {
            Title = menuData.Text,
            Buttons = menuData.Commands.AsEnumerable().Where(HasDescription).Select(CreateCommandAction).ToArray()
        }
        .ToAttachment()
        .ToActivity();

    private static IActivity CreateTelegramActivity(BotMenuData menuData)
    {
        var activity = MessageFactory.Text(default);

        var channelData = new TelegramChannelData(
            parameters: new TelegramParameters(BuildTelegramText(menuData))
            {
                ParseMode = TelegramParseMode.Html
            });

        activity.ChannelData = channelData.ToJObject();
        return activity;
    }

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

    private static string BuildTelegramText(BotMenuData menuData)
    {
        var encodedText = HttpUtility.HtmlEncode(menuData.Text);
        if (menuData.Commands.IsEmpty)
        {
            return encodedText;
        }

        var textBuilder = new StringBuilder();

        if (string.IsNullOrEmpty(menuData.Text) is false)
        {
            textBuilder = textBuilder.Append("<b>").Append(encodedText).Append("</b>");
        }

        foreach (var command in menuData.Commands)
        {
            if (textBuilder.Length is not 0)
            {
                textBuilder.Append("\n\r").Append(LineSeparator).Append("\n\r");
            }

            var encodedCommandName = HttpUtility.HtmlEncode(command.Name);
            var encodedCommandDescription = HttpUtility.HtmlEncode(command.Description);

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

    private static AdaptiveSchemaVersion GetAdaptiveSchemaVersion(this ITurnContext turnContext)
        =>
        turnContext.IsMsteamsChannel() ? AdaptiveCard.KnownSchemaVersion : new(1, 0);

    private static bool HasDescription(BotMenuCommand menuCommand)
        =>
        string.IsNullOrEmpty(menuCommand.Description) is false;
}
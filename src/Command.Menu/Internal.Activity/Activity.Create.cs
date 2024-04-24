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
            return CreateAdaptiveCard(turnContext, menuData).ToActivity();
        }

        if (turnContext.IsTelegramChannel())
        {
            return CreateTelegramParameters(menuData).BuildActivity();
        }

        return CreateHeroCard(menuData).ToAttachment().ToActivity();
    }

    private static Attachment CreateAdaptiveCard(ITurnContext context, BotMenuData menuData)
    {
        return new()
        {
            ContentType = AdaptiveCard.ContentType,
            Content = new AdaptiveCard(context.IsMsteamsChannel() ? AdaptiveCard.KnownSchemaVersion : new(1, 0))
            {
                Body = CreateBody(menuData),
                Actions = menuData.Commands.AsEnumerable().Where(HasDescription).Select(CreateAdaptiveSubmitAction).ToList<AdaptiveAction>()
            }
        };

        static List<AdaptiveElement> CreateBody(BotMenuData menuData)
        {
            if (string.IsNullOrEmpty(menuData.Text))
            {
                return [];
            }

            return
            [
                new AdaptiveTextBlock
                {
                    Text = menuData.Text,
                    Weight = AdaptiveTextWeight.Bolder,
                    Wrap = true
                }
            ];
        }

        static AdaptiveSubmitAction CreateAdaptiveSubmitAction(BotMenuCommand command)
            =>
            new()
            {
                Title = command.Description,
                Data = ToActionValue(command)
            };
    }

    private static HeroCard CreateHeroCard(BotMenuData menuData)
    {
        return new()
        {
            Title = menuData.Text,
            Buttons = menuData.Commands.AsEnumerable().Where(HasDescription).Select(CreateCommandAction).ToArray()
        };

        static CardAction CreateCommandAction(BotMenuCommand command)
            =>
            new(ActionTypes.PostBack)
            {
                Title = command.Description,
                Text = command.Description,
                Value = ToActionValue(command)
            };
    }

    private static TelegramParameters CreateTelegramParameters(BotMenuData menuData)
    {
        var encodedText = HttpUtility.HtmlEncode(menuData.Text);
        if (menuData.Commands.IsEmpty)
        {
            return new(encodedText)
            {
                ParseMode = TelegramParseMode.Html
            };
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

        return new(textBuilder.ToString())
        {
            ParseMode = TelegramParseMode.Html
        };
    }

    private static BotMenuCommandJson ToActionValue(BotMenuCommand command)
        =>
        new()
        {
            Id = command.Id,
            Name = command.Name
        };

    private static bool HasDescription(BotMenuCommand menuCommand)
        =>
        string.IsNullOrEmpty(menuCommand.Description) is false;
}
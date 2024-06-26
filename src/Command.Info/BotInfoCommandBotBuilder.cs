using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace GarageGroup.Infra.Bot.Builder;

public static class BotInfoCommandBotBuilder
{
    private const string DefaultCommandName = "info";

    public static IBotBuilder UseBotInfo(this IBotBuilder botBuilder, string commandName, Func<IBotContext, BotInfoData> botInfoResover)
    {
        ArgumentNullException.ThrowIfNull(botBuilder);
        ArgumentNullException.ThrowIfNull(botInfoResover);

        return botBuilder.Use(InnerInvokeAsync);

        ValueTask<Unit> InnerInvokeAsync(IBotContext botContext, CancellationToken cancellationToken)
            =>
            InvokeCommandAsync(
                botContext: botContext,
                commandName: commandName,
                botInfo: botInfoResover.Invoke(botContext),
                cancellationToken: cancellationToken);
    }

    private static ValueTask<Unit> InvokeCommandAsync(
        IBotContext botContext,
        [AllowNull] string commandName,
        [AllowNull] BotInfoData botInfo,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(botContext);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<Unit>(cancellationToken);
        }

        return InnerInvokeCommandAsync(
            botContext: botContext,
            commandName: string.IsNullOrEmpty(commandName) ? DefaultCommandName : commandName,
            botInfo: botInfo,
            cancellationToken: cancellationToken);
    }

    private static ValueTask<Unit> InnerInvokeCommandAsync(
        IBotContext botContext,
        string commandName,
        BotInfoData botInfo,
        CancellationToken cancellationToken)
    {
        return botContext.TurnContext.RecognizeCommandOrAbsent(commandName).FoldValueAsync(SendBotInfoAsync, NextAsync);

        async ValueTask<Unit> SendBotInfoAsync(string _)
        {
            var activity = botContext.TurnContext.BuildBotInfoActivity(botInfo);
            await botContext.TurnContext.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);

            return default;
        }

        ValueTask<Unit> NextAsync()
            =>
            botContext.BotFlow.NextAsync(cancellationToken);
    }

    private static Activity BuildBotInfoActivity(this ITurnContext turnContext, BotInfoData botInfo)
    {
        var text = string.Join(
            turnContext.GetLineSeparator(),
            botInfo.Values.AsEnumerable().Where(NotEmptyValue).Select(GetValueText));

        if (string.IsNullOrEmpty(text))
        {
            return MessageFactory.Text("Bot information is absent.");
        }

        if (turnContext.IsNotTelegramChannel())
        {
            return MessageFactory.Text(text);
        }

        return CreateTelegramParameters(text).BuildActivity();

        static bool NotEmptyValue(KeyValuePair<string, string?> item)
            =>
            string.IsNullOrEmpty(item.Value) is false;

        static string GetValueText(KeyValuePair<string, string?> item)
            =>
            $"{item.Key}: {item.Value}";
    }

    private static string GetLineSeparator(this ITurnContext turnContext)
    {
        if (turnContext.IsMsteamsChannel())
        {
            return "<br>";
        }

        return turnContext.IsTelegramChannel() ? "\n\r" : "\n\r\n\r";
    }

    private static TelegramParameters CreateTelegramParameters(string text)
        =>
        new(HttpUtility.HtmlEncode(text))
        {
            ParseMode = TelegramParseMode.Html
        };
}
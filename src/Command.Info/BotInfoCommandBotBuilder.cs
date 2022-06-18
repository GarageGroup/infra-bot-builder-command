using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace GGroupp.Infra.Bot.Builder;

public static class BotInfoCommandBotBuilder
{
    private const string DefaultCommandName = "info";

    public static IBotBuilder UseBotInfo(this IBotBuilder botBuilder, string commandName, Func<IBotContext, BotInfoData> botInfoResover)
    {
        _ = botBuilder ?? throw new ArgumentNullException(nameof(botBuilder));
        _ = botInfoResover ?? throw new ArgumentNullException(nameof(botInfoResover));

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
        _ = botContext ?? throw new ArgumentNullException(nameof(botContext));

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<Unit>(cancellationToken);
        }

        return InnerInvokeCommandAsync(
            botContext: botContext,
            commandName: string.IsNullOrEmpty(commandName) ? DefaultCommandName : commandName,
            botInfo: botInfo ?? new(Array.Empty<KeyValuePair<string, string?>>()),
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
            botContext.BotTelemetryClient.TrackEvent("Start", botContext.TurnContext.Activity);

            var activity = botContext.TurnContext.BuildBotInfoActivity(botInfo);
            await botContext.TurnContext.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);

            botContext.BotTelemetryClient.TrackEvent("Complete", botContext.TurnContext.Activity);
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
            botInfo.Values.Where(NotEmptyValue).Select(GetValueText));

        if (string.IsNullOrEmpty(text))
        {
            return MessageFactory.Text("Bot information is absent.");
        }

        if (turnContext.IsNotTelegramChannel())
        {
            return MessageFactory.Text(text);
        }

        var tgActivity = MessageFactory.Text(default);
        tgActivity.ChannelData = CreateTelegramChannelData(text).ToJObject();

        return tgActivity;

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

    private static TelegramChannelData CreateTelegramChannelData(string text)
        =>
        new(
            parameters: new TelegramParameters(HttpUtility.HtmlEncode(text))
            {
                ParseMode = TelegramParseMode.Html
            });

    private static void TrackEvent(this IBotTelemetryClient client, string eventName, IActivity activity)
    {
        const string flowId = "BotInfoGet";

        var properties = new Dictionary<string, string>
        {
            { "FlowId", flowId },
            { "InstanceId", activity.Id }
        };

        client.TrackEvent(flowId + eventName, properties);
    }
}
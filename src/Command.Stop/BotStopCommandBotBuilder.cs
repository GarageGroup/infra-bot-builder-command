using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace GGroupp.Infra.Bot.Builder;

using BotStopCommandOptionResolver = Func<IBotContext, BotStopCommandOption>;

public static class BotStopCommandBotBuilder
{
    private const string DefaultCommandName = "stop";

    public static IBotBuilder UseBotStop(this IBotBuilder botBuilder, string commandName, BotStopCommandOptionResolver optionResolver)
        =>
        InnerUseBotStop(
            botBuilder ?? throw new ArgumentNullException(nameof(botBuilder)),
            commandName,
            optionResolver ?? throw new ArgumentNullException(nameof(optionResolver)));

    public static IBotBuilder UseBotStop(this IBotBuilder botBuilder, string commandName, Func<BotStopCommandOption> optionFactory)
    {
        _ = botBuilder ?? throw new ArgumentNullException(nameof(botBuilder));
        _ = optionFactory ?? throw new ArgumentNullException(nameof(optionFactory));

        return InnerUseBotStop(botBuilder, commandName, ResolveOption);

        BotStopCommandOption ResolveOption(IBotContext _)
            =>
            optionFactory.Invoke();
    }

    private static IBotBuilder InnerUseBotStop(IBotBuilder botBuilder, string commandName, BotStopCommandOptionResolver optionResolver)
    {
        return botBuilder.Use(InnerInvokeAsync);

        ValueTask<Unit> InnerInvokeAsync(IBotContext botContext, CancellationToken cancellationToken)
            =>
            InvokeCommandAsync(botContext, commandName, optionResolver.Invoke(botContext), cancellationToken);
    }

    private static ValueTask<Unit> InvokeCommandAsync(
        IBotContext botContext,
        [AllowNull] string commandName,
        [AllowNull] BotStopCommandOption option,
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
            option: option ?? new(),
            cancellationToken: cancellationToken);
    }

    private static ValueTask<Unit> InnerInvokeCommandAsync(
        IBotContext botContext,
        string commandName,
        BotStopCommandOption option,
        CancellationToken cancellationToken)
    {
        return botContext.TurnContext.RecognizeCommandOrAbsent(commandName).FoldValueAsync(StopAsync, NextAsync);

        async ValueTask<Unit> StopAsync(string _)
        {
            botContext.BotTelemetryClient.TrackEvent("Start", botContext.TurnContext.Activity);
            await botContext.ConversationState.ClearStateAsync(botContext.TurnContext, cancellationToken).ConfigureAwait(false);

            var activity = MessageFactory.Text(option.SuccessText);
            if (botContext.TurnContext.IsTelegramChannel())
            {
                activity.ChannelData = CreateTelegramReplyKeyboardRemoveChannelData();
            }

            await botContext.TurnContext.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);
            botContext.BotTelemetryClient.TrackEvent("Complete", botContext.TurnContext.Activity);

            return default;
        }

        ValueTask<Unit> NextAsync()
            =>
            botContext.BotFlow.NextAsync(cancellationToken);
    }

    private static JObject CreateTelegramReplyKeyboardRemoveChannelData()
        =>
        new TelegramChannelData(
            parameters: new()
            {
                ReplyMarkup = new TelegramReplyKeyboardRemove()
            })
        .ToJObject();

    private static void TrackEvent(this IBotTelemetryClient client, string eventName, IActivity activity)
    {
        const string flowId = "BotCommandStop";

        var properties = new Dictionary<string, string>
        {
            { "FlowId", flowId },
            { "InstanceId", activity.Id },
        };

        client.TrackEvent(flowId + eventName, properties);
    }
}
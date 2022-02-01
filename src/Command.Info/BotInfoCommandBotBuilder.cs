using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
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
        return botContext.TurnContext.Activity.RecognizeCommandOrAbsent(commandName).FoldValueAsync(SendBotInfoAsync, NextAsync);

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
        var isTeams = string.Equals(turnContext.Activity.ChannelId, Channels.Msteams, StringComparison.InvariantCultureIgnoreCase);
        var separator = isTeams ? "<br>" : "\n\r\n\r";

        var text = string.Join(separator, botInfo.Values.Where(NotEmptyValue).Select(GetValueText));

        if (string.IsNullOrEmpty(text))
        {
            return MessageFactory.Text("Bot information is empty.");
        }

        return MessageFactory.Text(text);

        static bool NotEmptyValue(KeyValuePair<string, string?> item)
            =>
            string.IsNullOrEmpty(item.Value) is false;

        static string GetValueText(KeyValuePair<string, string?> item)
            =>
            $"{item.Key}: {item.Value}";
    }
}
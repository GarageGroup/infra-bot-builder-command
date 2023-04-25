using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

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
        ArgumentNullException.ThrowIfNull(botBuilder);
        ArgumentNullException.ThrowIfNull(optionFactory);

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
            await botContext.ConversationState.ClearStateAsync(botContext.TurnContext, cancellationToken).ConfigureAwait(false);

            var activity = botContext.TurnContext.CreateActivity(option);
            await botContext.TurnContext.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);

            return default;
        }

        ValueTask<Unit> NextAsync()
            =>
            botContext.BotFlow.NextAsync(cancellationToken);
    }

    private static IActivity CreateActivity(this ITurnContext turnContext, BotStopCommandOption option)
    {
        if (turnContext.IsNotTelegramChannel())
        {
            return MessageFactory.Text(option.SuccessText);
        }

        var tgActivity = MessageFactory.Text(default);
        
        var tgChannelData = new TelegramChannelData(
            parameters: new(HttpUtility.HtmlEncode(option.SuccessText))
            {
                ParseMode = TelegramParseMode.Html,
                ReplyMarkup = new TelegramReplyKeyboardRemove()
            });

        tgActivity.ChannelData = tgChannelData.ToJObject();
        return tgActivity;
    }
}
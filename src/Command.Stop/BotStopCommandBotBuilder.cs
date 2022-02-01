using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace GGroupp.Infra.Bot.Builder;

public static class BotStopCommandBotBuilder
{
    private const string DefaultCommandName = "stop";

    public static IBotBuilder UseBotStop(
        this IBotBuilder botBuilder,
        string commandName,
        Func<IBotContext, BotStopCommandOption> optionResolver)
        =>
        InnerUseBotStop(
            botBuilder ?? throw new ArgumentNullException(nameof(botBuilder)),
            commandName,
            optionResolver ?? throw new ArgumentNullException(nameof(optionResolver)));

    public static IBotBuilder UseBotStop(
        this IBotBuilder botBuilder,
        string commandName,
        Func<BotStopCommandOption> optionFactory)
    {
        _ = botBuilder ?? throw new ArgumentNullException(nameof(botBuilder));
        _ = optionFactory ?? throw new ArgumentNullException(nameof(optionFactory));

        return InnerUseBotStop(botBuilder, commandName, ResolveOption);

        BotStopCommandOption ResolveOption(IBotContext _)
            =>
            optionFactory.Invoke();
    }

    private static IBotBuilder InnerUseBotStop(
        IBotBuilder botBuilder,
        string commandName,
        Func<IBotContext, BotStopCommandOption> optionResolver)
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
        return botContext.TurnContext.Activity.RecognizeCommandOrAbsent(commandName).FoldValueAsync(StopAsync, NextAsync);

        async ValueTask<Unit> StopAsync(string _)
        {
            var user = await botContext.BotUserProvider.GetCurrentUserAsync(cancellationToken).ConfigureAwait(false);

            await botContext.UserState.ClearStateAsync(botContext.TurnContext, cancellationToken).ConfigureAwait(false);
            await botContext.ConversationState.ClearStateAsync(botContext.TurnContext, cancellationToken).ConfigureAwait(false);

            if (user is not null)
            {
                await botContext.BotUserProvider.SetCurrentUserAsync(user, cancellationToken).ConfigureAwait(false);
            }

            var activity = MessageFactory.Text(option.SuccessText);
            if (botContext.TurnContext.Activity.IsTelegram())
            {
                activity = activity.SetReplyTelegramKeyboardRemoveChannelData();
            }

            await botContext.TurnContext.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);
            return default;
        }

        ValueTask<Unit> NextAsync()
            =>
            botContext.BotFlow.NextAsync(cancellationToken);
    }
}
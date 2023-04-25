using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace GGroupp.Infra.Bot.Builder;

internal static partial class MenuBotMiddleware
{
    private static async ValueTask<Unit> StartWithCommandAsync(
        this IBotContext botContext, string command, CancellationToken cancellationToken)
    {
        var removeMenuTask = RemoveMenuAsync();
        var startTask = StartAsync();

        await Task.WhenAll(removeMenuTask, startTask).ConfigureAwait(false);
        return default;

        async Task RemoveMenuAsync()
        {
            var menuResourceAccessor = botContext.GetMenuResourceAccessor();

            await botContext.RemoveMenuResourceAsync(menuResourceAccessor, cancellationToken).ConfigureAwait(false);
            await menuResourceAccessor.DeleteAsync(botContext.TurnContext, cancellationToken).ConfigureAwait(false);

            var menuIdAccessor = botContext.GetMenuIdAccessor();
            await menuIdAccessor.DeleteAsync(botContext.TurnContext, cancellationToken).ConfigureAwait(false);
        }

        Task StartAsync()
        {
            var activity = botContext.TurnContext.Activity;

            activity.Text = command;
            activity.Value = null;

            return botContext.BotFlow.StartAsync(activity, cancellationToken).AsTask();
        }
    }

    private static async ValueTask<Unit> ReplaceMenuActivityAsync(
        this IBotContext botContext, IActivity menuActivity, CancellationToken cancellationToken)
    {
        var menuResourceAccessor = botContext.GetMenuResourceAccessor();

        var removeMenuTask = botContext.RemoveMenuResourceAsync(menuResourceAccessor, cancellationToken);
        var sendMenuTask = botContext.TurnContext.SendActivityAsync(menuActivity, cancellationToken);

        await Task.WhenAll(removeMenuTask, sendMenuTask).ConfigureAwait(false);
        await menuResourceAccessor.SetAsync(botContext.TurnContext, sendMenuTask.Result, cancellationToken).ConfigureAwait(false);

        return default;
    }

    private static async Task RemoveMenuResourceAsync(
        this IBotContext botContext, IStatePropertyAccessor<ResourceResponse?> menuResourceAccessor, CancellationToken cancellationToken)
    {
        if (botContext.TurnContext.IsWebchatChannel() || botContext.TurnContext.IsEmulatorChannel())
        {
            return;
        }

        var previewActivity = await menuResourceAccessor.GetAsync(botContext.TurnContext, default, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(previewActivity?.Id))
        {
            return;
        }

        await botContext.TurnContext.DeleteActivityAsync(previewActivity.Id, cancellationToken).ConfigureAwait(false);
    }

    private static IStatePropertyAccessor<ResourceResponse?> GetMenuResourceAccessor(this IBotContext botContext)
        =>
        botContext.ConversationState.CreateProperty<ResourceResponse?>("__botMenuResource");

    private static IStatePropertyAccessor<Guid> GetMenuIdAccessor(this IBotContext botContext)
        =>
        botContext.ConversationState.CreateProperty<Guid>("__botMenuId");
}
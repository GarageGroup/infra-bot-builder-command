using System;
using System.Threading;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

partial class MenuBotMiddleware
{
    internal static ValueTask<Unit> InvokeCommandAsync(this IBotContext botContext, BotMenuData menuData, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(botContext);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<Unit>(cancellationToken);
        }

        return InnerInvokeCommandAsync(botContext, menuData, cancellationToken);
    }

    private static ValueTask<Unit> InnerInvokeCommandAsync(IBotContext botContext, BotMenuData menuData, CancellationToken cancellationToken)
    {
        var turnContext = botContext.TurnContext;
        if (turnContext.IsNotMessageType())
        {
            return botContext.BotFlow.NextAsync(cancellationToken);
        }

        return turnContext.GetCommandNameOrAbsent().FoldValueAsync(OnCommandButtonAsync, SendMenuAsync);

        ValueTask<Unit> OnCommandButtonAsync(string command)
            =>
            botContext.StartWithCommandAsync(command, cancellationToken);

        async ValueTask<Unit> SendMenuAsync()
        {
            var menuId = Guid.NewGuid();
            await botContext.GetMenuIdAccessor().SetAsync(botContext.TurnContext, menuId, cancellationToken).ConfigureAwait(false);

            var menuActivity = botContext.TurnContext.CreateMenuActivity(menuData);
            return await botContext.ReplaceMenuActivityAsync(menuActivity, cancellationToken).ConfigureAwait(false);
        }
    }
}
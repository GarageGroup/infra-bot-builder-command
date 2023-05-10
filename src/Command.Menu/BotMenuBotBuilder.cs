using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Infra.Bot.Builder;

public static class BotMenuBotBuilder
{
    public static IBotBuilder UseBotMenu(this IBotBuilder botBuilder, BotMenuData menuData)
    {
        ArgumentNullException.ThrowIfNull(botBuilder);
        ArgumentNullException.ThrowIfNull(menuData);

        return botBuilder.Use(InnerInvokeAsync);

        ValueTask<Unit> InnerInvokeAsync(IBotContext botContext, CancellationToken cancellationToken)
            =>
            botContext.InvokeCommandAsync(menuData, cancellationToken);
    }
}
using System.Text;
using KickChatOverlay.Models;

namespace KickChatOverlay.Services;

public static class BotrixUrlBuilder
{
    public static Uri Build(AppSettings s)
    {
        var q = new StringBuilder("https://botrix.live/widgets/chat/?");
        q.Append($"bid={Uri.EscapeDataString(s.BotrixBotId)}");
        q.Append("&theme=transparent");
        q.Append($"&platformIcon={BoolStr(s.BotrixShowPlatformIcon)}");
        q.Append($"&bots={BoolStr(s.BotrixShowBots)}");
        q.Append($"&emojis={BoolStr(s.BotrixShowEmojis)}");
        q.Append($"&hideCommands={BoolStr(s.BotrixHideCommands)}");
        q.Append($"&hideMessages={BoolStr(s.BotrixHideMessages)}");
        q.Append($"&hideMessagesSeconds={s.BotrixHideMessagesSeconds}");
        q.Append($"&widgetSize={s.BotrixWidgetSize}");
        q.Append($"&streamTogether={BoolStr(s.BotrixStreamTogether)}");
        q.Append($"&cheer={BoolStr(s.BotrixCheer)}");
        q.Append($"&pointsReward={BoolStr(s.BotrixPointsReward)}");
        q.Append($"&showTimestamp={BoolStr(s.BotrixShowTimestamp)}");
        q.Append($"&shadowThickness={s.BotrixShadowThickness}");
        q.Append("&animation=fadeIn");
        q.Append("&kick=true");
        return new Uri(q.ToString());
    }

    private static string BoolStr(bool v) => v ? "true" : "false";
}

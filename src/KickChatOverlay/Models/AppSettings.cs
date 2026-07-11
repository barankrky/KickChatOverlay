using System.IO;
using System.Text.Json;

namespace KickChatOverlay.Models;

public sealed class AppSettings
{
    // Botrix widget settings
    public string BotrixBotId { get; set; } = "6XCA19I8ALN1vPxcz3rR1Q";
    public bool BotrixShowPlatformIcon { get; set; } = false;
    public bool BotrixShowBots { get; set; } = false;
    public bool BotrixShowEmojis { get; set; } = true;
    public bool BotrixHideCommands { get; set; } = false;
    public bool BotrixHideMessages { get; set; } = true;
    public int BotrixHideMessagesSeconds { get; set; } = 10;
    public int BotrixWidgetSize { get; set; } = 21;
    public bool BotrixStreamTogether { get; set; } = true;
    public bool BotrixCheer { get; set; } = true;
    public bool BotrixPointsReward { get; set; } = false;
    public bool BotrixShowTimestamp { get; set; } = false;
    public int BotrixShadowThickness { get; set; } = 1;

    // Overlay window
    public string BackgroundColor { get; set; } = "#00000000";
    public double WindowLeft { get; set; } = 100;
    public double WindowTop { get; set; } = 100;
    public double WindowWidth { get; set; } = 350;
    public double WindowHeight { get; set; } = 600;
    public string Language { get; set; } = "tr";

    private static readonly string SettingsPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsPath)) return new AppSettings();
        var json = File.ReadAllText(SettingsPath);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}

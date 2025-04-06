using Microsoft.CommandPalette.Extensions.Toolkit;

namespace YTDLPExtension;

public class ExtensionSettings : JsonSettingsManager {
    public static readonly ExtensionSettings Instance = new();

    public readonly TextSetting OutputPath = new(
        "outputPath",
        "Output path",
        "Output path",
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\VideoDownloads"
    );

    public readonly TextSetting YtdlpArgs = new(
        "ytdlpArgs",
        "Arguments to yt-dlp",
        "Arguments to yt-dlp",
        "--no-warnings --no-check-certificate"
    );

    public readonly TextSetting YtdlpOutput = new(
        "ytdlpOutput",
        "Output template",
        "Output template",
        "%(title)s.%(ext)s"
    );

    public readonly TextSetting YtdlpPath = new(
        "ytdlpPath",
        "Path to yt-dlp executable",
        "Path to yt-dlp executable",
        "ytdlp.exe"
    );


    public ExtensionSettings() {
        FilePath = SettingsJsonPath();
        Settings.Add(YtdlpPath);
        Settings.Add(YtdlpArgs);
        Settings.Add(YtdlpOutput);
        Settings.Add(OutputPath);
        
        LoadSettings();
        
        Settings.SettingsChanged += (s, a) => SaveSettings();
    }

    internal static string SettingsJsonPath() {
        var directory = Utilities.BaseSettingsPath("mantikafasi.YTDLP");
        Directory.CreateDirectory(directory);

        // now, the state is just next to the exe
        return Path.Combine(directory, "settings.json");
    }
}
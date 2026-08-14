using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tetris.App;

/// <summary>アプリ終了後も保持するユーザー設定（ミュート状態）。</summary>
public sealed class UserSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    [JsonPropertyName("muted")]
    public bool Muted { get; set; }

    [JsonIgnore]
    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AIAUTetris",
        "settings.json");

    public static UserSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"設定を読み込めなかったため既定値を使う: {ex.Message}");
        }

        return new UserSettings();
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(FilePath);
            if (directory is not null)
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"設定を保存できなかった: {ex.Message}");
        }
    }
}

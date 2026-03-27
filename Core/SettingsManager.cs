using System;
using System.IO;
using System.Text.Json;

namespace WenBrowser.Core;

public enum AppTheme {
    Default,
    VSDark,
    OfficeWhite,
    Transparent,
    Pink
}

public class AppSettings
{
    public double DefaultOpacity { get; set; } = 1.0;
    public string HomeUrl { get; set; } = "https://wen-browser-web.vercel.app/";
    public bool AutoHideInTaskbar { get; set; } = true;
    public AppTheme CurrentTheme { get; set; } = AppTheme.Default;

    // --- 终极隐蔽扩展 ---
    public bool AutoFadeOnBlur { get; set; } = false; // 失去焦点自动淡化
    public bool IsGreyscale { get; set; } = false;      // 全局黑白模式
    public bool IsCodeMode { get; set; } = false;       // 网页代码化伪装模式
    public double StealthOpacity { get; set; } = 0.15;   // 失去焦点时的透明度数值 (0.0 - 1.0)
    public string FakeTitle { get; set; } = "Wen 浏览器"; 
    
    // --- 字体配置 ---
    public bool UseCustomFont { get; set; } = false;
    public string CustomFontFamily { get; set; } = "Segoe UI";
    
    // --- 搜索引擎配置 ---
    public string SearchEngineName { get; set; } = "Bing";
    public string SearchEngineUrl { get; set; } = "https://www.bing.com/search?q=";
}

public static class SettingsManager
{
    private static readonly string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    public static AppSettings Current { get; private set; } = new();

    static SettingsManager()
    {
        Load();
    }

    public static void Load()
    {
        try {
            if (File.Exists(SettingsPath)) {
                string json = File.ReadAllText(SettingsPath);
                Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new();
                if (Current.HomeUrl.Contains(".top") || Current.HomeUrl.Contains("bing.com")) {
                    Current.HomeUrl = "https://wen-browser-web.vercel.app/";
                    Save();
                }
            }
        } catch { }
    }

    public static void Save()
    {
        try {
            string json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        } catch { }
    }
}

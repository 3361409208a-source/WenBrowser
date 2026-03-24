using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Collections.Generic;

namespace MoyuBrowser.Core;

public static class FontManager
{
    private static readonly PrivateFontCollection _privateFontCollection = new();
    private static readonly Dictionary<string, string> _availableFonts = new();
    private static readonly Dictionary<string, string> _familyToPath = new();

    public static IEnumerable<string> AvailableFontNames => _availableFonts.Keys;
    public static string? GetFontPath(string familyName) => _familyToPath.TryGetValue(familyName, out var path) ? path : null;

    static FontManager()
    {
        LoadFonts();
    }

    private static void LoadFonts()
    {
        string fontsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fonts");
        if (!Directory.Exists(fontsDir)) return;

        var fontFiles = Directory.GetFiles(fontsDir, "*.ttf");
        foreach (var file in fontFiles)
        {
            try
            {
                var collection = new PrivateFontCollection();
                collection.AddFontFile(file);
                _privateFontCollection.AddFontFile(file);
                
                foreach (var family in collection.Families)
                {
                    if (!_availableFonts.ContainsKey(family.Name))
                    {
                        _availableFonts.Add(family.Name, family.Name);
                        _familyToPath[family.Name] = Path.GetFileName(file);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load font {file}: {ex.Message}");
            }
        }
    }

    public static Font GetFont(string familyName, float size, FontStyle style = FontStyle.Regular)
    {
        if (string.IsNullOrEmpty(familyName) || familyName == "Segoe UI")
        {
            return new Font("Segoe UI", size, style);
        }

        foreach (var family in _privateFontCollection.Families)
        {
            if (family.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase))
            {
                return new Font(family, size, style);
            }
        }

        return new Font("Segoe UI", size, style);
    }

    public static Font GetDefaultFont(float size)
    {
        if (SettingsManager.Current.UseCustomFont && !string.IsNullOrEmpty(SettingsManager.Current.CustomFontFamily))
        {
            return GetFont(SettingsManager.Current.CustomFontFamily, size);
        }
        return new Font("Segoe UI", size);
    }
}

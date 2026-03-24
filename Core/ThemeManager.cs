using System.Drawing;

namespace MoyuBrowser.Core;

public class ThemeColors
{
    public Color HeaderBg { get; set; }
    public Color ContentBg { get; set; }
    public Color TabActive { get; set; }
    public Color TabInactive { get; set; }
    public Color TextColor { get; set; }
    public Color BorderColor { get; set; }
}

public static class ThemeManager
{
    public static ThemeColors GetColors(AppTheme theme)
    {
        return theme switch
        {
            AppTheme.VSDark => new ThemeColors
            {
                HeaderBg = Color.FromArgb(30, 30, 33),
                ContentBg = Color.FromArgb(30, 30, 30),
                TabActive = Color.FromArgb(37, 37, 38),
                TabInactive = Color.Transparent,
                TextColor = Color.FromArgb(200, 200, 200),
                BorderColor = Color.FromArgb(45, 45, 48)
            },
            AppTheme.OfficeWhite => new ThemeColors
            {
                HeaderBg = Color.FromArgb(243, 243, 243),
                ContentBg = Color.White,
                TabActive = Color.White,
                TabInactive = Color.FromArgb(230, 230, 230),
                TextColor = Color.FromArgb(64, 64, 64),
                BorderColor = Color.FromArgb(210, 210, 210)
            },
            AppTheme.Transparent => new ThemeColors
            {
                HeaderBg = Color.FromArgb(20, 0, 0, 0),
                ContentBg = Color.FromArgb(10, 0, 0, 0),
                TabActive = Color.FromArgb(40, 255, 255, 255),
                TabInactive = Color.Transparent,
                TextColor = Color.White,
                BorderColor = Color.FromArgb(30, 255, 255, 255)
            },
            _ => new ThemeColors // Default Dark
            {
                HeaderBg = Color.FromArgb(22, 22, 26),
                ContentBg = Color.FromArgb(30,30,34),
                TabActive = Color.FromArgb(40, 40, 45),
                TabInactive = Color.Transparent,
                TextColor = Color.Gray,
                BorderColor = Color.FromArgb(50, 50, 55)
            }
        };
    }
}

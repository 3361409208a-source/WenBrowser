using System.Drawing;

namespace WenBrowser.Core;

public class ThemeColors
{
    public Color HeaderBg { get; set; }
    public Color ContentBg { get; set; }
    public Color TabActive { get; set; }
    public Color TabInactive { get; set; }
    public Color TextColor { get; set; }
    public Color BorderColor { get; set; }
    public Color InputBg { get; set; }
    public Color HoverColor { get; set; }
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
                BorderColor = Color.FromArgb(45, 45, 48),
                InputBg = Color.FromArgb(40, 40, 44),
                HoverColor = Color.FromArgb(50, 50, 55)
            },
            AppTheme.OfficeWhite => new ThemeColors
            {
                HeaderBg = Color.FromArgb(243, 243, 243),
                ContentBg = Color.White,
                TabActive = Color.White,
                TabInactive = Color.FromArgb(230, 230, 230),
                TextColor = Color.FromArgb(64, 64, 64),
                BorderColor = Color.FromArgb(210, 210, 210),
                InputBg = Color.FromArgb(235, 235, 235),
                HoverColor = Color.FromArgb(220, 220, 220)
            },
            AppTheme.Transparent => new ThemeColors
            {
                HeaderBg = Color.FromArgb(40, 40, 45),
                ContentBg = Color.FromArgb(20, 20, 25),
                TabActive = Color.FromArgb(60, 60, 65),
                TabInactive = Color.Transparent,
                TextColor = Color.White,
                BorderColor = Color.FromArgb(45, 45, 50),
                InputBg = Color.FromArgb(40, 40, 44),
                HoverColor = Color.FromArgb(60, 60, 65)
            },
            AppTheme.Pink => new ThemeColors
            {
                HeaderBg = Color.FromArgb(255, 240, 245),
                ContentBg = Color.White,
                TabActive = Color.White,
                TabInactive = Color.FromArgb(255, 225, 235),
                TextColor = Color.FromArgb(140, 80, 100),
                BorderColor = Color.FromArgb(255, 210, 225),
                InputBg = Color.FromArgb(255, 230, 240),
                HoverColor = Color.FromArgb(255, 215, 230)
            },
            _ => new ThemeColors 
            {
                HeaderBg = Color.FromArgb(28, 28, 32),
                ContentBg = Color.FromArgb(20, 20, 23),
                TabActive = Color.FromArgb(45, 45, 52),
                TabInactive = Color.Transparent,
                TextColor = Color.FromArgb(220, 220, 225),
                BorderColor = Color.FromArgb(40, 40, 45),
                InputBg = Color.FromArgb(40, 40, 44),
                HoverColor = Color.FromArgb(50, 50, 55)
            }
        };
    }
}

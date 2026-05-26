using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace MarkTogether.Client.UI
{
    /// <summary>
    /// Design system tập trung — toàn bộ form trong app dùng các giá trị ở đây
    /// để đảm bảo nhất quán về màu sắc, typography và spacing.
    /// </summary>
    public static class AppTheme
    {
        // ─── Color palette (clean UI, modern) ─────────────────────
        public static readonly Color Primary = Color.FromArgb(0x25, 0x63, 0xEB);        // #2563EB
        public static readonly Color PrimaryLight = Color.FromArgb(0xDB, 0xEA, 0xFE);   // #DBEAFE
        public static readonly Color PrimaryHover = Color.FromArgb(0x1D, 0x4E, 0xD8);
        public static readonly Color PrimaryPressed = Color.FromArgb(0x1E, 0x40, 0xAF);

        public static readonly Color Secondary = Color.FromArgb(0x14, 0xB8, 0xA6);      // #14B8A6
        public static readonly Color SecondaryHover = Color.FromArgb(0x0D, 0x9A, 0x88);

        public static readonly Color Background = Color.FromArgb(0xF8, 0xFA, 0xFC);     // #F8FAFC
        public static readonly Color Surface = Color.White;                              // #FFFFFF
        public static readonly Color SurfaceElevated = Color.FromArgb(0xFF, 0xFF, 0xFF); // card nổi

        public static readonly Color TextPrimary = Color.FromArgb(0x0F, 0x17, 0x2A);    // #0F172A
        public static readonly Color TextSecondary = Color.FromArgb(0x64, 0x74, 0x8B);  // #64748B
        public static readonly Color TextOnPrimary = Color.White;
        public static readonly Color TextDisabled = Color.FromArgb(0x94, 0xA3, 0xB8);
        public static readonly Color TextMuted = Color.FromArgb(0x94, 0xA3, 0xB8);      // #94A3B8

        public static readonly Color Success = Color.FromArgb(0x22, 0xC5, 0x5E);        // #22C55E
        public static readonly Color SuccessLight = Color.FromArgb(0xDC, 0xFC, 0xE7);   // #DCFCE7
        public static readonly Color SuccessHover = Color.FromArgb(0x16, 0xA3, 0x4A);
        public static readonly Color Warning = Color.FromArgb(0xF5, 0x9E, 0x0B);        // #F59E0B
        public static readonly Color WarningLight = Color.FromArgb(0xFE, 0xF3, 0xC7);   // #FEF3C7
        public static readonly Color Error = Color.FromArgb(0xEF, 0x44, 0x44);          // #EF4444
        public static readonly Color ErrorLight = Color.FromArgb(0xFE, 0xF2, 0xF2);     // #FEF2F2
        public static readonly Color ErrorHover = Color.FromArgb(0xDC, 0x26, 0x26);

        public static readonly Color Border = Color.FromArgb(0xE2, 0xE8, 0xF0);         // #E2E8F0
        public static readonly Color BorderFocus = Primary;
        public static readonly Color BorderLight = Color.FromArgb(0xF1, 0xF5, 0xF9);    // #F1F5F9
        public static readonly Color Divider = Color.FromArgb(0xF1, 0xF5, 0xF9);

        public static readonly Color HoverFill = Color.FromArgb(0xF1, 0xF5, 0xF9);
        public static readonly Color SelectedFill = Color.FromArgb(0xDB, 0xEA, 0xFE);

        // Shadow simulation (dùng cho border nhẹ tạo cảm giác nổi)
        public static readonly Color Shadow = Color.FromArgb(20, 0, 0, 0);              // 8% opacity black
        public static readonly Color ShadowMedium = Color.FromArgb(40, 0, 0, 0);

        // ─── Typography ───────────────────────────────────────────
        public const string FontFamilyName = "Segoe UI";

        // Tự dò font ưa thích trên máy user, fallback Segoe UI
        public static string ResolvedFontFamily { get; } = ResolvePreferredFontFamily();

        public static readonly Font H1 = new Font(ResolvedFontFamily, 22f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font H2 = new Font(ResolvedFontFamily, 16f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font H3 = new Font(ResolvedFontFamily, 13f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font Subtitle = new Font(ResolvedFontFamily, 11f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font Body = new Font(ResolvedFontFamily, 10f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font BodyBold = new Font(ResolvedFontFamily, 10f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font BodyLarge = new Font(ResolvedFontFamily, 11f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font Caption = new Font(ResolvedFontFamily, 9f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font CaptionBold = new Font(ResolvedFontFamily, 9f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font Button = new Font(ResolvedFontFamily, 10f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font ButtonSmall = new Font(ResolvedFontFamily, 9f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font Mono = new Font("Consolas", 10f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font MonoLarge = new Font("Consolas", 12f, FontStyle.Bold, GraphicsUnit.Point);

        // ─── Spacing scale (px) ───────────────────────────────────
        public const int SpaceXs = 4;
        public const int SpaceSm = 8;
        public const int SpaceMd = 12;
        public const int SpaceLg = 16;
        public const int SpaceXl = 24;
        public const int Space2Xl = 32;
        public const int Space3Xl = 48;

        // ─── Sizing ───────────────────────────────────────────────
        public const int InputHeight = 38;
        public const int ButtonHeight = 40;
        public const int ButtonHeightSmall = 32;
        public const int CornerRadius = 0;
        public const int CornerRadiusLg = 0;
        public const int CornerRadiusSm = 0;
        public const int ToolbarHeight = 56;

        // ─── Setup chung cho mọi Form ─────────────────────────────
        public static void ApplyToForm(Form form)
        {
            if (form == null) return;
            form.BackColor = Background;
            form.Font = Body;
            form.ForeColor = TextPrimary;
            form.AutoScaleMode = AutoScaleMode.Dpi;
        }

        /// <summary>
        /// Apply theme cho dialog form (nhỏ, fixed size)
        /// </summary>
        public static void ApplyToDialog(Form form)
        {
            if (form == null) return;
            form.BackColor = Surface;
            form.Font = Body;
            form.ForeColor = TextPrimary;
            form.AutoScaleMode = AutoScaleMode.Dpi;
        }

        private static string ResolvePreferredFontFamily()
        {
            var preferred = new[] { "Inter", "Be Vietnam Pro", "SF Pro Display", "Roboto", "Segoe UI" };
            try
            {
                var installed = new InstalledFontCollection().Families.Select(f => f.Name).ToHashSet();
                foreach (var name in preferred)
                {
                    if (installed.Contains(name)) return name;
                }
            }
            catch { }
            return "Segoe UI";
        }
    }
}

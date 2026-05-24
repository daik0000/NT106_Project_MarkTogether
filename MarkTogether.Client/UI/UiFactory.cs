using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MarkTogether.Client.UI
{
    /// <summary>
    /// Factory tạo các control thống nhất theo AppTheme:
    /// PrimaryButton, SecondaryButton, GhostButton, Card, TextField, FormHeader,
    /// Divider, Badge, ListView styled, GroupBox styled, ComboBox styled, ...
    /// </summary>
    public static class UiFactory
    {
        // ═══════════════════════════════════════════════════════════
        //  BUTTONS
        // ═══════════════════════════════════════════════════════════
        public enum ButtonVariant { Primary, Secondary, Ghost, Danger, Success }

        public static Button CreateButton(string text, ButtonVariant variant = ButtonVariant.Primary,
            int width = 140, int height = 0)
        {
            var btn = new Button
            {
                Text = text,
                Font = AppTheme.Button,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                AutoSize = false,
                Height = height > 0 ? height : AppTheme.ButtonHeight,
                Width = width,
                TabStop = true
            };
            btn.FlatAppearance.BorderSize = 0;
            ApplyButtonVariant(btn, variant);
            ApplyRoundedRegion(btn, AppTheme.CornerRadius);
            btn.Resize += (s, e) => ApplyRoundedRegion(btn, AppTheme.CornerRadius);
            return btn;
        }

        public static void ApplyButtonVariant(Button btn, ButtonVariant variant)
        {
            switch (variant)
            {
                case ButtonVariant.Primary:
                    btn.BackColor = AppTheme.Primary;
                    btn.ForeColor = AppTheme.TextOnPrimary;
                    btn.FlatAppearance.MouseOverBackColor = AppTheme.PrimaryHover;
                    btn.FlatAppearance.MouseDownBackColor = AppTheme.PrimaryPressed;
                    btn.FlatAppearance.BorderSize = 0;
                    break;
                case ButtonVariant.Secondary:
                    btn.BackColor = AppTheme.Surface;
                    btn.ForeColor = AppTheme.Primary;
                    btn.FlatAppearance.MouseOverBackColor = AppTheme.HoverFill;
                    btn.FlatAppearance.BorderSize = 1;
                    btn.FlatAppearance.BorderColor = AppTheme.Border;
                    break;
                case ButtonVariant.Ghost:
                    btn.BackColor = AppTheme.Background;
                    btn.ForeColor = AppTheme.TextSecondary;
                    btn.FlatAppearance.MouseOverBackColor = AppTheme.HoverFill;
                    btn.FlatAppearance.BorderSize = 0;
                    break;
                case ButtonVariant.Danger:
                    btn.BackColor = AppTheme.Error;
                    btn.ForeColor = AppTheme.TextOnPrimary;
                    btn.FlatAppearance.MouseOverBackColor = AppTheme.ErrorHover;
                    btn.FlatAppearance.BorderSize = 0;
                    break;
                case ButtonVariant.Success:
                    btn.BackColor = AppTheme.Success;
                    btn.ForeColor = AppTheme.TextOnPrimary;
                    btn.FlatAppearance.MouseOverBackColor = AppTheme.SuccessHover;
                    btn.FlatAppearance.BorderSize = 0;
                    break;
            }
        }

        public static void StylePrimaryButton(Button btn) =>
            DressButton(btn, ButtonVariant.Primary);

        public static void StyleSecondaryButton(Button btn) =>
            DressButton(btn, ButtonVariant.Secondary);

        public static void StyleGhostButton(Button btn) =>
            DressButton(btn, ButtonVariant.Ghost);

        public static void StyleDangerButton(Button btn) =>
            DressButton(btn, ButtonVariant.Danger);

        public static void StyleSuccessButton(Button btn) =>
            DressButton(btn, ButtonVariant.Success);

        private static void DressButton(Button btn, ButtonVariant variant)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = AppTheme.Button;
            btn.Cursor = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
            ApplyButtonVariant(btn, variant);
            if (btn.Height < AppTheme.ButtonHeightSmall) btn.Height = AppTheme.ButtonHeightSmall;
            ApplyRoundedRegion(btn, AppTheme.CornerRadius);
            btn.Resize -= ButtonResizeRound;
            btn.Resize += ButtonResizeRound;
        }

        private static void ButtonResizeRound(object sender, EventArgs e)
        {
            if (sender is Button b) ApplyRoundedRegion(b, AppTheme.CornerRadius);
        }

        public static void ApplyRoundedRegion(Control ctrl, int radius)
        {
            if (ctrl == null || ctrl.Width <= 0 || ctrl.Height <= 0) return;
            using (var path = CreateRoundedRectanglePath(ctrl.ClientRectangle, radius))
            {
                ctrl.Region = new Region(path);
            }
        }

        public static GraphicsPath CreateRoundedRectanglePath(Rectangle bounds, int radius)
        {
            int diameter = Math.Max(1, radius * 2);
            var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
            var path = new GraphicsPath();
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ═══════════════════════════════════════════════════════════
        //  INPUT PANELS (border without Region clipping)
        // ═══════════════════════════════════════════════════════════
        /// <summary>
        /// Style một Panel wrapper cho TextBox: vẽ viền bo góc mà KHÔNG clip Region.
        /// Tránh lỗi border bị mất nét do Region cắt pixel ở viền.
        /// </summary>
        public static void StyleInputPanel(Panel panel)
        {
            panel.BackColor = AppTheme.Surface;
            panel.Paint += (s, e) =>
            {
                DrawBorder(e.Graphics, panel.ClientRectangle, AppTheme.Border, AppTheme.CornerRadius);
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  TEXT FIELDS
        // ═══════════════════════════════════════════════════════════
        public static void StyleTextBoxFlat(TextBox tb)
        {
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.Font = AppTheme.Body;
            tb.ForeColor = AppTheme.TextPrimary;
            tb.BackColor = AppTheme.Surface;
            if (tb.Height < AppTheme.InputHeight) tb.Height = AppTheme.InputHeight;
        }

        /// <summary>
        /// Style một TextBox với viền bo góc, focus highlight.
        /// TextBox phải nằm trong một Panel wrapper.
        /// </summary>
        public static void StyleTextBoxWithPanel(Panel panel, TextBox tb)
        {
            panel.BackColor = AppTheme.Surface;
            panel.Padding = new Padding(AppTheme.SpaceMd, 0, AppTheme.SpaceMd, 0);

            tb.BorderStyle = BorderStyle.None;
            tb.Font = AppTheme.Body;
            tb.ForeColor = AppTheme.TextPrimary;
            tb.BackColor = AppTheme.Surface;
            tb.Dock = DockStyle.Fill;

            StyleAsCard(panel, AppTheme.CornerRadius);

            tb.GotFocus += (s, e) => { panel.Tag = "focus"; panel.Invalidate(); };
            tb.LostFocus += (s, e) => { panel.Tag = null; panel.Invalidate(); };
            panel.Paint += (s, e) =>
            {
                bool focused = panel.Tag as string == "focus";
                DrawBorder(e.Graphics, panel.ClientRectangle,
                    focused ? AppTheme.BorderFocus : AppTheme.Border,
                    AppTheme.CornerRadius);
            };
        }

        /// <summary>
        /// Style một TextBox đơn giản (có border, bo góc nhẹ) — dùng cho form dialog nhỏ.
        /// </summary>
        public static void StyleTextBox(TextBox tb)
        {
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.Font = AppTheme.BodyLarge;
            tb.ForeColor = AppTheme.TextPrimary;
            tb.BackColor = AppTheme.Surface;
        }

        // ═══════════════════════════════════════════════════════════
        //  COMBOBOX
        // ═══════════════════════════════════════════════════════════
        public static void StyleComboBox(ComboBox cmb)
        {
            cmb.FlatStyle = FlatStyle.Flat;
            cmb.Font = AppTheme.Body;
            cmb.ForeColor = AppTheme.TextPrimary;
            cmb.BackColor = AppTheme.Surface;
        }

        // ═══════════════════════════════════════════════════════════
        //  GROUPBOX
        // ═══════════════════════════════════════════════════════════
        /// <summary>
        /// Style GroupBox theo theme: font, forecolor, padding.
        /// </summary>
        public static void StyleGroupBox(GroupBox grp)
        {
            grp.Font = AppTheme.BodyBold;
            grp.ForeColor = AppTheme.TextPrimary;
            grp.BackColor = AppTheme.Surface;
            grp.Padding = new Padding(AppTheme.SpaceMd);
        }

        // ═══════════════════════════════════════════════════════════
        //  SPLITCONTAINER
        // ═══════════════════════════════════════════════════════════
        public static void StyleSplitContainer(SplitContainer split)
        {
            split.BackColor = AppTheme.Border;
            split.Panel1.BackColor = AppTheme.Surface;
            split.Panel2.BackColor = AppTheme.Surface;
        }

        // ═══════════════════════════════════════════════════════════
        //  CARDS / PANELS
        // ═══════════════════════════════════════════════════════════
        /// <summary>
        /// Tạo panel "card" với background trắng, viền nhẹ, bo góc.
        /// </summary>
        public static Panel CreateCard(int padding = -1)
        {
            int p = padding < 0 ? AppTheme.SpaceXl : padding;
            var card = new Panel
            {
                BackColor = AppTheme.Surface,
                Padding = new Padding(p),
                Margin = new Padding(0)
            };
            card.Paint += (s, e) =>
            {
                DrawBorder(e.Graphics, card.ClientRectangle, AppTheme.Border, AppTheme.CornerRadiusLg);
            };
            ApplyRoundedRegion(card, AppTheme.CornerRadiusLg);
            card.Resize += (s, e) => ApplyRoundedRegion(card, AppTheme.CornerRadiusLg);
            return card;
        }

        public static void StyleAsCard(Control ctrl, int radius = -1)
        {
            int r = radius < 0 ? AppTheme.CornerRadiusLg : radius;
            ctrl.BackColor = AppTheme.Surface;
            ctrl.Paint += (s, e) =>
                DrawBorder(e.Graphics, ctrl.ClientRectangle, AppTheme.Border, r);
            ApplyRoundedRegion(ctrl, r);
            ctrl.Resize += (s, e) => ApplyRoundedRegion(ctrl, r);
        }

        /// <summary>
        /// Style một panel làm header bar (có divider line ở dưới).
        /// </summary>
        public static void StyleHeaderPanel(Panel panel)
        {
            panel.BackColor = AppTheme.Surface;
            panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(AppTheme.Border))
                    e.Graphics.DrawLine(pen, 0, panel.Height - 1, panel.Width, panel.Height - 1);
            };
        }

        /// <summary>
        /// Style một panel làm footer/button bar (có divider line ở trên).
        /// </summary>
        public static void StyleFooterPanel(Panel panel)
        {
            panel.BackColor = AppTheme.Surface;
            panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(AppTheme.Border))
                    e.Graphics.DrawLine(pen, 0, 0, panel.Width, 0);
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  LABELS / HEADERS
        // ═══════════════════════════════════════════════════════════
        public static Label CreateHeading(string text, int level = 1)
        {
            var lbl = new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = AppTheme.TextPrimary,
                BackColor = Color.Transparent
            };
            switch (level)
            {
                case 1: lbl.Font = AppTheme.H1; break;
                case 2: lbl.Font = AppTheme.H2; break;
                default: lbl.Font = AppTheme.H3; break;
            }
            return lbl;
        }

        public static Label CreateSubtitle(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = AppTheme.TextSecondary,
                BackColor = Color.Transparent,
                Font = AppTheme.Subtitle
            };
        }

        public static Label CreateFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = AppTheme.TextPrimary,
                BackColor = Color.Transparent,
                Font = AppTheme.BodyBold
            };
        }

        public static Label CreateCaption(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = AppTheme.TextSecondary,
                BackColor = Color.Transparent,
                Font = AppTheme.Caption
            };
        }

        public static Label CreateBadge(string text, Color background, Color foreground)
        {
            var lbl = new Label
            {
                Text = "  " + text + "  ",
                AutoSize = true,
                BackColor = background,
                ForeColor = foreground,
                Font = AppTheme.Caption,
                Padding = new Padding(AppTheme.SpaceSm, 4, AppTheme.SpaceSm, 4),
                TextAlign = ContentAlignment.MiddleCenter
            };
            ApplyRoundedRegion(lbl, AppTheme.CornerRadius);
            lbl.Resize += (s, e) => ApplyRoundedRegion(lbl, AppTheme.CornerRadius);
            return lbl;
        }

        public static Label CreateErrorBanner()
        {
            return new Label
            {
                Visible = false,
                BackColor = AppTheme.ErrorLight,
                ForeColor = AppTheme.Error,
                Font = AppTheme.Caption,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(AppTheme.SpaceMd, AppTheme.SpaceSm, AppTheme.SpaceMd, AppTheme.SpaceSm)
            };
        }

        /// <summary>
        /// Style một Label hiện có thành field label.
        /// </summary>
        public static void StyleFieldLabel(Label lbl)
        {
            lbl.Font = AppTheme.BodyBold;
            lbl.ForeColor = AppTheme.TextPrimary;
            lbl.BackColor = Color.Transparent;
        }

        /// <summary>
        /// Style một Label hiện có thành heading.
        /// </summary>
        public static void StyleHeading(Label lbl, int level = 2)
        {
            lbl.ForeColor = AppTheme.TextPrimary;
            lbl.BackColor = Color.Transparent;
            switch (level)
            {
                case 1: lbl.Font = AppTheme.H1; break;
                case 2: lbl.Font = AppTheme.H2; break;
                default: lbl.Font = AppTheme.H3; break;
            }
        }

        /// <summary>
        /// Style một Label hiện có thành subtitle/description.
        /// </summary>
        public static void StyleSubtitle(Label lbl)
        {
            lbl.Font = AppTheme.Subtitle;
            lbl.ForeColor = AppTheme.TextSecondary;
            lbl.BackColor = Color.Transparent;
        }

        // ═══════════════════════════════════════════════════════════
        //  BORDER DRAWING
        // ═══════════════════════════════════════════════════════════
        public static void DrawBorder(Graphics g, Rectangle bounds, Color color, int radius)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            using (var path = CreateRoundedRectanglePath(rect, radius))
            using (var pen = new Pen(color, 1f))
            {
                g.DrawPath(pen, path);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  LISTVIEW STYLED HEADER
        // ═══════════════════════════════════════════════════════════
        public static void StyleListView(ListView lv)
        {
            lv.BorderStyle = BorderStyle.None;
            lv.BackColor = AppTheme.Surface;
            lv.Font = AppTheme.Body;
            lv.ForeColor = AppTheme.TextPrimary;
            lv.FullRowSelect = true;
            lv.HideSelection = false;
            lv.GridLines = false;
            lv.OwnerDraw = true;

            lv.DrawColumnHeader -= ListViewHeaderPaint;
            lv.DrawColumnHeader += ListViewHeaderPaint;
            lv.DrawItem -= ListViewItemPaint;
            lv.DrawItem += ListViewItemPaint;
            lv.DrawSubItem -= ListViewSubItemPaint;
            lv.DrawSubItem += ListViewSubItemPaint;
        }

        private static void ListViewHeaderPaint(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (var bg = new SolidBrush(AppTheme.Background))
                e.Graphics.FillRectangle(bg, e.Bounds);
            using (var pen = new Pen(AppTheme.Divider))
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

            TextRenderer.DrawText(
                e.Graphics,
                e.Header.Text,
                AppTheme.BodyBold,
                new Rectangle(e.Bounds.X + AppTheme.SpaceMd, e.Bounds.Y, e.Bounds.Width - AppTheme.SpaceMd, e.Bounds.Height),
                AppTheme.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private static void ListViewItemPaint(object sender, DrawListViewItemEventArgs e)
        {
            Color bg = e.Item.Selected ? AppTheme.SelectedFill : AppTheme.Surface;
            using (var brush = new SolidBrush(bg))
                e.Graphics.FillRectangle(brush, e.Bounds);
            using (var pen = new Pen(AppTheme.Divider))
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        private static void ListViewSubItemPaint(object sender, DrawListViewSubItemEventArgs e)
        {
            string text = e.SubItem?.Text ?? "";
            TextRenderer.DrawText(
                e.Graphics,
                text,
                AppTheme.Body,
                new Rectangle(e.Bounds.X + AppTheme.SpaceMd, e.Bounds.Y, e.Bounds.Width - AppTheme.SpaceMd, e.Bounds.Height),
                AppTheme.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        // ═══════════════════════════════════════════════════════════
        //  FORM-LEVEL STYLING
        // ═══════════════════════════════════════════════════════════
        /// <summary>
        /// Apply theme chung cho một form (background, font, forecolor).
        /// Dùng cho form chính (có background xám nhạt).
        /// </summary>
        public static void ApplyFormStyle(Form form)
        {
            AppTheme.ApplyToForm(form);
        }

        /// <summary>
        /// Apply theme cho dialog form (background trắng, dùng cho popup nhỏ).
        /// </summary>
        public static void ApplyDialogStyle(Form form)
        {
            AppTheme.ApplyToDialog(form);
        }
    }
}
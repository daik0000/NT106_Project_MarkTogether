using MarkTogether.Client.UI;

namespace MarkTogether.Client
{
    partial class AIEditPreviewForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSummary;
        private System.Windows.Forms.Label lblScope;
        private System.Windows.Forms.Label lblNotes;
        private System.Windows.Forms.TableLayoutPanel tableDiff;
        private System.Windows.Forms.Label lblBefore;
        private System.Windows.Forms.Label lblAfter;
        private System.Windows.Forms.RichTextBox txtBefore;
        private System.Windows.Forms.RichTextBox txtAfter;
        private System.Windows.Forms.Panel pnlButtons;
        private System.Windows.Forms.Button btnReject;
        private System.Windows.Forms.Button btnSaveOnly;
        private System.Windows.Forms.Button btnApply;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSummary = new System.Windows.Forms.Label();
            this.lblScope = new System.Windows.Forms.Label();
            this.lblNotes = new System.Windows.Forms.Label();
            this.tableDiff = new System.Windows.Forms.TableLayoutPanel();
            this.lblBefore = new System.Windows.Forms.Label();
            this.lblAfter = new System.Windows.Forms.Label();
            this.txtBefore = new System.Windows.Forms.RichTextBox();
            this.txtAfter = new System.Windows.Forms.RichTextBox();
            this.pnlButtons = new System.Windows.Forms.Panel();
            this.btnReject = new System.Windows.Forms.Button();
            this.btnSaveOnly = new System.Windows.Forms.Button();
            this.btnApply = new System.Windows.Forms.Button();
            this.tableDiff.SuspendLayout();
            this.pnlButtons.SuspendLayout();
            this.SuspendLayout();

            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitle.Height = 44;
            this.lblTitle.Text = "AI muốn chỉnh sửa văn bản";
            this.lblTitle.Font = AppTheme.H3;
            this.lblTitle.ForeColor = AppTheme.TextPrimary;
            this.lblTitle.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceLg, AppTheme.SpaceSm, AppTheme.SpaceLg, 0);

            this.lblSummary.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSummary.Height = 28;
            this.lblSummary.Font = AppTheme.Body;
            this.lblSummary.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceLg, 0, AppTheme.SpaceLg, 0);

            this.lblScope.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblScope.Height = 28;
            this.lblScope.Font = AppTheme.Body;
            this.lblScope.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceLg, 0, AppTheme.SpaceLg, 0);

            this.lblNotes.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblNotes.Height = 44;
            this.lblNotes.Font = AppTheme.Body;
            this.lblNotes.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceLg, 0, AppTheme.SpaceLg, 0);

            this.tableDiff.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableDiff.ColumnCount = 2;
            this.tableDiff.RowCount = 2;
            this.tableDiff.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceLg, AppTheme.SpaceSm, AppTheme.SpaceLg, AppTheme.SpaceSm);
            this.tableDiff.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableDiff.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableDiff.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableDiff.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableDiff.Controls.Add(this.lblBefore, 0, 0);
            this.tableDiff.Controls.Add(this.lblAfter, 1, 0);
            this.tableDiff.Controls.Add(this.txtBefore, 0, 1);
            this.tableDiff.Controls.Add(this.txtAfter, 1, 1);

            this.lblBefore.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBefore.Text = "TRƯỚC";
            this.lblBefore.Font = AppTheme.BodyBold;
            this.lblBefore.ForeColor = AppTheme.TextSecondary;

            this.lblAfter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAfter.Text = "SAU";
            this.lblAfter.Font = AppTheme.BodyBold;
            this.lblAfter.ForeColor = AppTheme.TextSecondary;

            this.txtBefore.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtBefore.Font = AppTheme.Mono;
            this.txtBefore.ReadOnly = true;
            this.txtBefore.WordWrap = false;
            this.txtBefore.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Both;
            this.txtBefore.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            this.txtAfter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtAfter.Font = AppTheme.Mono;
            this.txtAfter.ReadOnly = true;
            this.txtAfter.WordWrap = false;
            this.txtAfter.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Both;
            this.txtAfter.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlButtons.Height = 58;
            this.pnlButtons.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceLg, AppTheme.SpaceSm, AppTheme.SpaceLg, AppTheme.SpaceSm);
            this.pnlButtons.Controls.Add(this.btnApply);
            this.pnlButtons.Controls.Add(this.btnSaveOnly);
            this.pnlButtons.Controls.Add(this.btnReject);

            this.btnApply.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnApply.Width = 120;
            this.btnApply.Text = "Áp dụng";
            this.btnApply.DialogResult = System.Windows.Forms.DialogResult.OK;

            this.btnSaveOnly.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnSaveOnly.Width = 150;
            this.btnSaveOnly.Text = "Lưu vào lịch sử AI";
            this.btnSaveOnly.DialogResult = System.Windows.Forms.DialogResult.Retry;

            this.btnReject.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnReject.Width = 110;
            this.btnReject.Text = "Từ chối";
            this.btnReject.DialogResult = System.Windows.Forms.DialogResult.Cancel;

            this.AcceptButton = this.btnApply;
            this.CancelButton = this.btnReject;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = AppTheme.Surface;
            this.ClientSize = new System.Drawing.Size(1100, 720);
            this.Controls.Add(this.tableDiff);
            this.Controls.Add(this.lblNotes);
            this.Controls.Add(this.lblScope);
            this.Controls.Add(this.lblSummary);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.pnlButtons);
            this.MinimumSize = new System.Drawing.Size(900, 560);
            this.Name = "AIEditPreviewForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "AI Edit Preview";
            this.tableDiff.ResumeLayout(false);
            this.pnlButtons.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
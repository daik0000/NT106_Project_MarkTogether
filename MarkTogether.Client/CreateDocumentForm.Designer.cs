using MarkTogether.Client.UI;

namespace MarkTogether.Client
{
    partial class CreateDocumentForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            this.lblHeading = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.lblFieldTitle = new System.Windows.Forms.Label();
            this.pnlTitle = new System.Windows.Forms.Panel();
            this.txtTitle = new System.Windows.Forms.TextBox();
            this.lblError = new System.Windows.Forms.Label();
            this.btnCreate = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.pnlTitle.SuspendLayout();
            this.SuspendLayout();
            //
            // Heading + subtitle
            //
            this.lblHeading.AutoSize = true;
            this.lblHeading.Font = AppTheme.H2;
            this.lblHeading.ForeColor = AppTheme.TextPrimary;
            this.lblHeading.Location = new System.Drawing.Point(24, 22);
            this.lblHeading.Text = "Tạo tài liệu mới";
            //
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = AppTheme.Subtitle;
            this.lblSubtitle.ForeColor = AppTheme.TextSecondary;
            this.lblSubtitle.Location = new System.Drawing.Point(24, 56);
            this.lblSubtitle.Text = "Đặt tên cho tài liệu để bắt đầu cộng tác.";
            //
            // Field
            //
            this.lblFieldTitle.AutoSize = true;
            this.lblFieldTitle.Font = AppTheme.BodyBold;
            this.lblFieldTitle.ForeColor = AppTheme.TextPrimary;
            this.lblFieldTitle.Location = new System.Drawing.Point(24, 96);
            this.lblFieldTitle.Text = "Tiêu đề";
            //
            this.pnlTitle.BackColor = AppTheme.Surface;
            this.pnlTitle.Location = new System.Drawing.Point(24, 122);
            this.pnlTitle.Size = new System.Drawing.Size(432, AppTheme.InputHeight);
            this.pnlTitle.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceMd, 0, AppTheme.SpaceMd, 0);
            this.pnlTitle.Name = "pnlTitle";
            //
            this.txtTitle.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtTitle.Font = AppTheme.Body;
            this.txtTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtTitle.TextChanged += new System.EventHandler(this.txtTitle_TextChanged);
            this.txtTitle.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtTitle_KeyDown);
            this.txtTitle.Name = "txtTitle";
            this.pnlTitle.Controls.Add(this.txtTitle);
            //
            // Error
            //
            this.lblError.Visible = false;
            this.lblError.AutoSize = false;
            this.lblError.Font = AppTheme.Caption;
            this.lblError.ForeColor = AppTheme.Error;
            this.lblError.Location = new System.Drawing.Point(24, 170);
            this.lblError.Size = new System.Drawing.Size(432, 22);
            this.lblError.Name = "lblError";
            //
            // Buttons
            //
            this.btnCancel.Location = new System.Drawing.Point(254, 208);
            this.btnCancel.Size = new System.Drawing.Size(100, AppTheme.ButtonHeight);
            this.btnCancel.Text = "Huỷ";
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            //
            this.btnCreate.Location = new System.Drawing.Point(360, 208);
            this.btnCreate.Size = new System.Drawing.Size(100, AppTheme.ButtonHeight);
            this.btnCreate.Text = "Tạo";
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Click += new System.EventHandler(this.btnCreate_Click);
            //
            // Form
            //
            this.AcceptButton = this.btnCreate;
            this.CancelButton = this.btnCancel;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = AppTheme.Surface;
            this.ClientSize = new System.Drawing.Size(484, 270);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnCreate);
            this.Controls.Add(this.lblError);
            this.Controls.Add(this.pnlTitle);
            this.Controls.Add(this.lblFieldTitle);
            this.Controls.Add(this.lblSubtitle);
            this.Controls.Add(this.lblHeading);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Name = "CreateDocumentForm";
            this.Text = "Tài liệu mới";
            this.pnlTitle.ResumeLayout(false);
            this.pnlTitle.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion

        private System.Windows.Forms.Label lblHeading;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Label lblFieldTitle;
        private System.Windows.Forms.Panel pnlTitle;
        private System.Windows.Forms.TextBox txtTitle;
        private System.Windows.Forms.Label lblError;
        private System.Windows.Forms.Button btnCreate;
        private System.Windows.Forms.Button btnCancel;
    }
}
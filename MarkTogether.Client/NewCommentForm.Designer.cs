using MarkTogether.Client.UI;

namespace MarkTogether.Client
{
    partial class NewCommentForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblPreview = new System.Windows.Forms.Label();
            this.lblPrompt = new System.Windows.Forms.Label();
            this.txtContent = new System.Windows.Forms.TextBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = AppTheme.H3;
            this.lblTitle.ForeColor = AppTheme.TextPrimary;
            this.lblTitle.Location = new System.Drawing.Point(20, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Text = "Đoạn văn bản:";
            // 
            // lblPreview
            // 
            this.lblPreview.BackColor = AppTheme.Background;
            this.lblPreview.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lblPreview.Font = AppTheme.Body;
            this.lblPreview.ForeColor = AppTheme.TextSecondary;
            this.lblPreview.Location = new System.Drawing.Point(20, 48);
            this.lblPreview.Name = "lblPreview";
            this.lblPreview.Padding = new System.Windows.Forms.Padding(AppTheme.SpaceMd);
            this.lblPreview.Size = new System.Drawing.Size(520, 70);
            // 
            // lblPrompt
            // 
            this.lblPrompt.AutoSize = true;
            this.lblPrompt.Font = AppTheme.H3;
            this.lblPrompt.ForeColor = AppTheme.TextPrimary;
            this.lblPrompt.Location = new System.Drawing.Point(20, 132);
            this.lblPrompt.Name = "lblPrompt";
            this.lblPrompt.Text = "Nội dung comment:";
            // 
            // txtContent
            // 
            this.txtContent.AcceptsReturn = true;
            this.txtContent.Font = AppTheme.BodyLarge;
            this.txtContent.ForeColor = AppTheme.TextPrimary;
            this.txtContent.BackColor = AppTheme.Surface;
            this.txtContent.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtContent.Location = new System.Drawing.Point(20, 160);
            this.txtContent.Multiline = true;
            this.txtContent.Name = "txtContent";
            this.txtContent.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtContent.Size = new System.Drawing.Size(520, 130);
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(340, 305);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(95, AppTheme.ButtonHeightSmall);
            this.btnOk.Text = "Gửi";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(445, 305);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(95, AppTheme.ButtonHeightSmall);
            this.btnCancel.Text = "Hủy";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // NewCommentForm
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = AppTheme.Surface;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(560, 355);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.txtContent);
            this.Controls.Add(this.lblPrompt);
            this.Controls.Add(this.lblPreview);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.Name = "NewCommentForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Thêm comment";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblPreview;
        private System.Windows.Forms.Label lblPrompt;
        private System.Windows.Forms.TextBox txtContent;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}
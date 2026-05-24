namespace MarkTogether.Client
{
    partial class AISettingsForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblProvider;
        private System.Windows.Forms.ComboBox cmbProvider;
        private System.Windows.Forms.Label lblModel;
        private System.Windows.Forms.ComboBox cmbModel;
        private System.Windows.Forms.Label lblApiKey;
        private System.Windows.Forms.TextBox txtApiKey;
        private System.Windows.Forms.Button btnToggleShow;
        private System.Windows.Forms.Label lblHint;
        private System.Windows.Forms.Button btnTest;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnSave;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblProvider = new System.Windows.Forms.Label();
            this.cmbProvider = new System.Windows.Forms.ComboBox();
            this.lblModel = new System.Windows.Forms.Label();
            this.cmbModel = new System.Windows.Forms.ComboBox();
            this.lblApiKey = new System.Windows.Forms.Label();
            this.txtApiKey = new System.Windows.Forms.TextBox();
            this.btnToggleShow = new System.Windows.Forms.Button();
            this.lblHint = new System.Windows.Forms.Label();
            this.btnTest = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblProvider
            // 
            this.lblProvider.AutoSize = true;
            this.lblProvider.Location = new System.Drawing.Point(24, 28);
            this.lblProvider.Name = "lblProvider";
            this.lblProvider.Size = new System.Drawing.Size(52, 17);
            this.lblProvider.Text = "Provider:";
            // 
            // cmbProvider
            // 
            this.cmbProvider.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbProvider.FormattingEnabled = true;
            this.cmbProvider.Items.AddRange(new object[] { "Gemini" });
            this.cmbProvider.Location = new System.Drawing.Point(110, 24);
            this.cmbProvider.Name = "cmbProvider";
            this.cmbProvider.Size = new System.Drawing.Size(220, 24);
            // 
            // lblModel
            // 
            this.lblModel.AutoSize = true;
            this.lblModel.Location = new System.Drawing.Point(24, 72);
            this.lblModel.Name = "lblModel";
            this.lblModel.Size = new System.Drawing.Size(45, 17);
            this.lblModel.Text = "Model:";
            // 
            // cmbModel
            // 
            this.cmbModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbModel.FormattingEnabled = true;
            this.cmbModel.Location = new System.Drawing.Point(110, 68);
            this.cmbModel.Name = "cmbModel";
            this.cmbModel.Size = new System.Drawing.Size(330, 24);
            // 
            // lblApiKey
            // 
            this.lblApiKey.AutoSize = true;
            this.lblApiKey.Location = new System.Drawing.Point(24, 116);
            this.lblApiKey.Name = "lblApiKey";
            this.lblApiKey.Size = new System.Drawing.Size(55, 17);
            this.lblApiKey.Text = "API key:";
            // 
            // txtApiKey
            // 
            this.txtApiKey.Location = new System.Drawing.Point(110, 112);
            this.txtApiKey.Name = "txtApiKey";
            this.txtApiKey.Size = new System.Drawing.Size(330, 23);
            this.txtApiKey.UseSystemPasswordChar = true;
            // 
            // btnToggleShow
            // 
            this.btnToggleShow.Location = new System.Drawing.Point(450, 110);
            this.btnToggleShow.Name = "btnToggleShow";
            this.btnToggleShow.Size = new System.Drawing.Size(70, 28);
            this.btnToggleShow.Text = "Hiện";
            this.btnToggleShow.UseVisualStyleBackColor = true;
            this.btnToggleShow.Click += new System.EventHandler(this.btnToggleShow_Click);
            // 
            // lblHint
            // 
            this.lblHint.AutoSize = true;
            this.lblHint.Location = new System.Drawing.Point(110, 145);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(204, 17);
            this.lblHint.Text = "Lấy key tại: aistudio.google.com";
            // 
            // btnTest
            // 
            this.btnTest.Location = new System.Drawing.Point(110, 180);
            this.btnTest.Name = "btnTest";
            this.btnTest.Size = new System.Drawing.Size(120, 32);
            this.btnTest.Text = "Test kết nối";
            this.btnTest.UseVisualStyleBackColor = true;
            this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(245, 187);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(73, 17);
            this.lblStatus.Text = "Trạng thái:";
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(320, 245);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(90, 32);
            this.btnCancel.Text = "Huỷ";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(430, 245);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(90, 32);
            this.btnSave.Text = "Lưu";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // AISettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(550, 305);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnTest);
            this.Controls.Add(this.lblHint);
            this.Controls.Add(this.btnToggleShow);
            this.Controls.Add(this.txtApiKey);
            this.Controls.Add(this.lblApiKey);
            this.Controls.Add(this.cmbModel);
            this.Controls.Add(this.lblModel);
            this.Controls.Add(this.cmbProvider);
            this.Controls.Add(this.lblProvider);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.Name = "AISettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Cài đặt Chatbot AI";
            this.Load += new System.EventHandler(this.AISettingsForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
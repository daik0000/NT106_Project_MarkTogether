namespace MarkTogether.Client
{
    partial class TypeRenderForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.txtRawMarkdown = new System.Windows.Forms.TextBox();
            this.lblRaw = new System.Windows.Forms.Label();
            this.webPreview = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.lblPreview = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.webPreview)).BeginInit();
            this.SuspendLayout();
            // 
            // splitMain
            // 
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Location = new System.Drawing.Point(0, 0);
            this.splitMain.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.Controls.Add(this.txtRawMarkdown);
            this.splitMain.Panel1.Controls.Add(this.lblRaw);
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.Controls.Add(this.webPreview);
            this.splitMain.Panel2.Controls.Add(this.lblPreview);
            this.splitMain.Size = new System.Drawing.Size(1600, 862);
            this.splitMain.SplitterDistance = 797;
            this.splitMain.SplitterWidth = 5;
            this.splitMain.TabIndex = 0;
            // 
            // txtRawMarkdown
            // 
            this.txtRawMarkdown.AcceptsReturn = true;
            this.txtRawMarkdown.AcceptsTab = true;
            this.txtRawMarkdown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtRawMarkdown.Font = new System.Drawing.Font("Consolas", 11F);
            this.txtRawMarkdown.Location = new System.Drawing.Point(0, 34);
            this.txtRawMarkdown.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtRawMarkdown.Multiline = true;
            this.txtRawMarkdown.Name = "txtRawMarkdown";
            this.txtRawMarkdown.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtRawMarkdown.Size = new System.Drawing.Size(797, 828);
            this.txtRawMarkdown.TabIndex = 1;
            this.txtRawMarkdown.WordWrap = false;
            this.txtRawMarkdown.TextChanged += new System.EventHandler(this.txtRawMarkdown_TextChanged);
            // 
            // lblRaw
            // 
            this.lblRaw.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRaw.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblRaw.Location = new System.Drawing.Point(0, 0);
            this.lblRaw.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblRaw.Name = "lblRaw";
            this.lblRaw.Padding = new System.Windows.Forms.Padding(11, 7, 11, 7);
            this.lblRaw.Size = new System.Drawing.Size(797, 34);
            this.lblRaw.TabIndex = 0;
            this.lblRaw.Text = "Raw Markdown";
            // 
            // webPreview
            // 
            this.webPreview.CreationProperties = null;
            this.webPreview.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webPreview.Location = new System.Drawing.Point(0, 34);
            this.webPreview.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.webPreview.Name = "webPreview";
            this.webPreview.Size = new System.Drawing.Size(798, 828);
            this.webPreview.TabIndex = 1;
            this.webPreview.ZoomFactor = 1D;
            // 
            // lblPreview
            // 
            this.lblPreview.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPreview.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblPreview.Location = new System.Drawing.Point(0, 0);
            this.lblPreview.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblPreview.Name = "lblPreview";
            this.lblPreview.Padding = new System.Windows.Forms.Padding(11, 7, 11, 7);
            this.lblPreview.Size = new System.Drawing.Size(798, 34);
            this.lblPreview.TabIndex = 0;
            this.lblPreview.Text = "Rendered Preview";
            // 
            // TypeRenderForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1600, 862);
            this.Controls.Add(this.splitMain);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "TypeRenderForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MarkTogether - Type Render";
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel1.PerformLayout();
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.webPreview)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.TextBox txtRawMarkdown;
        private System.Windows.Forms.Label lblRaw;
        private Microsoft.Web.WebView2.WinForms.WebView2 webPreview;
        private System.Windows.Forms.Label lblPreview;
    }
}

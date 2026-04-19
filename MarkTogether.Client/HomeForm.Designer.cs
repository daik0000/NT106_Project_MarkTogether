namespace MarkTogether.Client
{
    partial class HomeForm
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
            this.pnlTop = new System.Windows.Forms.Panel();
            this.btnImportMd = new System.Windows.Forms.Button();
            this.cmbSortMode = new System.Windows.Forms.ComboBox();
            this.lblSort = new System.Windows.Forms.Label();
            this.btnCreateDocument = new System.Windows.Forms.Button();
            this.lblWelcome = new System.Windows.Forms.Label();
            this.listDocuments = new System.Windows.Forms.ListView();
            this.colTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colUpdatedAt = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPermission = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lblCount = new System.Windows.Forms.Label();
            this.pnlTop.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlTop
            // 
            this.pnlTop.BackColor = System.Drawing.Color.White;
            this.pnlTop.Controls.Add(this.btnImportMd);
            this.pnlTop.Controls.Add(this.cmbSortMode);
            this.pnlTop.Controls.Add(this.lblSort);
            this.pnlTop.Controls.Add(this.btnCreateDocument);
            this.pnlTop.Controls.Add(this.lblWelcome);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTop.Location = new System.Drawing.Point(0, 0);
            this.pnlTop.Margin = new System.Windows.Forms.Padding(4);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Size = new System.Drawing.Size(1765, 262);
            this.pnlTop.TabIndex = 0;
            // 
            // btnImportMd
            // 
            this.btnImportMd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnImportMd.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(89)))), ((int)(((byte)(182)))));
            this.btnImportMd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnImportMd.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnImportMd.ForeColor = System.Drawing.Color.White;
            this.btnImportMd.Location = new System.Drawing.Point(1076, 67);
            this.btnImportMd.Margin = new System.Windows.Forms.Padding(4);
            this.btnImportMd.Name = "btnImportMd";
            this.btnImportMd.Size = new System.Drawing.Size(230, 55);
            this.btnImportMd.TabIndex = 4;
            this.btnImportMd.Text = "Import MD File";
            this.btnImportMd.UseVisualStyleBackColor = false;
            this.btnImportMd.Click += new System.EventHandler(this.btnImportMd_Click);
            // 
            // cmbSortMode
            // 
            this.cmbSortMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSortMode.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.cmbSortMode.FormattingEnabled = true;
            this.cmbSortMode.Items.AddRange(new object[] {
            "New to Old",
            "Old to New",
            "A to Z",
            "Z to A"});
            this.cmbSortMode.Location = new System.Drawing.Point(572, 81);
            this.cmbSortMode.Margin = new System.Windows.Forms.Padding(4);
            this.cmbSortMode.Name = "cmbSortMode";
            this.cmbSortMode.Size = new System.Drawing.Size(249, 33);
            this.cmbSortMode.TabIndex = 3;
            this.cmbSortMode.SelectedIndexChanged += new System.EventHandler(this.cmbSortMode_SelectedIndexChanged);
            // 
            // lblSort
            // 
            this.lblSort.AutoSize = true;
            this.lblSort.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblSort.Location = new System.Drawing.Point(512, 81);
            this.lblSort.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblSort.Name = "lblSort";
            this.lblSort.Size = new System.Drawing.Size(52, 28);
            this.lblSort.TabIndex = 2;
            this.lblSort.Text = "Sort";
            // 
            // btnCreateDocument
            // 
            this.btnCreateDocument.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCreateDocument.BackColor = System.Drawing.Color.DodgerBlue;
            this.btnCreateDocument.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCreateDocument.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCreateDocument.ForeColor = System.Drawing.Color.White;
            this.btnCreateDocument.Location = new System.Drawing.Point(1350, 67);
            this.btnCreateDocument.Margin = new System.Windows.Forms.Padding(4);
            this.btnCreateDocument.Name = "btnCreateDocument";
            this.btnCreateDocument.Size = new System.Drawing.Size(154, 55);
            this.btnCreateDocument.TabIndex = 1;
            this.btnCreateDocument.Text = "New Note";
            this.btnCreateDocument.UseVisualStyleBackColor = false;
            this.btnCreateDocument.Click += new System.EventHandler(this.btnCreateDocument_Click);
            // 
            // lblWelcome
            // 
            this.lblWelcome.AutoSize = true;
            this.lblWelcome.Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);
            this.lblWelcome.ForeColor = System.Drawing.SystemColors.Highlight;
            this.lblWelcome.Location = new System.Drawing.Point(52, 67);
            this.lblWelcome.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblWelcome.Name = "lblWelcome";
            this.lblWelcome.Size = new System.Drawing.Size(154, 50);
            this.lblWelcome.TabIndex = 0;
            this.lblWelcome.Text = "Hi, user";
            // 
            // listDocuments
            // 
            this.listDocuments.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colTitle,
            this.colUpdatedAt,
            this.colPermission});
            this.listDocuments.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listDocuments.FullRowSelect = true;
            this.listDocuments.GridLines = true;
            this.listDocuments.HideSelection = false;
            this.listDocuments.Location = new System.Drawing.Point(0, 262);
            this.listDocuments.Margin = new System.Windows.Forms.Padding(4);
            this.listDocuments.MultiSelect = false;
            this.listDocuments.Name = "listDocuments";
            this.listDocuments.OwnerDraw = true;
            this.listDocuments.Size = new System.Drawing.Size(1765, 488);
            this.listDocuments.TabIndex = 1;
            this.listDocuments.UseCompatibleStateImageBehavior = false;
            this.listDocuments.View = System.Windows.Forms.View.Details;
            this.listDocuments.DrawColumnHeader += new System.Windows.Forms.DrawListViewColumnHeaderEventHandler(this.listDocuments_DrawColumnHeader);
            this.listDocuments.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.listDocuments_DrawItem);
            this.listDocuments.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.listDocuments_DrawSubItem);
            this.listDocuments.ItemActivate += new System.EventHandler(this.listDocuments_ItemActivate);
            // 
            // colTitle
            // 
            this.colTitle.Text = "Title";
            this.colTitle.Width = 560;
            // 
            // colUpdatedAt
            // 
            this.colUpdatedAt.Text = "Updated At";
            this.colUpdatedAt.Width = 220;
            // 
            // colPermission
            // 
            this.colPermission.Text = "Permission";
            this.colPermission.Width = 140;
            // 
            // lblCount
            // 
            this.lblCount.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lblCount.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblCount.ForeColor = System.Drawing.Color.DimGray;
            this.lblCount.Location = new System.Drawing.Point(0, 750);
            this.lblCount.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCount.Name = "lblCount";
            this.lblCount.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.lblCount.Size = new System.Drawing.Size(1765, 34);
            this.lblCount.TabIndex = 2;
            this.lblCount.Text = "Total documents: 0";
            this.lblCount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // HomeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1765, 784);
            this.Controls.Add(this.listDocuments);
            this.Controls.Add(this.lblCount);
            this.Controls.Add(this.pnlTop);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MinimumSize = new System.Drawing.Size(1087, 629);
            this.Name = "HomeForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MarkTogether - Home";
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.Label lblWelcome;
        private System.Windows.Forms.Button btnCreateDocument;
    private System.Windows.Forms.Button btnImportMd;
    private System.Windows.Forms.ComboBox cmbSortMode;
    private System.Windows.Forms.Label lblSort;
        private System.Windows.Forms.ListView listDocuments;
        private System.Windows.Forms.ColumnHeader colTitle;
        private System.Windows.Forms.ColumnHeader colUpdatedAt;
        private System.Windows.Forms.ColumnHeader colPermission;
        private System.Windows.Forms.Label lblCount;
    }
}

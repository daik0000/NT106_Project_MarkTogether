using System;
using System.Windows.Forms;
using MarkTogether.Client.UI;

namespace MarkTogether.Client
{
    public partial class NewCommentForm : Form
    {
        public string CommentText { get; private set; }

        public NewCommentForm(string anchorPreview)
        {
            InitializeComponent();
            string preview = anchorPreview ?? "";
            if (preview.Length > 200) preview = preview.Substring(0, 200) + "...";
            lblPreview.Text = string.IsNullOrEmpty(preview)
                ? "(không có đoạn được chọn)"
                : preview;

            BackColor = AppTheme.Surface;
            UiFactory.StylePrimaryButton(btnOk);
            UiFactory.StyleSecondaryButton(btnCancel);
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            string text = (txtContent.Text ?? "").Trim();
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("Vui lòng nhập nội dung comment.", "Comment");
                return;
            }
            CommentText = text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
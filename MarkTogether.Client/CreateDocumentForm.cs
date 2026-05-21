using System;
using System.Windows.Forms;
using MarkTogether.Client.UI;

namespace MarkTogether.Client
{
    public partial class CreateDocumentForm : Form
    {
        public string DocumentTitle { get; private set; }

        public CreateDocumentForm()
        {
            InitializeComponent();
            Load += CreateDocumentForm_Load;
        }

        private void CreateDocumentForm_Load(object sender, EventArgs e)
        {
            UiFactory.StyleInputPanel(pnlTitle);
            UiFactory.StylePrimaryButton(btnCreate);
            UiFactory.StyleSecondaryButton(btnCancel);

            txtTitle.GotFocus += (s, ev) => { pnlTitle.Tag = "focus"; pnlTitle.Invalidate(); };
            txtTitle.LostFocus += (s, ev) => { pnlTitle.Tag = null; pnlTitle.Invalidate(); };
            pnlTitle.Paint += (s, ev) =>
            {
                bool focused = pnlTitle.Tag as string == "focus";
                UiFactory.DrawBorder(ev.Graphics, pnlTitle.ClientRectangle,
                    focused ? AppTheme.BorderFocus : AppTheme.Border,
                    AppTheme.CornerRadius);
            };
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            string title = txtTitle.Text?.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                lblError.Text = "Vui lòng nhập tiêu đề tài liệu.";
                lblError.Visible = true;
                txtTitle.Focus();
                return;
            }
            if (title.Length > 500)
            {
                lblError.Text = "Tiêu đề tối đa 500 ký tự.";
                lblError.Visible = true;
                txtTitle.Focus();
                return;
            }

            DocumentTitle = title;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void txtTitle_TextChanged(object sender, EventArgs e)
        {
            lblError.Visible = false;
        }

        private void txtTitle_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnCreate_Click(sender, e);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
    }
}
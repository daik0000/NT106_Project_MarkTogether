using System;
using System.Windows.Forms;

namespace MarkTogether.Client
{
    public partial class CreateDocumentForm : Form
    {
        public string DocumentTitle { get; private set; }

        public CreateDocumentForm()
        {
            InitializeComponent();
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            string title = txtTitle.Text?.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                lblError.Text = "Please enter a document title.";
                lblError.Visible = true;
                txtTitle.Focus();
                return;
            }

            if (title.Length > 500)
            {
                lblError.Text = "Title must be at most 500 characters.";
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

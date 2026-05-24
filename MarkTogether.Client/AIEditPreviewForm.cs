using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MarkTogether.Client.Services;
using MarkTogether.Client.UI;

namespace MarkTogether.Client
{
    public partial class AIEditPreviewForm : Form
    {
        public AIEditPreviewForm(string oldText, AiEditPlan plan, string newText)
        {
            InitializeComponent();

            lblSummary.Text = "Tóm tắt: " + (string.IsNullOrWhiteSpace(plan?.Summary) ? "(không có)" : plan.Summary);
            lblScope.Text = "Phạm vi: " + DescribeScope(plan);
            lblNotes.Text = "Ghi chú: " + (string.IsNullOrWhiteSpace(plan?.Notes) ? "(không có)" : plan.Notes);

            RenderDiff(oldText ?? "", newText ?? "");
        }

        private static string DescribeScope(AiEditPlan plan)
        {
            if (plan == null) return "(không rõ)";
            if (plan.Type == "rewrite_document") return "Toàn bộ tài liệu (rewrite_document)";
            if (plan.Type == "edit_plan")
            {
                int count = plan.Patches?.Count ?? 0;
                return (plan.Target == "selection" ? "Vùng chọn" : "Tài liệu") + $" ({count} patch)";
            }
            return plan.Type ?? "(không rõ)";
        }

        private void RenderDiff(string oldText, string newText)
        {
            txtBefore.Clear();
            txtAfter.Clear();

            var diff = LineDiff(oldText, newText);
            AppendLines(txtBefore, diff.beforeLines, diff.beforeChanged, Color.FromArgb(255, 228, 230));
            AppendLines(txtAfter, diff.afterLines, diff.afterChanged, Color.FromArgb(220, 252, 231));

            txtBefore.SelectionStart = 0;
            txtAfter.SelectionStart = 0;
            txtBefore.ScrollToCaret();
            txtAfter.ScrollToCaret();
        }

        private static void AppendLines(RichTextBox box, List<string> lines, List<bool> changed, Color backColor)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                int start = box.TextLength;
                box.AppendText(lines[i]);
                if (i < lines.Count - 1) box.AppendText("\n");

                bool mark = i < changed.Count && changed[i];
                if (mark)
                {
                    box.Select(start, box.TextLength - start);
                    box.SelectionBackColor = backColor;
                    box.SelectionLength = 0;
                }
            }
        }

        private static LineDiffResult LineDiff(string a, string b)
        {
            var aLines = (a ?? "").Split('\n');
            var bLines = (b ?? "").Split('\n');
            int n = aLines.Length, m = bLines.Length;
            var dp = new int[n + 1, m + 1];

            for (int i = n - 1; i >= 0; i--)
                for (int j = m - 1; j >= 0; j--)
                    dp[i, j] = aLines[i] == bLines[j]
                        ? dp[i + 1, j + 1] + 1
                        : Math.Max(dp[i + 1, j], dp[i, j + 1]);

            var beforeMark = Enumerable.Repeat(false, n).ToList();
            var afterMark = Enumerable.Repeat(false, m).ToList();

            int x = 0, y = 0;
            while (x < n && y < m)
            {
                if (aLines[x] == bLines[y])
                {
                    x++;
                    y++;
                }
                else if (dp[x + 1, y] >= dp[x, y + 1])
                {
                    beforeMark[x] = true;
                    x++;
                }
                else
                {
                    afterMark[y] = true;
                    y++;
                }
            }

            while (x < n) beforeMark[x++] = true;
            while (y < m) afterMark[y++] = true;

            return new LineDiffResult
            {
                beforeLines = aLines.ToList(),
                beforeChanged = beforeMark,
                afterLines = bLines.ToList(),
                afterChanged = afterMark
            };
        }

        private class LineDiffResult
        {
            public List<string> beforeLines { get; set; }
            public List<bool> beforeChanged { get; set; }
            public List<string> afterLines { get; set; }
            public List<bool> afterChanged { get; set; }
        }
    }
}
using System.Linq;

namespace MarkTogether.Client.Services
{
    public static class AIEditPlanApplier
    {
        public class ApplyResult
        {
            public bool Ok { get; set; }
            public string NewText { get; set; }
            public string Error { get; set; }
        }

        public static ApplyResult Apply(string oldText, AiEditPlan plan)
        {
            if (plan == null)
                return new ApplyResult { Ok = false, Error = "Plan rỗng." };

            oldText = oldText ?? "";

            if (plan.Type == "rewrite_document")
                return new ApplyResult { Ok = true, NewText = plan.NewContent ?? "" };

            if (plan.Type != "edit_plan")
                return new ApplyResult { Ok = false, Error = "Plan không có patches để apply (type=" + plan.Type + ")." };

            if (plan.Patches == null || plan.Patches.Count == 0)
                return new ApplyResult { Ok = false, Error = "Không có patches." };

            var sorted = plan.Patches.OrderByDescending(p => p.Start).ToList();
            string text = oldText;

            foreach (var p in sorted)
            {
                if (p.Start < 0 || p.Start > text.Length || p.End < p.Start || p.End > text.Length)
                    return new ApplyResult { Ok = false, Error = $"Patch [{p.Start},{p.End}] vượt biên (text length={text.Length})." };

                switch (p.Op)
                {
                    case "replace":
                        text = text.Substring(0, p.Start) + (p.NewText ?? "") + text.Substring(p.End);
                        break;
                    case "insert":
                        text = text.Substring(0, p.Start) + (p.NewText ?? "") + text.Substring(p.Start);
                        break;
                    case "delete":
                        text = text.Substring(0, p.Start) + text.Substring(p.End);
                        break;
                    default:
                        return new ApplyResult { Ok = false, Error = "Op không hỗ trợ: " + p.Op };
                }
            }

            return new ApplyResult { Ok = true, NewText = text };
        }
    }
}
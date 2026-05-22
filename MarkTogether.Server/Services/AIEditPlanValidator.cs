using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace MarkTogether.Server.Services
{
    public class AIEditPlanValidator
    {
        private const int MaxPatches = 50;
        private const int MaxTotalNewTextBytes = 80 * 1024;
        private const int MaxRewriteBytes = 200 * 1024;
        private const int MaxSummaryLen = 200;
        private const int MaxNotesLen = 500;

        private static readonly Regex ControlCharRegex =
            new Regex("[\\x00-\\x08\\x0B\\x0C\\x0E-\\x1F]", RegexOptions.Compiled);

        public class ValidationResult
        {
            public bool Ok { get; set; }
            public string Reason { get; set; }
            public string NormalizedJson { get; set; }
            public string Kind { get; set; }
            public string TextFallback { get; set; }
        }

        public static ValidationResult Validate(
            string rawJson, int documentLength,
            int selectionStart, int selectionEnd)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
                return Fail("AI trả về rỗng.");

            JObject jo;
            try
            {
                jo = JObject.Parse(rawJson);
            }
            catch (Exception ex)
            {
                return Fail("JSON không hợp lệ: " + ex.Message);
            }

            string type = (jo["type"] ?? "").ToString();
            switch (type)
            {
                case "chat":
                    return ValidateChat(jo);
                case "edit_plan":
                    return ValidateEditPlan(jo, documentLength, selectionStart, selectionEnd);
                case "rewrite_document":
                    return ValidateRewrite(jo);
                default:
                    return Fail("Loại phản hồi không hợp lệ: " + type);
            }
        }

        private static ValidationResult ValidateChat(JObject jo)
        {
            string msg = (jo["message"] ?? "").ToString();
            if (string.IsNullOrEmpty(msg))
                return Fail("type=chat nhưng thiếu message.");

            return new ValidationResult
            {
                Ok = true,
                Kind = "text",
                TextFallback = msg.Length > 4000 ? msg.Substring(0, 4000) : msg
            };
        }

        private static ValidationResult ValidateEditPlan(
            JObject jo, int docLen, int selStart, int selEnd)
        {
            string summary = (jo["summary"] ?? "").ToString();
            if (summary.Length > MaxSummaryLen)
                return Fail("summary quá dài.");

            string target = (jo["target"] ?? "document").ToString();
            if (target != "selection" && target != "document")
                return Fail("target không hợp lệ: " + target);

            var patches = jo["patches"] as JArray;
            if (patches == null || patches.Count == 0)
                return Fail("edit_plan không có patches.");
            if (patches.Count > MaxPatches)
                return Fail("Quá nhiều patches: " + patches.Count);

            var parsed = new List<PatchInfo>();
            int totalNewBytes = 0;

            foreach (var p in patches)
            {
                string op = (p["op"] ?? "").ToString();
                if (op != "replace" && op != "insert" && op != "delete")
                    return Fail("op không hợp lệ: " + op);

                int start = (p["start"] ?? -1).Value<int>();
                int end = (p["end"] ?? start).Value<int>();
                string newText = (p["newText"] ?? "").ToString();

                if (op == "insert")
                    end = start;

                if (start < 0 || start > docLen)
                    return Fail($"start={start} ngoài [0,{docLen}].");
                if (end < start || end > docLen)
                    return Fail($"end={end} không hợp lệ.");

                if (target == "selection" && (start < selStart || end > selEnd))
                    return Fail($"Patch [{start},{end}] vượt selection [{selStart},{selEnd}].");

                if (ControlCharRegex.IsMatch(newText))
                    return Fail("newText chứa ký tự control.");

                totalNewBytes += System.Text.Encoding.UTF8.GetByteCount(newText);
                if (totalNewBytes > MaxTotalNewTextBytes)
                    return Fail("Tổng newText vượt 80 KB.");

                parsed.Add(new PatchInfo { Op = op, Start = start, End = end, NewText = newText });
            }

            var sorted = parsed.OrderBy(x => x.Start).ToList();
            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i - 1].End > sorted[i].Start)
                    return Fail($"Patches chồng lấn tại ~{sorted[i].Start}.");
            }

            var clean = new JObject
            {
                ["type"] = "edit_plan",
                ["summary"] = summary,
                ["target"] = target,
                ["patches"] = new JArray(parsed.Select(p => new JObject
                {
                    ["op"] = p.Op,
                    ["start"] = p.Start,
                    ["end"] = p.End,
                    ["newText"] = p.NewText
                })),
                ["notes"] = TruncateNotes(jo["notes"]?.ToString())
            };

            return new ValidationResult
            {
                Ok = true,
                Kind = "edit_plan",
                NormalizedJson = clean.ToString()
            };
        }

        private static ValidationResult ValidateRewrite(JObject jo)
        {
            string newContent = (jo["newContent"] ?? "").ToString();
            int bytes = System.Text.Encoding.UTF8.GetByteCount(newContent);
            if (bytes > MaxRewriteBytes)
                return Fail("newContent quá lớn (>200KB).");
            if (ControlCharRegex.IsMatch(newContent))
                return Fail("newContent chứa ký tự control.");

            string summary = (jo["summary"] ?? "").ToString();
            if (summary.Length > MaxSummaryLen)
                return Fail("summary quá dài.");

            var clean = new JObject
            {
                ["type"] = "rewrite_document",
                ["summary"] = summary,
                ["newContent"] = newContent,
                ["notes"] = TruncateNotes(jo["notes"]?.ToString())
            };

            return new ValidationResult
            {
                Ok = true,
                Kind = "edit_plan",
                NormalizedJson = clean.ToString()
            };
        }

        private static string TruncateNotes(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return s.Length <= MaxNotesLen ? s : s.Substring(0, MaxNotesLen);
        }

        private static ValidationResult Fail(string reason)
        {
            return new ValidationResult { Ok = false, Reason = reason };
        }

        private class PatchInfo
        {
            public string Op { get; set; }
            public int Start { get; set; }
            public int End { get; set; }
            public string NewText { get; set; }
        }
    }
}
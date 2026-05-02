using System;
using MarkTogether.Shared;

namespace MarkTogether.Server.OT
{
    // [OT] Operational Transformation Engine logic
    public static class OTEngine
    {
        public static EditOpItem TransformInsertAgainstInsert(EditOpItem op1, EditOpItem op2)
        {
            var result = new EditOpItem
            {
                pos = op1.pos,
                text = op1.text,
                timestamp = op1.timestamp
            };

            // Rule: If op2.pos <= op1.pos → op1.pos += op2.text.Length
            if (op2.pos <= op1.pos)
            {
                result.pos += op2.text.Length;
            }

            return result;
        }

        public static EditOpItem TransformInsertAgainstDelete(EditOpItem op1, EditOpItem op2, int deleteLength)
        {
            var result = new EditOpItem
            {
                pos = op1.pos,
                text = op1.text,
                timestamp = op1.timestamp
            };

            // Rule:
            // Nếu delete xảy ra hoàn toàn trước op1.pos (op2.pos + deleteLength <= op1.pos) → op1.pos -= deleteLength
            // Nếu delete overlap với op1.pos (op2.pos < op1.pos) → op1.pos = op2.pos
            if (op2.pos + deleteLength <= op1.pos)
            {
                result.pos -= deleteLength;
            }
            else if (op2.pos < op1.pos)
            {
                result.pos = op2.pos;
            }

            return result;
        }

        public static EditOpItem TransformDeleteAgainstInsert(EditOpItem op1, int deleteLength, EditOpItem op2)
        {
            var result = new EditOpItem
            {
                pos = op1.pos,
                text = op1.text, 
                timestamp = op1.timestamp
            };

            // Rule: Nếu op2.pos <= op1.pos → op1.pos += op2.text.Length
            if (op2.pos <= op1.pos)
            {
                result.pos += op2.text.Length;
            }

            return result;
        }

        public static EditOpItem TransformDeleteAgainstDelete(EditOpItem op1, int deleteLength1, EditOpItem op2, int deleteLength2)
        {
            var result = new EditOpItem
            {
                pos = op1.pos,
                text = op1.text,
                timestamp = op1.timestamp
            };

            // [BUG 1 FIX] Proper overlap calculation for Delete vs Delete
            // 1. Adjust position: subtract characters from op2 that were BEFORE op1
            int charsBefore = Math.Min(deleteLength2, Math.Max(0, op1.pos - op2.pos));
            result.pos = op1.pos - charsBefore;

            // 2. Adjust text: remove characters from op1.text that overlap with op2's range
            if (string.IsNullOrEmpty(op1.text))
            {
                return result;
            }

            string textBefore = op1.text.Substring(0, Math.Max(0, Math.Min(deleteLength1, op2.pos - op1.pos)));
            int overlapEnd = op2.pos + deleteLength2;
            int startAfter = Math.Max(0, overlapEnd - op1.pos);
            string textAfter = startAfter < deleteLength1 ? op1.text.Substring(startAfter) : string.Empty;

            result.text = textBefore + textAfter;

            return result;
        }
    }
}

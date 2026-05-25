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

            int insertLen = op2.text?.Length ?? 0;
            if (insertLen == 0) return result;

            if (op2.pos <= op1.pos)
            {
                result.pos += insertLen;
            }
            else if (op2.pos < op1.pos + deleteLength)
            {
                if (!string.IsNullOrEmpty(result.text))
                {
                    int cutAt = op2.pos - op1.pos;
                    if (cutAt < result.text.Length)
                        result.text = result.text.Substring(0, cutAt);
                }
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

            // [BUG 1 FIX] Proper overlap calculation for Delete vs Delete.
            int charsBefore = Math.Min(deleteLength2, Math.Max(0, op1.pos - op2.pos));
            result.pos = op1.pos - charsBefore;

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

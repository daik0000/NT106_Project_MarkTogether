using System;
using System.Collections.Generic;
using MarkTogether.Server.Database.Models;
using MarkTogether.Server.Database.Repositories;
using MarkTogether.Shared;

namespace MarkTogether.Server.OT
{
    // [OT] State management for a specific document
    public class DocumentState
    {
        public string DocId { get; }
        public int ServerRevision { get; private set; }
        private readonly object _lock = new object();

        public DocumentState(string docId)
        {
            DocId = docId;
            ServerRevision = DocumentRepository.GetCurrentRevision(docId);
        }

        // [OT] Transform and apply a single operation
        public EditOpItem TransformAndApply(EditOpItem op, string opType, int clientRevision, int userId)
        {
            lock (_lock)
            {
                // 1. Get concurrent server operations
                List<DocumentOperation> serverOps = GetOpsSince(clientRevision);

                EditOpItem transformedOp = new EditOpItem
                {
                    pos = op.pos,
                    text = op.text,
                    timestamp = op.timestamp
                };

                // 2. Transform against each concurrent op
                foreach (var sOp in serverOps)
                {
                    transformedOp = Transform(transformedOp, opType, sOp);
                }

                // 3. Increment revision
                ServerRevision++;

                // 4. Save to database
                DocumentOperation newOp = new DocumentOperation
                {
                    DocId = DocId,
                    UserId = userId,
                    OpType = opType,
                    Pos = transformedOp.pos,
                    Text = opType == "insert" ? transformedOp.text : null,
                    Length = opType == "delete" ? (transformedOp.text?.Length ?? 0) : (int?)null,
                    Revision = ServerRevision,
                    AppliedAt = DateTime.UtcNow
                };
                DocumentRepository.SaveOperation(newOp);

                return transformedOp;
            }
        }

        private List<DocumentOperation> GetOpsSince(int revision)
        {
            return DocumentRepository.GetOpsSince(DocId, revision);
        }

        private EditOpItem Transform(EditOpItem op, string opType, DocumentOperation serverOp)
        {
            if (opType == "insert")
            {
                if (serverOp.OpType == "insert")
                {
                    return OTEngine.TransformInsertAgainstInsert(op, new EditOpItem { pos = serverOp.Pos, text = serverOp.Text });
                }
                else
                {
                    return OTEngine.TransformInsertAgainstDelete(op, new EditOpItem { pos = serverOp.Pos }, serverOp.Length ?? 0);
                }
            }
            else // delete
            {
                int deleteLen = op.text?.Length ?? 0;
                if (serverOp.OpType == "insert")
                {
                    return OTEngine.TransformDeleteAgainstInsert(op, deleteLen, new EditOpItem { pos = serverOp.Pos, text = serverOp.Text });
                }
                else
                {
                    return OTEngine.TransformDeleteAgainstDelete(op, deleteLen, new EditOpItem { pos = serverOp.Pos }, serverOp.Length ?? 0);
                }
            }
        }
    }
}

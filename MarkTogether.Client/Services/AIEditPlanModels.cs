using System.Collections.Generic;
using Newtonsoft.Json;

namespace MarkTogether.Client.Services
{
    public class AiEditPlan
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("target")]
        public string Target { get; set; }

        [JsonProperty("patches")]
        public List<AiEditPatch> Patches { get; set; }

        [JsonProperty("newContent")]
        public string NewContent { get; set; }

        [JsonProperty("notes")]
        public string Notes { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class AiEditPatch
    {
        [JsonProperty("op")]
        public string Op { get; set; }

        [JsonProperty("start")]
        public int Start { get; set; }

        [JsonProperty("end")]
        public int End { get; set; }

        [JsonProperty("newText")]
        public string NewText { get; set; }
    }
}
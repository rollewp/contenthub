using System;
using Newtonsoft.Json;

namespace RDA.ContentHub.AzureFunctions.Models
{
    public class CHPostRequest
    {
        [JsonProperty("saveEntityMessage")]
        public SaveEntityMessage SaveEntityMessage { get; set; }

        [JsonProperty("context")]
        public Context Context { get; set; }
    }

    public partial class Context
    {
    }

    public partial class SaveEntityMessage
    {
        [JsonProperty("EventType")]
        public string EventType { get; set; }

        [JsonProperty("TimeStamp")]
        public DateTimeOffset TimeStamp { get; set; }

        [JsonProperty("IsNew")]
        public bool IsNew { get; set; }

        [JsonProperty("TargetDefinition")]
        public string TargetDefinition { get; set; }

        [JsonProperty("TargetId")]
        public long TargetId { get; set; }

        [JsonProperty("TargetIdentifier")]
        public string TargetIdentifier { get; set; }

        [JsonProperty("CreatedOn")]
        public DateTimeOffset CreatedOn { get; set; }

        [JsonProperty("UserId")]
        public long UserId { get; set; }

        [JsonProperty("Version")]
        public long Version { get; set; }

        [JsonProperty("ChangeSet")]
        public ChangeSet ChangeSet { get; set; }
    }

    public partial class ChangeSet
    {
        [JsonProperty("PropertyChanges")]
        public PropertyChange[] PropertyChanges { get; set; }

        [JsonProperty("Cultures")]
        public string[] Cultures { get; set; }

        [JsonProperty("RelationChanges")]
        public RelationChange[] RelationChanges { get; set; }
    }

    public partial class PropertyChange
    {
        [JsonProperty("Culture")]
        public string Culture { get; set; }

        [JsonProperty("Property")]
        public string Property { get; set; }

        [JsonProperty("Type")]
        public string Type { get; set; }

        [JsonProperty("OriginalValue")]
        public string OriginalValue { get; set; }

        [JsonProperty("NewValue")]
        public string NewValue { get; set; }
    }

    public partial class RelationChange
    {
        [JsonProperty("Relation")]
        public string Relation { get; set; }

        [JsonProperty("Role")]
        public long Role { get; set; }

        [JsonProperty("Cardinality")]
        public long Cardinality { get; set; }

        [JsonProperty("NewValues")]
        public long[] NewValues { get; set; }

        [JsonProperty("RemovedValues")]
        public long[] RemovedValues { get; set; }
    }
}

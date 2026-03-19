using Neo4j.Driver;
using System.Text.Json;

namespace CobolUploadApi.Models.Neo4j;

public class CobolNode
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public DateTime? AnalyzedAt { get; set; }
    public long FileSize { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "uploaded";
    public List<string>? Dependencies { get; set; }
    public string? Metadata { get; set; }
    
    public static CobolNode FromRecord(IRecord record)
    {
        var node = record["c"].As<INode>();
        
        // Helper function to convert Neo4j temporal types to DateTime
        DateTime ToDateTime(object value)
        {
            if (value is ZonedDateTime zoned)
            {
                // Convert ZonedDateTime to DateTime
                return new DateTime(zoned.Year, zoned.Month, zoned.Day, 
                                    zoned.Hour, zoned.Minute, zoned.Second, 
                                    zoned.Nanosecond / 1000000, DateTimeKind.Utc);
            }
            if (value is LocalDateTime local)
            {
                return new DateTime(local.Year, local.Month, local.Day, 
                                    local.Hour, local.Minute, local.Second, 
                                    local.Nanosecond / 1000000, DateTimeKind.Local);
            }
            return Convert.ToDateTime(value);
        }
        
        // Helper function to get nullable DateTime
        DateTime? ToNullableDateTime(object? value)
        {
            if (value == null) return null;
            return ToDateTime(value);
        }
        
        // Xử lý metadata - nếu là object thì convert sang JSON string
        string? metadataString = null;
        if (node.Properties.ContainsKey("metadata") && node.Properties["metadata"] != null)
        {
            var metadataObj = node.Properties["metadata"];
            if (metadataObj is IDictionary<string, object> dict)
            {
                metadataString = JsonSerializer.Serialize(dict);
            }
            else if (metadataObj is string str)
            {
                metadataString = str;
            }
            else
            {
                metadataString = metadataObj.ToString();
            }
        }
        
        return new CobolNode
        {
            Id = node.Properties["id"].As<string>(),
            FileName = node.Properties["fileName"].As<string>(),
            Content = node.Properties["content"].As<string>(),
            UploadedAt = ToDateTime(node.Properties["uploadedAt"]),
            AnalyzedAt = node.Properties.ContainsKey("analyzedAt") ? 
                ToNullableDateTime(node.Properties["analyzedAt"]) : null,
            FileSize = node.Properties["fileSize"].As<long>(),
            Description = node.Properties.ContainsKey("description") ? 
                node.Properties["description"].As<string>() : null,
            Status = node.Properties["status"].As<string>(),
            Metadata = metadataString
        };
    }
}

public class DesignDocumentNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CobolFileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "markdown", "json", "diagram"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? Metadata { get; set; }
    
    public static DesignDocumentNode FromRecord(IRecord record)
    {
        var node = record["d"].As<INode>();
        
        // Helper function to convert Neo4j temporal types to DateTime
        DateTime ToDateTime(object value)
        {
            if (value is ZonedDateTime zoned)
            {
                return new DateTime(zoned.Year, zoned.Month, zoned.Day, 
                                    zoned.Hour, zoned.Minute, zoned.Second, 
                                    zoned.Nanosecond / 1000000, DateTimeKind.Utc);
            }
            if (value is LocalDateTime local)
            {
                return new DateTime(local.Year, local.Month, local.Day, 
                                    local.Hour, local.Minute, local.Second, 
                                    local.Nanosecond / 1000000, DateTimeKind.Local);
            }
            return Convert.ToDateTime(value);
        }
        
        return new DesignDocumentNode
        {
            Id = node.Properties["id"].As<string>(),
            CobolFileId = node.Properties["cobolFileId"].As<string>(),
            FileName = node.Properties["fileName"].As<string>(),
            Content = node.Properties["content"].As<string>(),
            Type = node.Properties["type"].As<string>(),
            CreatedAt = ToDateTime(node.Properties["createdAt"])
        };
    }
}
using Neo4j.Driver;

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
    public string? Metadata { get; set; }  // Lưu dạng JSON string
    
    public static CobolNode FromRecord(IRecord record)
    {
        var node = record["c"].As<INode>();
        return new CobolNode
        {
            Id = node.Properties["id"].As<string>(),
            FileName = node.Properties["fileName"].As<string>(),
            Content = node.Properties["content"].As<string>(),
            UploadedAt = node.Properties["uploadedAt"].As<DateTime>(),
            AnalyzedAt = node.Properties.ContainsKey("analyzedAt") ? node.Properties["analyzedAt"].As<DateTime?>() : null,
            FileSize = node.Properties["fileSize"].As<long>(),
            Description = node.Properties.ContainsKey("description") ? node.Properties["description"].As<string>() : null,
            Status = node.Properties["status"].As<string>(),
            Metadata = node.Properties.ContainsKey("metadata") ? node.Properties["metadata"].As<string>() : null
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
}
using System.ComponentModel.DataAnnotations;

namespace CobolUploadApi.Models;

public class CobolUploadRequest
{
    [Required]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public Dictionary<string, string>? Metadata { get; set; }
}

public class CobolUploadResponse
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Status { get; set; } = "success";
    public string? DesignDocumentPath { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public class CobolFileInfo
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public long FileSize { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AnalyzeRequest
{
    public string FileId { get; set; } = string.Empty;
    public bool GenerateDesign { get; set; } = true;
}
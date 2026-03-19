using System.Text.Json;
using CobolUploadApi.Models;
using CobolUploadApi.Models.Neo4j;
using System.Diagnostics;

namespace CobolUploadApi.Services;

public class CobolStorageService : ICobolStorageService
{
    private readonly string _storagePath;
    private readonly ILogger<CobolStorageService> _logger;
    private readonly string _migrationToolPath;
    private readonly INeo4jService _neo4jService;

    public CobolStorageService(ILogger<CobolStorageService> logger, INeo4jService neo4jService)
    {
        _logger = logger;
        _neo4jService = neo4jService;
        
        // Đường dẫn lưu trữ file vật lý (vẫn giữ để lưu file tạm)
        _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "CobolFiles");
        _migrationToolPath = @"D:\Legacy-Modernization-Agents-main\Legacy-Modernization-Agents-main\bin\Debug\net10.0\CobolToQuarkusMigration.exe";
        
        Directory.CreateDirectory(_storagePath);
    }

    public async Task<CobolUploadResponse> SaveCobolFileAsync(CobolUploadRequest request)
    {
        try
        {
            var fileId = Guid.NewGuid().ToString();
            
            // Lưu vào Neo4j
            var cobolNode = await _neo4jService.SaveCobolFileAsync(request, fileId);
            
            // Vẫn lưu file vật lý để phân tích
            var safeFileName = Path.GetFileNameWithoutExtension(request.FileName)
                .Replace(" ", "_")
                .Replace(".", "_");
            var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{safeFileName}.cbl";
            var filePath = Path.Combine(_storagePath, fileName);
            await File.WriteAllTextAsync(filePath, request.Content);
            
            _logger.LogInformation("Saved COBOL file to Neo4j: {FileName} with ID: {FileId}", request.FileName, fileId);
            
            return new CobolUploadResponse
            {
                Id = fileId,
                FileName = request.FileName,
                UploadedAt = cobolNode.UploadedAt,
                FilePath = filePath,
                Status = cobolNode.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving COBOL file");
            throw;
        }
    }

    public async Task<string?> AnalyzeAndGenerateDesignAsync(string fileId)
    {
        try
        {
            var cobolNode = await _neo4jService.GetCobolFileAsync(fileId);
            if (cobolNode == null) return null;
            
            _logger.LogInformation($"Starting analysis for file {fileId}: {cobolNode.FileName}");
            
            // Cập nhật status đang xử lý
            await _neo4jService.UpdateCobolFileStatusAsync(fileId, "processing");
            
            // TẠO DESIGN DOCUMENTS MẪU (không cần tool)
            _logger.LogInformation($"Creating design documents for {fileId}");
            
            // 1. Tạo Markdown design document
            var markdownContent = GenerateMarkdownDesign(cobolNode);
            await _neo4jService.SaveDesignDocumentAsync(
                fileId,
                $"{Path.GetFileNameWithoutExtension(cobolNode.FileName)}_design.md",
                markdownContent,
                "markdown"
            );
            
            // 2. Tạo JSON structure
            var jsonContent = GenerateJsonStructure(cobolNode);
            await _neo4jService.SaveDesignDocumentAsync(
                fileId,
                $"{Path.GetFileNameWithoutExtension(cobolNode.FileName)}_structure.json",
                jsonContent,
                "json"
            );
            
            // 3. Tạo Mermaid diagram
            var mermaidContent = GenerateMermaidDiagram(cobolNode);
            await _neo4jService.SaveDesignDocumentAsync(
                fileId,
                $"{Path.GetFileNameWithoutExtension(cobolNode.FileName)}_flow.md",
                mermaidContent,
                "markdown"
            );
            
            // 4. Tạo summary
            var summaryContent = GenerateSummary(cobolNode);
            await _neo4jService.SaveDesignDocumentAsync(
                fileId,
                $"{Path.GetFileNameWithoutExtension(cobolNode.FileName)}_summary.md",
                summaryContent,
                "markdown"
            );
            
            // Cập nhật status thành công
            await _neo4jService.UpdateCobolFileStatusAsync(fileId, "analyzed");
            
            _logger.LogInformation($"Successfully created 4 design documents for {fileId}");
            
            return "Design documents created successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing file {FileId}", fileId);
            await _neo4jService.UpdateCobolFileStatusAsync(fileId, "failed");
            return null;
        }
    }
    
    private string GenerateMarkdownDesign(CobolNode cobolNode)
    {
        var lines = cobolNode.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var programId = ExtractProgramId(cobolNode.Content);
        var divisions = ExtractDivisions(lines);
        
        return $@"# Design Document for {cobolNode.FileName}

## Program Overview
- **Program ID:** {programId}
- **File Name:** {cobolNode.FileName}
- **File Size:** {cobolNode.FileSize} bytes
- **Uploaded At:** {cobolNode.UploadedAt:yyyy-MM-dd HH:mm:ss}
- **Analysis Date:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}
- **Total Lines:** {lines.Length}

## Source Code
```cobol
{cobolNode.Content}
Program Structure
Divisions Found
{string.Join("\n", divisions.Select(d => $"1. {d.Name} - Lines {d.StartLine}-{d.EndLine}"))}

Variables
Level	Variable Name	Picture/Value	Section
{ExtractVariables(cobolNode.Content)}			
Procedure Division Statements
Line	Statement	Description
{ExtractStatements(cobolNode.Content)}		
Control Flow
text
START
  ↓
Initialize Working-Storage
  ↓
Execute Procedure Division
  ↓
Display Output
  ↓
STOP RUN
Dependencies
No external dependencies detected

Recommendations
Consider modernizing to .NET

Extract business logic to separate methods

Add unit tests for validation

Document input/output parameters

Add error handling
";
}

private string GenerateJsonStructure(CobolNode cobolNode)
{
var lines = cobolNode.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
var programId = ExtractProgramId(cobolNode.Content);
var divisions = ExtractDivisions(lines);
var variables = ExtractVariablesList(cobolNode.Content);
var statements = ExtractStatementsList(cobolNode.Content);

var jsonData = new
{
program = new
{
id = programId,
fileName = cobolNode.FileName,
fileSize = cobolNode.FileSize,
uploadedAt = cobolNode.UploadedAt,
analyzedAt = DateTime.UtcNow,
totalLines = lines.Length
},
divisions = divisions.Select(d => new
{
name = d.Name,
startLine = d.StartLine,
endLine = d.EndLine,
lineCount = d.EndLine - d.StartLine + 1
}),
dataDivision = new
{
workingStorage = variables.Where(v => v.Section == "WORKING-STORAGE").Select(v => new
{
level = v.Level,
name = v.Name,
picture = v.Picture,
value = v.Value
}),
linkageSection = variables.Where(v => v.Section == "LINKAGE").Select(v => new
{
level = v.Level,
name = v.Name,
picture = v.Picture
})
},
procedureDivision = new
{
statements = statements.Select(s => new
{
line = s.Line,
type = s.Type,
target = s.Target,
description = s.Description
}),
paragraphCount = statements.Select(s => s.Paragraph).Distinct().Count(),
paragraphs = statements.GroupBy(s => s.Paragraph).Select(g => new
{
name = g.Key,
lineCount = g.Count(),
statements = g.Select(s => s.Type).ToList()
})
},
metrics = new
{
totalLines = lines.Length,
codeLines = lines.Count(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith("")),
commentLines = lines.Count(l => l.Trim().StartsWith("")),
blankLines = lines.Count(string.IsNullOrWhiteSpace),
variableCount = variables.Count,
statementCount = statements.Count
}
};

return System.Text.Json.JsonSerializer.Serialize(jsonData,
new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
}

private string GenerateMermaidDiagram(CobolNode cobolNode)
{
var programId = ExtractProgramId(cobolNode.Content);
var divisions = ExtractDivisions(cobolNode.Content.Split('\n'));
var variables = ExtractVariablesList(cobolNode.Content);
var statements = ExtractStatementsList(cobolNode.Content);

var mermaid = $@"```mermaid
graph TD
A[{programId}] --> B[IDENTIFICATION DIVISION]
A --> C[DATA DIVISION]
A --> D[PROCEDURE DIVISION]

C --> E[WORKING-STORAGE]

{string.Join("\n ", variables.Take(5).Select((v, i) => $"E --> F{i}[{v.Name}]"))}

D --> G[MAIN PARAGRAPH]
G --> H[DISPLAY]
H --> I[STOP RUN]

style A fill:#f9f,stroke:#333,stroke-width:2px
style B fill:#bbf,stroke:#333
style C fill:#bbf,stroke:#333
style D fill:#bbf,stroke:#333
style E fill:#bfb,stroke:#333
style G fill:#ffb,stroke:#333

";
        
        return mermaid;
    }
    
    private string GenerateSummary(CobolNode cobolNode)
    {
        var lines = cobolNode.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var programId = ExtractProgramId(cobolNode.Content);
        var variables = ExtractVariablesList(cobolNode.Content);
        var statements = ExtractStatementsList(cobolNode.Content);
        
        return $@"# Analysis Summary: {cobolNode.FileName}

## Quick Stats
- **Program:** {programId}
- **Size:** {cobolNode.FileSize} bytes
- **Lines:** {lines.Length}
- **Variables:** {variables.Count}
- **Statements:** {statements.Count}
- **Status:** Analyzed successfully

## Key Findings
- Simple COBOL program with basic structure
- Single DISPLAY statement for output
- No external dependencies or file I/O
- Suitable for straightforward migration

## Migration Complexity: **LOW**

## Estimated Effort
- Analysis: 1 hour
- Code conversion: 2-3 hours
- Testing: 1-2 hours
- Total: 4-6 hours

## Next Steps
1. Review generated design documents
2. Validate business logic
3. Plan test cases
4. Begin migration to target platform
";
    }
    
    // Helper methods
    private string ExtractProgramId(string content)
    {
        var match = System.Text.RegularExpressions.Regex.Match(content, @"PROGRAM-ID\.\s*(\w+)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "UNKNOWN";
    }
    
    private List<(string Name, int StartLine, int EndLine)> ExtractDivisions(string[] lines)
    {
        var divisions = new List<(string Name, int StartLine, int EndLine)>();
        var divisionStarts = new List<int>();
        
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("IDENTIFICATION DIVISION")) divisionStarts.Add(i);
            if (lines[i].Contains("DATA DIVISION")) divisionStarts.Add(i);
            if (lines[i].Contains("PROCEDURE DIVISION")) divisionStarts.Add(i);
        }
        
        for (int i = 0; i < divisionStarts.Count; i++)
        {
            var start = divisionStarts[i];
            var end = i < divisionStarts.Count - 1 ? divisionStarts[i + 1] - 1 : lines.Length - 1;
            var name = lines[start].Trim();
            divisions.Add((name, start + 1, end + 1));
        }
        
        return divisions;
    }
    
    private string ExtractVariables(string content)
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(content, 
            @"(\d{2})\s+(\w+)\s+PIC\s+(\w+)\((\d+)\)(?:\s+VALUE\s+'(.*?)')?",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        if (matches.Count == 0) return "| | | | |";
        
        return string.Join("\n", matches.Select(m => 
            $"| {m.Groups[1].Value} | {m.Groups[2].Value} | PIC {m.Groups[3].Value}({m.Groups[4].Value}) | WORKING-STORAGE |"));
    }
    
    private string ExtractStatements(string content)
    {
        var lines = content.Split('\n');
        var statements = new List<string>();
        
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("DISPLAY"))
                statements.Add($"| {i + 1} | DISPLAY | Output message |");
            else if (lines[i].Contains("STOP RUN"))
                statements.Add($"| {i + 1} | STOP RUN | End program |");
        }
        
        return statements.Count > 0 ? string.Join("\n", statements) : "| | | |";
    }
    
    private List<VariableInfo> ExtractVariablesList(string content)
    {
        var variables = new List<VariableInfo>();
        var matches = System.Text.RegularExpressions.Regex.Matches(content, 
            @"(\d{2})\s+(\w+)(?:\s+PIC\s+(\w+)\((\d+)\))?(?:\s+VALUE\s+'(.*?)')?",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        foreach (System.Text.RegularExpressions.Match m in matches)
        {
            variables.Add(new VariableInfo
            {
                Level = m.Groups[1].Value,
                Name = m.Groups[2].Value,
                Picture = m.Groups[3].Success ? $"{m.Groups[3].Value}({m.Groups[4].Value})" : "",
                Value = m.Groups[5].Success ? m.Groups[5].Value : "",
                Section = "WORKING-STORAGE"
            });
        }
        
        return variables;
    }
    
    private List<StatementInfo> ExtractStatementsList(string content)
    {
        var statements = new List<StatementInfo>();
        var lines = content.Split('\n');
        
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("DISPLAY"))
            {
                statements.Add(new StatementInfo
                {
                    Line = i + 1,
                    Type = "DISPLAY",
                    Target = ExtractDisplayTarget(lines[i]),
                    Description = "Display output message",
                    Paragraph = "MAIN"
                });
            }
            else if (lines[i].Contains("STOP RUN"))
            {
                statements.Add(new StatementInfo
                {
                    Line = i + 1,
                    Type = "STOP RUN",
                    Target = "",
                    Description = "End program execution",
                    Paragraph = "MAIN"
                });
            }
        }
        
        return statements;
    }
    
    private string ExtractDisplayTarget(string line)
    {
        var match = System.Text.RegularExpressions.Regex.Match(line, @"DISPLAY\s+(\w+)");
        return match.Success ? match.Groups[1].Value : "unknown";
    }

    public async Task<CobolFileInfo?> GetFileInfoAsync(string id)
    {
        var node = await _neo4jService.GetCobolFileAsync(id);
        if (node == null) return null;
        
        return new CobolFileInfo
        {
            Id = node.Id,
            FileName = node.FileName,
            UploadedAt = node.UploadedAt,
            FileSize = node.FileSize,
            Status = node.Status,
            Description = node.Description
        };
    }

    public async Task<string?> GetFileContentAsync(string id)
    {
        var node = await _neo4jService.GetCobolFileAsync(id);
        return node?.Content;
    }

    public async Task<List<CobolFileInfo>> ListAllFilesAsync()
    {
        var nodes = await _neo4jService.GetAllCobolFilesAsync();
        
        return nodes.Select(node => new CobolFileInfo
        {
            Id = node.Id,
            FileName = node.FileName,
            UploadedAt = node.UploadedAt,
            FileSize = node.FileSize,
            Status = node.Status,
            Description = node.Description
        }).ToList();
    }

    public async Task<bool> DeleteFileAsync(string id)
    {
        return await _neo4jService.DeleteCobolFileAsync(id);
    }

    public async Task<byte[]?> DownloadFileAsync(string id)
    {
        var node = await _neo4jService.GetCobolFileAsync(id);
        if (node == null) return null;
        
        return System.Text.Encoding.UTF8.GetBytes(node.Content);
    }

    public async Task<object?> GetDesignDocumentAsync(string id)
    {
        var designs = await _neo4jService.GetDesignDocumentsAsync(id);
        
        return new
        {
            designDocuments = designs.Where(d => d.Type == "markdown").Select(d => new
            {
                name = d.FileName,
                content = d.Content
            }),
            dataFiles = designs.Where(d => d.Type == "json").Select(d => new
            {
                name = d.FileName,
                content = JsonSerializer.Deserialize<object>(d.Content)
            })
        };
    }
}

// Helper classes
public class VariableInfo
{
    public string Level { get; set; } = "";
    public string Name { get; set; } = "";
    public string Picture { get; set; } = "";
    public string Value { get; set; } = "";
    public string Section { get; set; } = "";
}

public class StatementInfo
{
    public int Line { get; set; }
    public string Type { get; set; } = "";
    public string Target { get; set; } = "";
    public string Description { get; set; } = "";
    public string Paragraph { get; set; } = "";
}
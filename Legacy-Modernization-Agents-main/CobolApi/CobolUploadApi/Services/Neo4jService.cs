using Neo4j.Driver;
using CobolUploadApi.Models;
using CobolUploadApi.Models.Neo4j;

namespace CobolUploadApi.Services;

public interface INeo4jService
{
    Task InitializeAsync();
    Task<CobolNode> SaveCobolFileAsync(CobolUploadRequest request, string fileId);
    Task<CobolNode?> GetCobolFileAsync(string id);
    Task<List<CobolNode>> GetAllCobolFilesAsync();
    Task<bool> UpdateCobolFileStatusAsync(string id, string status);
    Task<bool> DeleteCobolFileAsync(string id);
    Task SaveDesignDocumentAsync(string cobolFileId, string fileName, string content, string type);
    Task<List<DesignDocumentNode>> GetDesignDocumentsAsync(string cobolFileId);
}

public class Neo4jService : INeo4jService, IAsyncDisposable
{
    private readonly IDriver _driver;
    private readonly ILogger<Neo4jService> _logger;

    public Neo4jService(IConfiguration configuration, ILogger<Neo4jService> logger)
    {
        _logger = logger;
        
        var uri = configuration.GetValue<string>("Neo4j:Uri") ?? "bolt://localhost:7687";
        var user = configuration.GetValue<string>("Neo4j:User") ?? "neo4j";
        var password = configuration.GetValue<string>("Neo4j:Password") ?? "cobol-migration-2025";
        
        _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
    }

    public async Task InitializeAsync()
    {
        await _driver.VerifyConnectivityAsync();
        _logger.LogInformation("Neo4j connected successfully");
    }

    public async Task<CobolNode> SaveCobolFileAsync(CobolUploadRequest request, string fileId)
    {
        var session = _driver.AsyncSession();
        try
        {
            var result = await session.ExecuteWriteAsync(async tx =>
            {
                var cursor = await tx.RunAsync(
                    @"CREATE (c:CobolFile {
                        id: $id,
                        fileName: $fileName,
                        content: $content,
                        uploadedAt: $uploadedAt,
                        fileSize: $fileSize,
                        description: $description,
                        status: $status
                    }) RETURN c",
                    new
                    {
                        id = fileId,
                        fileName = request.FileName,
                        content = request.Content,
                        uploadedAt = DateTime.UtcNow,
                        fileSize = request.Content.Length,
                        description = request.Description ?? "",
                        status = "uploaded"
                    }
                );
                
                return CobolNode.FromRecord(await cursor.SingleAsync());
            });
            
            return result;
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<CobolNode?> GetCobolFileAsync(string id)
    {
        var session = _driver.AsyncSession();
        try
        {
            return await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync("MATCH (c:CobolFile {id: $id}) RETURN c", new { id });
                if (await cursor.FetchAsync())
                    return CobolNode.FromRecord(cursor.Current);
                return null;
            });
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<List<CobolNode>> GetAllCobolFilesAsync()
    {
        var session = _driver.AsyncSession();
        try
        {
            return await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync("MATCH (c:CobolFile) RETURN c ORDER BY c.uploadedAt DESC");
                var files = new List<CobolNode>();
                await foreach (var record in cursor)
                    files.Add(CobolNode.FromRecord(record));
                return files;
            });
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<bool> UpdateCobolFileStatusAsync(string id, string status)
    {
        var session = _driver.AsyncSession();
        try
        {
            return await session.ExecuteWriteAsync(async tx =>
            {
                var cursor = await tx.RunAsync(
                    "MATCH (c:CobolFile {id: $id}) SET c.status = $status RETURN c",
                    new { id, status });
                return await cursor.FetchAsync();
            });
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<bool> DeleteCobolFileAsync(string id)
    {
        var session = _driver.AsyncSession();
        try
        {
            return await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync("MATCH (c:CobolFile {id: $id}) DETACH DELETE c");
                return true;
            });
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task SaveDesignDocumentAsync(string cobolFileId, string fileName, string content, string type)
    {
        var session = _driver.AsyncSession();
        try
        {
            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(
                    @"MATCH (c:CobolFile {id: $cobolFileId})
                    CREATE (d:DesignDocument {
                        id: $id,
                        cobolFileId: $cobolFileId,
                        fileName: $fileName,
                        content: $content,
                        type: $type,
                        createdAt: $createdAt
                    })
                    CREATE (c)-[:HAS_DESIGN]->(d)",
                    new
                    {
                        id = Guid.NewGuid().ToString(),
                        cobolFileId,
                        fileName,
                        content,
                        type,
                        createdAt = DateTime.UtcNow
                    }
                );
            });
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task<List<DesignDocumentNode>> GetDesignDocumentsAsync(string cobolFileId)
    {
        var session = _driver.AsyncSession();
        try
        {
            return await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(
                    @"MATCH (c:CobolFile {id: $cobolFileId})-[:HAS_DESIGN]->(d:DesignDocument) 
                    RETURN d ORDER BY d.createdAt DESC",
                    new { cobolFileId });
                
                var docs = new List<DesignDocumentNode>();
                await foreach (var record in cursor)
                {
                    var node = record["d"].As<INode>();
                    docs.Add(new DesignDocumentNode
                    {
                        Id = node.Properties["id"].As<string>(),
                        CobolFileId = node.Properties["cobolFileId"].As<string>(),
                        FileName = node.Properties["fileName"].As<string>(),
                        Content = node.Properties["content"].As<string>(),
                        Type = node.Properties["type"].As<string>(),
                        CreatedAt = node.Properties["createdAt"].As<DateTime>()
                    });
                }
                return docs;
            });
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _driver.DisposeAsync();
    }
}
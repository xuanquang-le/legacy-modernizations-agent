using Microsoft.AspNetCore.Mvc;
using CobolUploadApi.Models;
using CobolUploadApi.Services;

namespace CobolUploadApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CobolController : ControllerBase
{
    private readonly ICobolStorageService _storageService;
    private readonly ILogger<CobolController> _logger;

    public CobolController(ICobolStorageService storageService, ILogger<CobolController> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload file COBOL
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(CobolUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CobolUploadResponse>> UploadCobol([FromBody] CobolUploadRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _storageService.SaveCobolFileAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading COBOL file");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Upload file COBOL dạng multipart/form-data
    /// </summary>
    [HttpPost("upload-file")]
    [ProducesResponseType(typeof(CobolUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CobolUploadResponse>> UploadCobolFile(IFormFile file, [FromForm] string? description)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();

            var request = new CobolUploadRequest
            {
                FileName = file.FileName,
                Content = content,
                Description = description
            };

            var result = await _storageService.SaveCobolFileAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading COBOL file");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy danh sách tất cả file COBOL
    /// </summary>
    [HttpGet("files")]
    [ProducesResponseType(typeof(List<CobolFileInfo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CobolFileInfo>>> GetAllFiles()
    {
        var files = await _storageService.ListAllFilesAsync();
        return Ok(files);
    }

    /// <summary>
    /// Lấy thông tin file COBOL theo ID
    /// </summary>
    [HttpGet("files/{id}")]
    [ProducesResponseType(typeof(CobolFileInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CobolFileInfo>> GetFileInfo(string id)
    {
        var fileInfo = await _storageService.GetFileInfoAsync(id);
        if (fileInfo == null)
            return NotFound();

        return Ok(fileInfo);
    }

    /// <summary>
    /// Lấy nội dung file COBOL
    /// </summary>
    [HttpGet("files/{id}/content")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> GetFileContent(string id)
    {
        var content = await _storageService.GetFileContentAsync(id);
        if (content == null)
            return NotFound();

        return Ok(new { content });
    }

    /// <summary>
    /// Download file COBOL
    /// </summary>
    [HttpGet("files/{id}/download")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile(string id)
    {
        var fileBytes = await _storageService.DownloadFileAsync(id);
        if (fileBytes == null)
            return NotFound();

        var fileInfo = await _storageService.GetFileInfoAsync(id);
        return File(fileBytes, "text/plain", fileInfo?.FileName ?? "download.cbl");
    }

    /// <summary>
    /// Phân tích và sinh design document
    /// </summary>
[HttpPost("analyze/{id}")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> AnalyzeCobolFile(string id)
{
    var fileInfo = await _storageService.GetFileInfoAsync(id);
    if (fileInfo == null)
        return NotFound();

    var outputPath = await _storageService.AnalyzeAndGenerateDesignAsync(id);
    
    if (outputPath == null)
        return StatusCode(500, new { error = "Analysis failed" });

    return Ok(new 
    { 
        message = "Analysis completed", 
        outputPath,
        fileId = id 
    });
}

    /// <summary>
    /// Xóa file COBOL
    /// </summary>
    [HttpDelete("files/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFile(string id)
    {
        var deleted = await _storageService.DeleteFileAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Lấy design document (nếu đã phân tích)
    /// </summary>
[HttpGet("files/{id}/design")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetDesignDocument(string id)
{
    var design = await _storageService.GetDesignDocumentAsync(id);
    if (design == null)
        return NotFound(new { error = "Design document not found. Please analyze first." });

    return Ok(design);
}
}
using CobolUploadApi.Models;

namespace CobolUploadApi.Services;

public interface ICobolStorageService
{
    Task<CobolUploadResponse> SaveCobolFileAsync(CobolUploadRequest request);
    Task<CobolFileInfo?> GetFileInfoAsync(string id);
    Task<string?> GetFileContentAsync(string id);
    Task<List<CobolFileInfo>> ListAllFilesAsync();
    Task<bool> DeleteFileAsync(string id);
    Task<string?> AnalyzeAndGenerateDesignAsync(string fileId);
    Task<byte[]?> DownloadFileAsync(string id);
    Task<object?> GetDesignDocumentAsync(string id);  // THÊM DÒNG NÀY
}
using Ardalis.Result;
using ConnectHub.BLL.DTOs.Attachments;

namespace ConnectHub.BLL.Interfaces.Services;

public interface IAttachmentService
{
    Task<Result<AttachmentResponseDto>> UploadAsync(Guid currentUserId, Stream stream, string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid attachmentId, Guid currentUserId, CancellationToken cancellationToken = default);
}
using ConnectHub.BLL.DTOs.Attachments;

namespace ConnectHub.BLL.Interfaces.Services
{

    public interface IAttachmentService
    {
        Task<AttachmentResponseDto> UploadAsync(Guid currentUserId, Stream stream, string fileName, string contentType, long fileSize);
        Task DeleteAsync(Guid attachmentId, Guid currentUserId);
    }
}
using ConnectHub.BLL.DTOs.Attachments;
using ConnectHub.BLL.Interfaces.Services;
using ConnectHub.BLL.Interfaces.Storage;
using ConnectHub.DAL.Interfaces;
using ConnectHub.Models.Entities;
using Microsoft.AspNetCore.Hosting;

namespace ConnectHub.BLL.Services
{

    public class AttachmentService : IAttachmentService
    {
        private readonly IAttachmentRepository _attachmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;

        private const string AttachmentFolder = "uploads/attachments";

        public AttachmentService(
            IAttachmentRepository attachmentRepository,
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService)
        {
            _attachmentRepository = attachmentRepository;
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
        }

        public async Task<AttachmentResponseDto> UploadAsync(
            Guid currentUserId,
            Stream stream,
            string fileName,
            string contentType,
            long fileSize)
        {
            ValidateUpload(stream, fileName, contentType, fileSize);

            var attachmentId = Guid.NewGuid();
            var storedFileName = GenerateStoredFileName(attachmentId, fileName);

            var physicalFilePath = GetPhysicalFilePath(storedFileName);

            await SaveFileAsync(stream, physicalFilePath);

            var attachment = CreateAttachment(
                attachmentId,
                currentUserId,
                fileName,
                contentType,
                fileSize,
                storedFileName);

            await _attachmentRepository.AddAsync(attachment);
            await _unitOfWork.SaveChangesAsync();

            return MapToResponseDto(attachment);
        }

        public async Task DeleteAsync(
            Guid attachmentId,
            Guid currentUserId)
        {
            var attachment = await GetAttachmentAsync(attachmentId);

            EnsureUserCanDelete(attachment, currentUserId);

            DeletePhysicalFile(attachment.FilePath);

            _attachmentRepository.Delete(attachment);

            await _unitOfWork.SaveChangesAsync();
        }

        // -------------------------
        // Validation
        // -------------------------

        private static void ValidateUpload(
            Stream stream,
            string fileName,
            string contentType,
            long fileSize)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            if (stream.Length == 0)
                throw new ArgumentException(
                    "File cannot be empty.",
                    nameof(stream));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException(
                    "File name is required.",
                    nameof(fileName));

            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentException(
                    "Content type is required.",
                    nameof(contentType));

            if (fileSize <= 0)
                throw new ArgumentException(
                    "File size must be greater than zero.",
                    nameof(fileSize));
        }

        // -------------------------
        // File handling
        // -------------------------

        private string GenerateStoredFileName(
            Guid attachmentId,
            string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);

            return $"{attachmentId}{extension}";
        }

        //private string GetPhysicalFilePath(string storedFileName)
        //{
        //    var uploadDirectory = Path.Combine(
        //        _fileStorageService.WebRootPath,
        //        AttachmentFolder.Replace(
        //            '/',
        //            Path.DirectorySeparatorChar));

        //    Directory.CreateDirectory(uploadDirectory);

        //    return Path.Combine(
        //        uploadDirectory,
        //        storedFileName);
        //}

        private async Task SaveFileAsync(
            Stream stream,
            string physicalFilePath)
        {
            await using var fileStream = new FileStream(
                physicalFilePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await stream.CopyToAsync(fileStream);
        }

        //private void DeletePhysicalFile(string relativeFilePath)
        //{
        //    var physicalFilePath = Path.Combine(
        //        _fileStorageService.WebRootPath,
        //        relativeFilePath.Replace(
        //            '/',
        //            Path.DirectorySeparatorChar));

        //    if (File.Exists(physicalFilePath))
        //    {
        //        File.Delete(physicalFilePath);
        //    }
        //}

        // -------------------------
        // Entity creation
        // -------------------------

        private static Attachment CreateAttachment(
            Guid attachmentId,
            Guid currentUserId,
            string fileName,
            string contentType,
            long fileSize,
            string storedFileName)
        {
            var filePath = $"{AttachmentFolder}/{storedFileName}";

            return new Attachment
            {
                Id = attachmentId,
                FilePath = filePath,
                FileName = fileName,
                ContentType = contentType,
                FileSize = fileSize,
                UploadedById = currentUserId,
                UploadedAt = DateTime.UtcNow,
                PostId = null
            };
        }

        // -------------------------
        // Delete / authorization
        // -------------------------

        private async Task<Attachment> GetAttachmentAsync(
            Guid attachmentId)
        {
            var attachment = await _attachmentRepository
                .GetByIdAsync(attachmentId);

            return attachment
                ?? throw new KeyNotFoundException(
                    $"Attachment with ID '{attachmentId}' was not found.");
        }

        private static void EnsureUserCanDelete(
            Attachment attachment,
            Guid currentUserId)
        {
            if (attachment.UploadedById != currentUserId)
            {
                throw new UnauthorizedAccessException(
                    "You are not allowed to delete this attachment.");
            }
        }

        // -------------------------
        // Mapping
        // -------------------------

        private static AttachmentResponseDto MapToResponseDto(
            Attachment attachment)
        {
            return new AttachmentResponseDto
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                FileUrl = $"/{attachment.FilePath}",
                ContentType = attachment.ContentType,
                FileSize = attachment.FileSize,
                UploadedAt = attachment.UploadedAt
            };
        }
    }
}
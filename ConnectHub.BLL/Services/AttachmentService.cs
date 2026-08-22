using Ardalis.Result;
using AutoMapper;
using ConnectHub.BLL.DTOs.Attachments;
using ConnectHub.BLL.Interfaces.Services;
using ConnectHub.BLL.Interfaces.Storage;
using ConnectHub.DAL.Interfaces;
using ConnectHub.Models.Entities;
using Microsoft.Extensions.Logging;

namespace ConnectHub.BLL.Services;

public class AttachmentService : IAttachmentService
{
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly IMapper _mapper;
    private readonly ILogger<AttachmentService> _logger;

    private const string AttachmentFolder = "uploads/attachments";

    public AttachmentService(
        IAttachmentRepository attachmentRepository,
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        IMapper mapper,
        ILogger<AttachmentService> logger)
    {
        _attachmentRepository = attachmentRepository;
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<AttachmentResponseDto>> UploadAsync(
        Guid currentUserId,
        Stream stream,
        string fileName,
        string contentType,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        if (stream is null || stream.Length == 0)
            return Result.Invalid(new ValidationError("File stream cannot be null or empty."));

        if (string.IsNullOrWhiteSpace(fileName))
            return Result.Invalid(new ValidationError("File name is required."));

        if (string.IsNullOrWhiteSpace(contentType))
            return Result.Invalid(new ValidationError("Content type is required."));

        if (fileSize <= 0)
            return Result.Invalid(new ValidationError("File size must be greater than zero."));

        var attachmentId = Guid.NewGuid();
        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{attachmentId}{extension}";

        var relativePath = await _fileStorageService.SaveFileAsync(stream, storedFileName, AttachmentFolder);

        var attachment = new Attachment
        {
            Id = attachmentId,
            FilePath = relativePath,
            FileName = fileName,
            ContentType = contentType,
            FileSize = fileSize,
            UploadedById = currentUserId,
            UploadedAt = DateTime.UtcNow,
            PostId = null
        };

        await _attachmentRepository.AddAsync(attachment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Attachment {AttachmentId} uploaded by user {UserId}.", attachment.Id, currentUserId);

        return Result.Success(_mapper.Map<AttachmentResponseDto>(attachment));
    }

    public async Task<Result> DeleteAsync(
        Guid attachmentId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(attachmentId);
        if (attachment is null)
            return Result.NotFound($"Attachment with ID '{attachmentId}' was not found.");

        if (attachment.UploadedById != currentUserId)
            return Result.Forbidden("You are not allowed to delete this attachment.");

        if (!string.IsNullOrWhiteSpace(attachment.FilePath))
        {
            await _fileStorageService.DeleteFileAsync(attachment.FilePath);
        }

        _attachmentRepository.Delete(attachment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Attachment {AttachmentId} deleted by user {UserId}.", attachmentId, currentUserId);

        return Result.Success();
    }
}
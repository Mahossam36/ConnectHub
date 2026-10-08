using AutoMapper;
using ConnectHub.BLL.DTOs.Attachments;
using ConnectHub.BLL.DTOs.Comments;
using ConnectHub.BLL.DTOs.Groups;
using ConnectHub.BLL.DTOs.Notifications;
using ConnectHub.BLL.DTOs.Posts;
using ConnectHub.BLL.DTOs.Reports;
using ConnectHub.BLL.DTOs.Users;
using ConnectHub.Models.Entities;

namespace ConnectHub.BLL.Mappers;

public class ConnectHubProfile : Profile
{
    public ConnectHubProfile()
    {
        // User mappings
        CreateMap<User, UserSummaryDto>()
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}".Trim()))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => ToAvatarUrl(src.ProfileImage)));

        CreateMap<User, UserProfileResponseDto>()
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => ToAvatarUrl(src.ProfileImage)))
            .ForMember(dest => dest.Email, opt => opt.Ignore()); // set from ApplicationUser if needed

        // Category & Tag
        CreateMap<Category, CategoryDto>();
        CreateMap<Tag, TagDto>();

        // Group mappings
        CreateMap<Group, GroupSummaryResponseDto>()
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => src.CoverImagePath))
            .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.CountMembers))
            .ForMember(dest => dest.CurrentUserRole, opt => opt.Ignore());

        CreateMap<Group, GroupDetailResponseDto>()
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => src.CoverImagePath))
            .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.CountMembers))
            .ForMember(dest => dest.CurrentUserRole, opt => opt.Ignore());

        CreateMap<GroupMember, GroupMemberResponseDto>();

        // Post mappings
        CreateMap<Post, PostResponseDto>()
            .ForMember(dest => dest.LikeCount, opt => opt.MapFrom(src => src.LikesCount))
            .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.CommentsCount))
            .ForMember(dest => dest.IsLikedByCurrentUser, opt => opt.Ignore());

        // Comment mappings
        CreateMap<Comment, CommentResponseDto>()
            .ForMember(dest => dest.LikeCount, opt => opt.MapFrom(src => src.LikesCount))
            .ForMember(dest => dest.IsLikedByCurrentUser, opt => opt.Ignore())
            .ForMember(dest => dest.Replies, opt => opt.MapFrom(src => src.Replies));

        // Attachment mappings
        CreateMap<Attachment, AttachmentResponseDto>()
            .ForMember(dest => dest.FileUrl, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.FilePath) ? string.Empty : $"/{src.FilePath.Replace('\\', '/').TrimStart('/')}"));

        // Notification mappings
        CreateMap<Notification, NotificationResponseDto>();

        // Report mappings
        CreateMap<Report, ReportResponseDto>();
    }

    private static string? ToAvatarUrl(string? profileImage)
    {
        if (string.IsNullOrWhiteSpace(profileImage))
            return null;

        return Uri.TryCreate(profileImage, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? profileImage
            : $"/{profileImage.TrimStart('/')}";
    }
}

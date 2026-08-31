export interface PagedResult<T> {
  items: T[];
  total: number;
  skip: number;
  take: number;
}

export enum GroupRole {
  Member = 1,
  Admin = 2,
  Owner = 3
}

export interface TagItem {
  id: string;
  name: string;
}

export interface CategoryItem {
  id: string;
  name: string;
  description?: string | null;
}

export interface Community {
  id: string;
  name: string;
  description?: string | null;
  coverImageUrl?: string | null;
  memberCount: number;
  currentUserRole?: GroupRole | number | string | null;
  category?: CategoryItem | null;
  tags?: TagItem[];
  createdBy?: PostAuthor | null;
  createdAt?: string;
}

export interface CommunityDetail extends Community {
  category: CategoryItem;
  tags: TagItem[];
  createdBy: PostAuthor;
  createdAt: string;
}

export interface PostAuthor {
  id: string;
  displayName: string;
  userName?: string;
  profileImage?: string | null;
  profileImageUrl?: string | null;
  avatarUrl?: string | null;
}

export interface GroupMember {
  id: string;
  groupId: string;
  user: PostAuthor;
  role: GroupRole | number | string;
  joinedAt: string;
}

export interface FeedPost {
  id: string;
  groupId: string;
  groupName: string;
  content: string;
  isPinned: boolean;
  author: PostAuthor;
  likeCount: number;
  commentCount: number;
  isLikedByCurrentUser: boolean;
  createdAt: string;
  updatedAt?: string | null;
  attachments: Array<{
    id?: string;
    filePath?: string | null;
    fileUrl?: string | null;
    fileName?: string | null;
    contentType?: string | null;
  }>;
}

export interface ApiPost extends Omit<FeedPost, 'groupName'> {}

export interface CreatePostRequest {
  content: string;
  attachmentIds: string[];
}

export interface FeedComment {
  id: string;
  postId?: string;
  parentCommentId?: string | null;
  content: string;
  author: PostAuthor;
  createdAt: string;
  updatedAt?: string | null;
  likeCount: number;
  isLikedByCurrentUser: boolean;
  replies?: FeedComment[];
}

export interface Attachment {
  id: string;
  fileName: string;
  fileUrl: string;
  contentType: string;
  fileSize: number;
  uploadedAt: string;
}

export enum ReportTargetType {
  Post = 1,
  Comment = 2
}

export enum ReportStatus {
  Pending = 1,
  ActionTaken = 2,
  Dismissed = 3
}

export interface Report {
  id: string;
  reportedBy: PostAuthor;
  targetType: ReportTargetType | number | string;
  targetId: string;
  reason: string;
  status: ReportStatus | number | string;
  createdAt: string;
  reviewedAt?: string | null;
  contentSnippet?: string | null;
  contentAuthor?: PostAuthor | null;
  postId?: string | null;
  commentId?: string | null;
  groupId?: string | null;
}

export interface CreateReportRequest {
  targetType: number;
  targetId: string;
  reason: string;
}

export interface ResolveReportRequest {
  status: number;
}

export interface Notification {
  id: string;
  message: string;
  targetUrl?: string | null;
  isRead: boolean;
  createdAt: string;
  type: string;
}

export interface NotificationFeed {
  items: Notification[];
  unreadCount: number;
}

export interface CreateCommunityRequest {
  name: string;
  description: string;
  categoryId: string;
  tagIds: string[];
  coverImage?: File | null;
}

export type JoinState = 'none' | 'joining' | 'joined' | 'leaving';

import { Injectable } from '@angular/core';
import { Observable, catchError, forkJoin, map, of, switchMap } from 'rxjs';

import {
  ApiPost,
  Attachment,
  Community,
  CommunityDetail,
  CategoryItem,
  CreatePostRequest,
  CreateReportRequest,
  FeedComment,
  FeedPost,
  GroupMember,
  NotificationFeed,
  PagedResult,
  Report,
  TagItem,
  CreateCommunityRequest,
} from '../models/feed.models';

import { BffApiService } from './bff-api.service';

@Injectable({ providedIn: 'root' })
export class FeedApiService {
  constructor(private readonly bffApi: BffApiService) {}

  // =====================================================
  // COMMUNITIES
  // =====================================================

  getCommunities(search = '', take = 12): Observable<Community[]> {
    const query = new URLSearchParams({
      skip: '0',
      take: take.toString(),
    });

    if (search.trim()) {
      query.set('search', search.trim());
    }

    return this.bffApi
      .get<PagedResult<Community>>(`/api/Groups?${query}`)
      .pipe(map((result) => result.items ?? []));
  }

  searchCommunities(search: string): Observable<Community[]> {
    return this.getCommunities(search, 6);
  }

  getCommunityDetails(groupId: string): Observable<CommunityDetail> {
    return this.bffApi.get<CommunityDetail>(`/api/Groups/${groupId}`);
  }

  getCommunityMembers(groupId: string, skip = 0, take = 100): Observable<GroupMember[]> {
    return this.bffApi
      .get<PagedResult<GroupMember>>(`/api/Groups/${groupId}/members?skip=${skip}&take=${take}`)
      .pipe(map((result) => result.items ?? []));
  }

  changeMemberRole(groupId: string, targetUserId: string, role: number): Observable<void> {
    return this.bffApi.put<void>(`/api/Groups/${groupId}/members/${targetUserId}/role`, { role });
  }

  removeMember(groupId: string, targetUserId: string): Observable<void> {
    return this.bffApi.delete<void>(`/api/Groups/${groupId}/members/${targetUserId}`);
  }

  joinCommunity(groupId: string): Observable<void> {
    return this.bffApi.post<void>(`/api/Groups/${groupId}/join`);
  }

  leaveCommunity(groupId: string): Observable<void> {
    return this.bffApi.post<void>(`/api/Groups/${groupId}/leave`);
  }

  createCommunity(request: CreateCommunityRequest): Observable<CommunityDetail> {
    const body = new FormData();

    body.append('name', request.name.trim());
    body.append('description', request.description.trim());
    body.append('categoryId', request.categoryId);

    request.tagIds.forEach((tagId) => {
      body.append('tagIds', tagId);
    });

    if (request.coverImage) {
      body.append('coverImage', request.coverImage, request.coverImage.name);
    }

    return this.bffApi.postForm<CommunityDetail>('/api/Groups', body);
  }

  // =====================================================
  // CATEGORIES
  // =====================================================

  getCategories(): Observable<CategoryItem[]> {
    return this.bffApi
      .get<PagedResult<CategoryItem>>('/api/Categories?skip=0&take=100')
      .pipe(map((result) => result.items ?? []));
  }

  createCategory(name: string): Observable<CategoryItem> {
    return this.bffApi.post<CategoryItem>('/api/Categories', {
      name: name.trim(),
    });
  }

  // =====================================================
  // TAGS
  // =====================================================

  getTags(): Observable<TagItem[]> {
    return this.bffApi
      .get<PagedResult<TagItem>>('/api/Tags?skip=0&take=100')
      .pipe(map((result) => result.items ?? []));
  }

  createTag(name: string): Observable<TagItem> {
    return this.bffApi.post<TagItem>('/api/Tags', {
      name: name.trim(),
    });
  }

  // =====================================================
  // GROUP FEED
  // =====================================================

  getGroupFeed(groupId: string, skip = 0, take = 20): Observable<FeedPost[]> {
    const url = `/api/Posts/api/groups/${groupId}/posts` + `?skip=${skip}&take=${take}`;

    console.log('[Feed] Loading group feed:', url);

    return this.bffApi.get<PagedResult<ApiPost>>(url).pipe(
      map((result) => {
        console.log(`[Feed] Group ${groupId} posts response:`, result);

        if (!result || !Array.isArray(result.items)) {
          throw new Error(`Invalid posts response for group ${groupId}.`);
        }

        return result.items.map((post) => ({
          ...post,
          groupName: '',
        }));
      }),
    );
  }

  // =====================================================
  // HOME RECENT FEED
  // =====================================================

  // =====================================================
  // HOME RECENT FEED (simplified — no role filtering, no silent drops)
  // =====================================================

  getRecentFeed(): Observable<{ posts: FeedPost[]; communities: Community[] }> {
    return this.getCommunities('', 100).pipe(
      switchMap((communities) => {
        console.log('[Feed] Communities received:', communities.length);

        if (!communities.length) {
          return of({ posts: [] as FeedPost[], communities });
        }

        const requests = communities.map((community) =>
          this.bffApi
            .get<PagedResult<ApiPost>>(`/api/Posts/api/groups/${community.id}/posts?skip=0&take=20`)
            .pipe(
              map(
                (result) =>
                  (result?.items ?? []).map((post) => ({
                    ...post,
                    groupName: community.name,
                  })) as FeedPost[],
              ),
              catchError((error) => {
                console.error(`[Feed] Failed for "${community.name}"`, error);
                return of([] as FeedPost[]);
              }),
            ),
        );

        return forkJoin(requests).pipe(
          map((feeds) => {
            const allPosts = feeds.flat();
            const sorted = [...allPosts].sort(
              (a, b) => +new Date(b.createdAt) - +new Date(a.createdAt),
            );
            console.log('[Feed] Total posts:', sorted.length);
            return { posts: sorted, communities };
          }),
        );
      }),
    );
  }

  getMoreFeed(communities: Community[], skip: number): Observable<FeedPost[]> {
    if (!communities.length) {
      return of([]);
    }

    const requests = communities.map((community) =>
      this.bffApi
        .get<PagedResult<ApiPost>>(
          `/api/Posts/api/groups/${community.id}/posts?skip=${skip}&take=10`,
        )
        .pipe(
          map(
            (result) =>
              (result?.items ?? []).map((post) => ({
                ...post,
                groupName: community.name,
              })) as FeedPost[],
          ),
          catchError((error) => {
            console.error(`[Feed] Failed to load more for "${community.name}"`, error);
            return of([] as FeedPost[]);
          }),
        ),
    );

    return forkJoin(requests).pipe(
      map((feeds) => feeds.flat().sort((a, b) => +new Date(b.createdAt) - +new Date(a.createdAt))),
    );
  }

  // =====================================================
  // POSTS
  // =====================================================

  createPost(groupId: string, request: CreatePostRequest): Observable<ApiPost> {
    return this.bffApi.post<ApiPost>(`/api/Posts/api/groups/${groupId}/posts`, request);
  }

  pinPost(postId: string): Observable<void> {
    return this.bffApi.post<void>(`/api/Posts/api/posts/${postId}/pin`);
  }

  unpinPost(postId: string): Observable<void> {
    return this.bffApi.delete<void>(`/api/Posts/api/posts/${postId}/pin`);
  }

  deletePost(postId: string): Observable<void> {
    return this.bffApi.delete<void>(`/api/Posts/api/posts/${postId}`);
  }

  like(postId: string): Observable<void> {
    return this.bffApi.post<void>(`/api/Posts/api/posts/${postId}/like`);
  }

  unlike(postId: string): Observable<void> {
    return this.bffApi.delete<void>(`/api/Posts/api/posts/${postId}/like`);
  }

  // =====================================================
  // ATTACHMENTS
  // =====================================================

  uploadAttachment(file: File): Observable<Attachment> {
    const body = new FormData();

    body.append('file', file, file.name);

    return this.bffApi.postForm<Attachment>('/api/Attachments', body);
  }

  getAttachmentBlob(attachmentId: string): Observable<Blob> {
    return this.bffApi.getBlob(`/api/Attachments/${attachmentId}`);
  }

  deleteAttachment(attachmentId: string): Observable<void> {
    return this.bffApi.delete<void>(`/api/Attachments/${attachmentId}`);
  }

  // =====================================================
  // COMMENTS
  // =====================================================

  getComments(postId: string, skip = 0, take = 20): Observable<FeedComment[]> {
    return this.bffApi
      .get<PagedResult<FeedComment>>(
        `/api/Comments/api/posts/${postId}/comments` + `?skip=${skip}&take=${take}`,
      )
      .pipe(map((result) => result.items ?? []));
  }

  createComment(
    postId: string,
    content: string,
    parentCommentId?: string | null,
  ): Observable<FeedComment> {
    return this.bffApi.post<FeedComment>(`/api/Comments/api/posts/${postId}/comments`, {
      content,
      parentCommentId: parentCommentId || null,
    });
  }

  updateComment(commentId: string, content: string): Observable<FeedComment> {
    return this.bffApi.put<FeedComment>(`/api/Comments/api/comments/${commentId}`, {
      content,
    });
  }

  deleteComment(commentId: string): Observable<void> {
    return this.bffApi.delete<void>(`/api/Comments/api/comments/${commentId}`);
  }

  likeComment(commentId: string): Observable<void> {
    return this.bffApi.post<void>(`/api/Comments/api/comments/${commentId}/like`);
  }

  unlikeComment(commentId: string): Observable<void> {
    return this.bffApi.delete<void>(`/api/Comments/api/comments/${commentId}/like`);
  }

  // =====================================================
  // NOTIFICATIONS
  // =====================================================

  getNotifications(): Observable<NotificationFeed> {
    return this.bffApi.get<NotificationFeed>('/api/Notifications?skip=0&take=20');
  }

  markAllNotificationsAsRead(): Observable<void> {
    return this.bffApi.put<void>('/api/Notifications/read-all', {});
  }

  markNotificationAsRead(id: string): Observable<void> {
    return this.bffApi.put<void>(`/api/Notifications/${id}/read`, {});
  }

  // =====================================================
  // REPORTS / MODERATION
  // =====================================================

  getReports(skip = 0, take = 50): Observable<Report[]> {
    return this.bffApi
      .get<PagedResult<Report>>(`/api/Reports?skip=${skip}&take=${take}`)
      .pipe(map((result) => result.items ?? []));
  }

  submitReport(request: CreateReportRequest): Observable<Report> {
    return this.bffApi.post<Report>('/api/Reports', request);
  }

  resolveReport(reportId: string, status: number): Observable<Report> {
    return this.bffApi.put<Report>(`/api/Reports/${reportId}/resolve`, {
      status,
    });
  }

  // =====================================================
  // FEED MIXING
  // =====================================================

  private mixRecentPosts(posts: FeedPost[]): FeedPost[] {
    const newest = [...posts].sort((a, b) => +new Date(b.createdAt) - +new Date(a.createdAt));

    return Array.from(
      {
        length: Math.ceil(newest.length / 4),
      },
      (_, index) => this.shuffle(newest.slice(index * 4, index * 4 + 4)),
    ).flat();
  }

  private shuffle<T>(items: T[]): T[] {
    const result = [...items];

    for (let i = result.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));

      [result[i], result[j]] = [result[j], result[i]];
    }

    return result;
  }
}

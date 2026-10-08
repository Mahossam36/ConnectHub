import { Component, OnDestroy, OnInit } from '@angular/core';

import { CommonModule } from '@angular/common';

import { Subject, takeUntil } from 'rxjs';

import { PostComponent, PostResponse } from '../post/post';

import { CommentsPanelComponent } from '../comments-panel/comments-panel';

import { FeedApiService } from '../../../core/services/feed-api.service';
import { Community, FeedPost } from '../../../core/models/feed.models';

@Component({
  selector: 'app-home',

  standalone: true,

  imports: [CommonModule, PostComponent, CommentsPanelComponent],

  templateUrl: './feed.component.html',

  styleUrls: ['./feed.component.scss'],
})
export class HomeComponent implements OnInit, OnDestroy {
  posts: PostResponse[] = [];

  isLoading = false;

  isLoadingMore = false;

  errorMessage = '';

  /*
   * Current authenticated user.
   *
   * This should eventually come from the
   * authenticated-user state/service.
   */
  currentUserId: string | null = null;

  /*
   * Comments drawer.
   */
  commentsOpen = false;

  selectedPost: PostResponse | null = null;

  /*
   * Report notification.
   */
  reportMessage = '';

  // Communities the current user belongs to — used to page in more
  // posts once the initial recent feed has been shown.
  private joinedCommunities: Community[] = [];
  private feedSkip = 0;
  private readonly feedPageSize = 20;

  private readonly destroy$ = new Subject<void>();

  constructor(private readonly feedApi: FeedApiService) {}

  ngOnInit(): void {
    this.loadPosts();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // =====================================================
  // LOAD POSTS
  // =====================================================

  loadPosts(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.feedApi
      .getRecentFeed()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ posts, communities }) => {
          console.log('[Home] posts:', posts.length, 'communities:', communities.length);
          this.joinedCommunities = communities;
          this.posts = posts.map((post) => this.toPostResponse(post));
          this.feedSkip = this.feedPageSize;
          this.isLoading = false;
        },
        error: (error) => {
          console.error('[Home] getRecentFeed failed:', error);
          this.errorMessage = error?.message ?? 'Unable to load posts.';
          this.isLoading = false;
        },
      });
  }

  // =====================================================
  // LOAD MORE
  // =====================================================

  loadMorePosts(): void {
    if (this.isLoading || this.isLoadingMore || !this.joinedCommunities.length) {
      return;
    }

    this.isLoadingMore = true;

    this.errorMessage = '';

    this.feedApi
      .getMoreFeed(this.joinedCommunities, this.feedSkip)

      .pipe(takeUntil(this.destroy$))

      .subscribe({
        next: (newPosts) => {
          this.posts = [...this.posts, ...newPosts.map((post) => this.toPostResponse(post))];

          this.feedSkip += this.feedPageSize;

          this.isLoadingMore = false;
        },

        error: (error) => {
          this.errorMessage = error?.message ?? 'Unable to load more posts.';

          this.isLoadingMore = false;
        },
      });
  }

  // =====================================================
  // HAS MORE POSTS
  // =====================================================

  hasMorePosts(): boolean {
    return this.joinedCommunities.length > 0;
  }

  // =====================================================
  // COMMENTS
  // =====================================================

  openComments(post: PostResponse): void {
    this.selectedPost = post;

    this.commentsOpen = true;
  }

  closeComments(): void {
    this.commentsOpen = false;

    this.selectedPost = null;
  }

  updateCommentCount(count: number): void {
    if (!this.selectedPost) {
      return;
    }

    this.selectedPost.commentCount = count;

    const index = this.posts.findIndex((post) => post.id === this.selectedPost!.id);

    if (index !== -1) {
      this.posts[index].commentCount = count;
    }
  }

  // =====================================================
  // DELETE POST
  // =====================================================

  removePost(postId: string): void {
    this.posts = this.posts.filter((post) => post.id !== postId);

    if (this.selectedPost?.id === postId) {
      this.closeComments();
    }
  }

  // =====================================================
  // LIKE
  // =====================================================

  onPostLikeChanged(updatedPost: PostResponse): void {
    const index = this.posts.findIndex((post) => post.id === updatedPost.id);

    if (index === -1) {
      return;
    }

    this.posts[index] = updatedPost;

    if (this.selectedPost?.id === updatedPost.id) {
      this.selectedPost = updatedPost;
    }
  }

  // =====================================================
  // REPORT
  // =====================================================

  reportPost(post: PostResponse): void {
    this.reportMessage = `Post by ${post.author?.displayName || 'this user'} has been reported.`;

    setTimeout(() => {
      this.reportMessage = '';
    }, 3500);
  }

  // =====================================================
  // RETRY
  // =====================================================

  retry(): void {
    this.posts = [];

    this.feedSkip = 0;

    this.loadPosts();
  }

  // =====================================================
  // TRACK BY
  // =====================================================

  trackByPostId(_: number, post: PostResponse): string {
    return post.id;
  }

  // =====================================================
  // MAPPING — FeedPost (feed.models) -> PostResponse (post.ts)
  // =====================================================

  private toPostResponse(post: FeedPost): PostResponse {
    return {
      id: post.id,
      groupId: post.groupId,
      content: post.content,
      isPinned: post.isPinned,
      author: {
        id: post.author.id,
        displayName: post.author.displayName,
        avatarUrl:
          post.author.avatarUrl ?? post.author.profileImageUrl ?? post.author.profileImage ?? null,
      },
      likeCount: post.likeCount,
      commentCount: post.commentCount,
      isLikedByCurrentUser: post.isLikedByCurrentUser,
      attachments: (post.attachments ?? []).map((attachment) => ({
        id: attachment.id ?? '',
        fileName: attachment.fileName ?? null,
        fileUrl: attachment.fileUrl ?? attachment.filePath ?? null,
        contentType: attachment.contentType ?? null,
        fileSize: 0,
        uploadedAt: post.createdAt,
      })),
      createdAt: post.createdAt,
      updatedAt: post.updatedAt ?? null,
    };
  }
}

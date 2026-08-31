import { Component, OnDestroy, OnInit } from '@angular/core';

import { CommonModule } from '@angular/common';

import { Subject, takeUntil } from 'rxjs';

import { PostComponent, PostResponse } from '../post/post';

import { CommentsPanelComponent } from '../comments-panel/comments-panel';

import { BffApiService } from '../../../core/services/bff-api.service';

interface PostPagedResult {
  items: PostResponse[] | null;
  total: number;
  skip: number;
  take: number;
}

@Component({
  selector: 'app-home',

  standalone: true,

  imports: [CommonModule, PostComponent, CommentsPanelComponent],

  templateUrl: './home.component.html',

  styleUrls: ['./home.component.scss'],
})
export class HomeComponent implements OnInit, OnDestroy {
  posts: PostResponse[] = [];

  totalPosts = 0;

  skip = 0;

  readonly take = 20;

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

  private readonly destroy$ = new Subject<void>();

  constructor(private readonly bffApi: BffApiService) {}

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

    this.bffApi
      .get<PostPagedResult>(`/api/Posts?skip=0&take=${this.take}`)

      .pipe(takeUntil(this.destroy$))

      .subscribe({
        next: (result) => {
          this.posts = result.items ?? [];

          this.totalPosts = result.total ?? 0;

          this.skip = result.skip + this.posts.length;

          this.isLoading = false;
        },

        error: (error) => {
          this.errorMessage = error?.message ?? 'Unable to load posts.';

          this.isLoading = false;
        },
      });
  }

  // =====================================================
  // LOAD MORE
  // =====================================================

  loadMorePosts(): void {
    if (this.isLoading || this.isLoadingMore || !this.hasMorePosts()) {
      return;
    }

    this.isLoadingMore = true;

    this.errorMessage = '';

    this.bffApi
      .get<PostPagedResult>(`/api/Posts?skip=${this.skip}&take=${this.take}`)

      .pipe(takeUntil(this.destroy$))

      .subscribe({
        next: (result) => {
          const newPosts = result.items ?? [];

          this.posts = [...this.posts, ...newPosts];

          this.totalPosts = result.total ?? 0;

          this.skip += newPosts.length;

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
    return this.posts.length < this.totalPosts;
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

    this.totalPosts = Math.max(0, this.totalPosts - 1);

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
    this.skip = 0;

    this.posts = [];

    this.totalPosts = 0;

    this.loadPosts();
  }

  // =====================================================
  // TRACK BY
  // =====================================================

  trackByPostId(_: number, post: PostResponse): string {
    return post.id;
  }
}

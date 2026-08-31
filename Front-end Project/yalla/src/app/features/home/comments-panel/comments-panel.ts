import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  OnDestroy,
  Output,
  SimpleChanges,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { FeedApiService } from '../../../core/services/feed-api.service';
import { FeedComment } from '../../../core/models/feed.models';

export interface CommentResponse {
  id: string;
  postId: string;
  parentCommentId: string | null;
  content: string | null;
  author: {
    id: string;
    displayName: string | null;
    avatarUrl: string | null;
  };
  likeCount: number;
  isLikedByCurrentUser: boolean;
  replies: CommentResponse[] | null;
  createdAt: string;
  updatedAt: string | null;
}

interface CommentPagedResult {
  items: CommentResponse[] | null;
  total: number;
  skip: number;
  take: number;
}

interface CreateCommentRequest {
  content: string | null;
  parentCommentId: string | null;
}

interface UpdateCommentRequest {
  content: string | null;
}

@Component({
  selector: 'app-comments-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './comments-panel.html',
  styleUrls: ['./comments-panel.scss'],
})
export class CommentsPanelComponent
  implements OnChanges, OnDestroy
{
  @Input()
  postId: string | null = null;

  @Input()
  postAuthorId: string | null = null;

  @Input()
  isOpen = false;

  @Output()
  readonly closed = new EventEmitter<void>();

  @Output()
  readonly commentCountChanged = new EventEmitter<number>();

  comments: CommentResponse[] = [];

  totalComments = 0;

  commentText = '';

  replyingTo: CommentResponse | null = null;

  editingCommentId: string | null = null;

  editingText = '';

  isLoading = false;
  isLoadingMore = false;
  isSubmitting = false;

  errorMessage = '';

  skip = 0;
  readonly take = 20;

  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly feedApi: FeedApiService
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    const postChanged =
      changes['postId'] &&
      changes['postId'].currentValue !==
        changes['postId'].previousValue;

    const opened =
      changes['isOpen'] &&
      changes['isOpen'].currentValue === true;

    if (
      this.isOpen &&
      this.postId &&
      (postChanged || opened)
    ) {
      this.resetAndLoadComments();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  close(): void {
    if (this.isSubmitting) {
      return;
    }

    this.closed.emit();
  }

  private resetAndLoadComments(): void {
    this.comments = [];
    this.skip = 0;
    this.totalComments = 0;
    this.errorMessage = '';
    this.replyingTo = null;
    this.editingCommentId = null;

    this.loadComments();
  }

  loadComments(): void {
    if (!this.postId) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.feedApi
      .getComments(this.postId, this.skip, this.take)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.comments = (result ?? []).map(this.mapFeedComment);
          this.totalComments = this.comments.length;
          this.skip = this.skip + this.comments.length;

          this.commentCountChanged.emit(
            this.totalComments
          );

          this.isLoading = false;
        },

        error: (error) => {
          this.errorMessage = error?.message ?? 'Unable to load comments.';

          this.isLoading = false;
        },
      });
  }

  loadMore(): void {
    if (
      !this.postId ||
      this.isLoadingMore ||
      this.isLoading ||
      !this.hasMoreComments()
    ) {
      return;
    }

    this.isLoadingMore = true;
    this.errorMessage = '';

    this.feedApi
      .getComments(this.postId, this.skip, this.take)
      .pipe(
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (result) => {
          const newComments = (result ?? []).map(this.mapFeedComment);

          this.comments = [
            ...this.comments,
            ...newComments,
          ];

          this.totalComments += newComments.length;
          this.skip += newComments.length;

          this.commentCountChanged.emit(
            this.totalComments
          );

          this.isLoadingMore = false;
        },

        error: (error) => {
          this.errorMessage = error?.message ?? 'Unable to load more comments.';

          this.isLoadingMore = false;
        },
      });
  }

  private mapFeedComment = (fc: FeedComment): CommentResponse => ({
    id: fc.id,
    postId: fc.postId ?? '',
    parentCommentId: fc.parentCommentId ?? null,
    content: fc.content ?? null,
    author: {
      id: fc.author.id,
      displayName: fc.author.displayName ?? null,
      avatarUrl: fc.author.avatarUrl ?? fc.author.profileImageUrl ?? fc.author.profileImage ?? null,
    },
    likeCount: fc.likeCount,
    isLikedByCurrentUser: fc.isLikedByCurrentUser,
    replies: fc.replies?.map(this.mapFeedComment) ?? null,
    createdAt: fc.createdAt,
    updatedAt: fc.updatedAt ?? null,
  });

  hasMoreComments(): boolean {
    return this.comments.length < this.totalComments;
  }

  submitComment(): void {
    const content = this.commentText.trim();

    if (!content || !this.postId || this.isSubmitting) {
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    this.feedApi
      .createComment(this.postId, content, this.replyingTo?.id ?? null)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (comment) => {
          const mappedComment = this.mapFeedComment(comment);
          if (comment.parentCommentId) {
            this.addReplyToComment(
              this.comments,
              mappedComment
            );
          } else {
            this.comments = [
              mappedComment,
              ...this.comments,
            ];
          }

          this.totalComments++;

          this.commentCountChanged.emit(
            this.totalComments
          );

          this.commentText = '';
          this.replyingTo = null;
          this.isSubmitting = false;
        },

        error: (error) => {
          this.errorMessage = error?.message ?? 'Unable to post your comment.';

          this.isSubmitting = false;
        },
      });
  }

  startReply(comment: CommentResponse): void {
    this.replyingTo = comment;
    this.editingCommentId = null;

    setTimeout(() => {
      document
        .getElementById('comment-input')
        ?.focus();
    });
  }

  cancelReply(): void {
    this.replyingTo = null;
  }

  startEditing(comment: CommentResponse): void {
    this.editingCommentId = comment.id;
    this.editingText = comment.content ?? '';
    this.replyingTo = null;
  }

  cancelEditing(): void {
    this.editingCommentId = null;
    this.editingText = '';
  }

  saveEdit(comment: CommentResponse): void {
    const content = this.editingText.trim();

    if (!content || !comment.id) {
      return;
    }

    this.feedApi
      .updateComment(comment.id, content)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (updatedComment) => {
          this.replaceComment(
            this.comments,
            this.mapFeedComment(updatedComment)
          );

          this.cancelEditing();
        },

        error: (error) => {
          this.errorMessage = error?.message ?? 'Unable to update the comment.';
        },
      });
  }

  deleteComment(comment: CommentResponse): void {
    if (!comment.id) {
      return;
    }

    const confirmed = window.confirm(
      'Delete this comment?'
    );

    if (!confirmed) {
      return;
    }

    this.feedApi
      .deleteComment(comment.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          const removed = this.removeComment(
            this.comments,
            comment.id
          );

          if (removed) {
            this.totalComments = Math.max(
              0,
              this.totalComments - 1
            );

            this.commentCountChanged.emit(
              this.totalComments
            );
          }
        },

        error: (error) => {
          this.errorMessage = error?.message ?? 'Unable to delete the comment.';
        },
      });
  }

  toggleLike(comment: CommentResponse): void {
    if (!comment.id) {
      return;
    }

    const wasLiked =
      comment.isLikedByCurrentUser;

    /*
     * Optimistic UI update.
     * The UI changes immediately and is reverted
     * if the BFF request fails.
     */
    comment.isLikedByCurrentUser = !wasLiked;

    comment.likeCount = Math.max(
      0,
      comment.likeCount + (wasLiked ? -1 : 1)
    );

    const request$ = wasLiked
      ? this.feedApi.unlikeComment(comment.id)
      : this.feedApi.likeComment(comment.id);

    request$
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        error: (error) => {
          comment.isLikedByCurrentUser = wasLiked;

          comment.likeCount = Math.max(
            0,
            comment.likeCount + (wasLiked ? 1 : -1)
          );

          this.errorMessage = error?.message ?? 'Unable to update comment like.';
        },
      });
  }

  isCommentAuthor(
    comment: CommentResponse
  ): boolean {
    /*
     * The parent Home/Post component can provide the
     * current user ID later if needed.
     *
     * For now the BFF remains the authority for
     * authorization. The edit/delete controls are
     * therefore intentionally not rendered unless
     * the parent author ID matches.
     */
    return (
      !!this.postAuthorId &&
      comment.author?.id === this.postAuthorId
    );
  }

  getDisplayName(
    author: CommentResponse['author'] | null | undefined
  ): string {
    return author?.displayName?.trim() || 'User';
  }

  getInitial(
    author: CommentResponse['author'] | null | undefined
  ): string {
    return this.getDisplayName(author)
      .charAt(0)
      .toUpperCase();
  }

  formatDate(date: string): string {
    const created = new Date(date);

    if (Number.isNaN(created.getTime())) {
      return '';
    }

    const now = Date.now();
    const difference =
      now - created.getTime();

    const minute = 60 * 1000;
    const hour = 60 * minute;
    const day = 24 * hour;

    if (difference < minute) {
      return 'just now';
    }

    if (difference < hour) {
      return `${Math.floor(
        difference / minute
      )}m ago`;
    }

    if (difference < day) {
      return `${Math.floor(
        difference / hour
      )}h ago`;
    }

    if (difference < 7 * day) {
      return `${Math.floor(
        difference / day
      )}d ago`;
    }

    return created.toLocaleDateString();
  }

  trackByCommentId(
    _: number,
    comment: CommentResponse
  ): string {
    return comment.id;
  }

  private addReplyToComment(
    comments: CommentResponse[],
    reply: CommentResponse
  ): boolean {
    for (const comment of comments) {
      if (comment.id === reply.parentCommentId) {
        comment.replies = [
          ...(comment.replies ?? []),
          reply,
        ];

        return true;
      }

      if (
        comment.replies?.length &&
        this.addReplyToComment(
          comment.replies,
          reply
        )
      ) {
        return true;
      }
    }

    return false;
  }

  private replaceComment(
    comments: CommentResponse[],
    updated: CommentResponse
  ): boolean {
    for (let index = 0; index < comments.length; index++) {
      if (comments[index].id === updated.id) {
        comments[index] = updated;
        return true;
      }

      if (
        comments[index].replies?.length &&
        this.replaceComment(
          comments[index].replies!,
          updated
        )
      ) {
        return true;
      }
    }

    return false;
  }

  private removeComment(
    comments: CommentResponse[],
    commentId: string
  ): boolean {
    const index = comments.findIndex(
      (comment) => comment.id === commentId
    );

    if (index !== -1) {
      comments.splice(index, 1);
      return true;
    }

    for (const comment of comments) {
      if (comment.replies?.length) {
        if (
          this.removeComment(
            comment.replies,
            commentId
          )
        ) {
          return true;
        }
      }
    }

    return false;
  }
}

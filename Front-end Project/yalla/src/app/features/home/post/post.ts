import {
  Component,
  EventEmitter,
  Input,
  OnDestroy,
  Output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { FeedApiService } from '../../../core/services/feed-api.service';

export interface PostAuthor {
  id: string;
  displayName: string | null;
  avatarUrl: string | null;
}

export interface PostAttachment {
  id: string;
  fileName: string | null;
  fileUrl: string | null;
  contentType: string | null;
  fileSize: number;
  uploadedAt: string;
}

export interface PostResponse {
  id: string;
  groupId: string;
  content: string | null;
  isPinned: boolean;
  author: PostAuthor;
  likeCount: number;
  commentCount: number;
  isLikedByCurrentUser: boolean;
  attachments: PostAttachment[] | null;
  createdAt: string;
  updatedAt: string | null;
}

@Component({
  selector: 'app-post',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './post.html',
  styleUrls: ['./post.scss'],
})
export class PostComponent implements OnDestroy {
  @Input({ required: true })
  post!: PostResponse;

  @Input()
  currentUserId: string | null = null;

  @Output()
  readonly commentsClicked =
    new EventEmitter<PostResponse>();

  @Output()
  readonly postDeleted =
    new EventEmitter<string>();

  @Output()
  readonly likeChanged =
    new EventEmitter<PostResponse>();

  @Output()
  readonly reportClicked =
    new EventEmitter<PostResponse>();

  menuOpen = false;

  isLiking = false;
  isDeleting = false;

  errorMessage = '';

  private readonly destroy$ =
    new Subject<void>();

  constructor(
    private readonly feedApi: FeedApiService
  ) {}

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  toggleMenu(event?: Event): void {
    event?.stopPropagation();

    if (this.isDeleting) {
      return;
    }

    this.menuOpen = !this.menuOpen;
  }

  closeMenu(): void {
    this.menuOpen = false;
  }

  openComments(): void {
    this.closeMenu();
    this.commentsClicked.emit(this.post);
  }

  toggleLike(): void {
    if (
      !this.post?.id ||
      this.isLiking ||
      this.isDeleting
    ) {
      return;
    }

    this.closeMenu();
    this.errorMessage = '';

    const wasLiked =
      this.post.isLikedByCurrentUser;

    /*
     * Optimistic update.
     */
    this.post.isLikedByCurrentUser = !wasLiked;

    this.post.likeCount = Math.max(
      0,
      this.post.likeCount +
        (wasLiked ? -1 : 1)
    );

    this.isLiking = true;

    const request$ = wasLiked
      ? this.feedApi.unlike(this.post.id)
      : this.feedApi.like(this.post.id);

    request$
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.isLiking = false;
          this.likeChanged.emit(this.post);
        },

        error: (error) => {
          /*
           * Roll back optimistic state.
           */
          this.post.isLikedByCurrentUser =
            wasLiked;

          this.post.likeCount = Math.max(
            0,
            this.post.likeCount +
              (wasLiked ? 1 : -1)
          );

          this.errorMessage =
            error?.message ?? 'Unable to update the post like.';

          this.isLiking = false;
        },
      });
  }

  deletePost(): void {
    this.closeMenu();

    if (
      !this.post?.id ||
      this.isDeleting
    ) {
      return;
    }

    const confirmed = window.confirm(
      'Are you sure you want to delete this post?'
    );

    if (!confirmed) {
      return;
    }

    this.errorMessage = '';
    this.isDeleting = true;

    this.feedApi
      .deletePost(this.post.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.isDeleting = false;
          this.postDeleted.emit(
            this.post.id
          );
        },

        error: (error) => {
          this.errorMessage =
            error?.message ?? 'Unable to delete the post.';

          this.isDeleting = false;
        },
      });
  }

  reportPost(): void {
    this.closeMenu();
    this.reportClicked.emit(this.post);
  }

  isOwnPost(): boolean {
    return (
      !!this.currentUserId &&
      !!this.post?.author?.id &&
      this.currentUserId ===
        this.post.author.id
    );
  }

  getDisplayName(): string {
    return (
      this.post?.author?.displayName?.trim() ||
      'User'
    );
  }

  getInitial(): string {
    return this.getDisplayName()
      .charAt(0)
      .toUpperCase();
  }

  getAttachmentUrl(
    attachment: PostAttachment
  ): string {
    return (
      attachment.fileUrl?.trim() ||
      ''
    );
  }

  isImage(
    attachment: PostAttachment
  ): boolean {
    return (
      attachment.contentType?.startsWith(
        'image/'
      ) ?? false
    );
  }

  isVideo(
    attachment: PostAttachment
  ): boolean {
    return (
      attachment.contentType?.startsWith(
        'video/'
      ) ?? false
    );
  }

  isPdf(
    attachment: PostAttachment
  ): boolean {
    return (
      attachment.contentType ===
      'application/pdf'
    );
  }

  getOtherAttachmentIcon(
    attachment: PostAttachment
  ): string {
    const contentType =
      attachment.contentType ?? '';

    if (
      contentType.includes('word') ||
      contentType.includes('document')
    ) {
      return 'description';
    }

    if (
      contentType.includes('excel') ||
      contentType.includes('spreadsheet')
    ) {
      return 'table_chart';
    }

    if (
      contentType.includes('zip') ||
      contentType.includes('compressed')
    ) {
      return 'folder_zip';
    }

    return 'insert_drive_file';
  }

  formatDate(
    date: string
  ): string {
    const created =
      new Date(date);

    if (
      Number.isNaN(
        created.getTime()
      )
    ) {
      return '';
    }

    const now = Date.now();

    const difference =
      now - created.getTime();

    const minute =
      60 * 1000;

    const hour =
      60 * minute;

    const day =
      24 * hour;

    if (
      difference < minute
    ) {
      return 'just now';
    }

    if (
      difference < hour
    ) {
      return `${Math.floor(
        difference / minute
      )}m ago`;
    }

    if (
      difference < day
    ) {
      return `${Math.floor(
        difference / hour
      )}h ago`;
    }

    if (
      difference <
      7 * day
    ) {
      return `${Math.floor(
        difference / day
      )}d ago`;
    }

    return created.toLocaleDateString(
      undefined,
      {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
      }
    );
  }

  trackByAttachmentId(
    _: number,
    attachment: PostAttachment
  ): string {
    return attachment.id;
  }

  }

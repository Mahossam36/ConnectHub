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
import { Subject, takeUntil } from 'rxjs';
import { FeedApiService } from '../../../core/services/feed-api.service';
import { resolveMediaUrl } from '../../../core/utils/media-url.util';

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

const IMAGE_EXTENSIONS = ['jpg', 'jpeg', 'png', 'gif', 'webp', 'bmp', 'svg', 'avif'];
const VIDEO_EXTENSIONS = ['mp4', 'webm', 'ogg', 'mov', 'm4v'];

@Component({
  selector: 'app-post',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './post.html',
  styleUrls: ['./post.scss'],
})
export class PostComponent implements OnChanges, OnDestroy {
  @Input({ required: true })
  post!: PostResponse;

  @Input()
  currentUserId: string | null = null;

  @Output()
  readonly commentsClicked = new EventEmitter<PostResponse>();

  @Output()
  readonly postDeleted = new EventEmitter<string>();

  @Output()
  readonly likeChanged = new EventEmitter<PostResponse>();

  @Output()
  readonly reportClicked = new EventEmitter<PostResponse>();

  menuOpen = false;

  isLiking = false;
  isDeleting = false;

  errorMessage = '';

  /*
   * ATTACHMENT MEDIA
   * =====================================================
   * /api/Attachments/{id} is an authenticated endpoint — it requires
   * the session cookie. A plain <img>/<video> src pointing straight at
   * it won't reliably send that cookie cross-origin, so instead we
   * fetch each image/video attachment as a Blob through HttpClient
   * (which does attach the cookie, same as every other API call) and
   * expose it to the template as an object URL.
   */
  private readonly attachmentObjectUrls = new Map<string, string>();
  private readonly loadedAttachmentIds = new Set<string>();

  private readonly destroy$ = new Subject<void>();

  constructor(private readonly feedApi: FeedApiService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['post']) {
      this.loadAttachmentMedia();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.revokeAllObjectUrls();
  }

  private loadAttachmentMedia(): void {
    const attachments = this.post?.attachments ?? [];

    for (const attachment of attachments) {
      if (!attachment?.id || this.loadedAttachmentIds.has(attachment.id)) {
        continue;
      }

      if (!this.isImage(attachment) && !this.isVideo(attachment)) {
        continue;
      }

      this.loadedAttachmentIds.add(attachment.id);

      this.feedApi
        .getAttachmentBlob(attachment.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (blob) => {
            const objectUrl = URL.createObjectURL(blob);
            this.attachmentObjectUrls.set(attachment.id, objectUrl);
          },
          error: () => {
            // Leave it unresolved; the template hides the element
            // when there's no resolved URL for this attachment.
            this.loadedAttachmentIds.delete(attachment.id);
          },
        });
    }
  }

  private revokeAllObjectUrls(): void {
    for (const url of this.attachmentObjectUrls.values()) {
      URL.revokeObjectURL(url);
    }
    this.attachmentObjectUrls.clear();
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
    if (!this.post?.id || this.isLiking || this.isDeleting) {
      return;
    }

    this.closeMenu();
    this.errorMessage = '';

    const wasLiked = this.post.isLikedByCurrentUser;

    /*
     * Optimistic update.
     */
    this.post.isLikedByCurrentUser = !wasLiked;

    this.post.likeCount = Math.max(0, this.post.likeCount + (wasLiked ? -1 : 1));

    this.isLiking = true;

    const request$ = wasLiked ? this.feedApi.unlike(this.post.id) : this.feedApi.like(this.post.id);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.isLiking = false;
        this.likeChanged.emit(this.post);
      },

      error: (error) => {
        /*
         * Roll back optimistic state.
         */
        this.post.isLikedByCurrentUser = wasLiked;

        this.post.likeCount = Math.max(0, this.post.likeCount + (wasLiked ? 1 : -1));

        this.errorMessage = error?.message ?? 'Unable to update the post like.';

        this.isLiking = false;
      },
    });
  }

  deletePost(): void {
    this.closeMenu();

    if (!this.post?.id || this.isDeleting) {
      return;
    }

    const confirmed = window.confirm('Are you sure you want to delete this post?');

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
          this.postDeleted.emit(this.post.id);
        },

        error: (error) => {
          this.errorMessage = error?.message ?? 'Unable to delete the post.';

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
      !!this.currentUserId && !!this.post?.author?.id && this.currentUserId === this.post.author.id
    );
  }

  getDisplayName(): string {
    return this.post?.author?.displayName?.trim() || 'User';
  }

  getInitial(): string {
    return this.getDisplayName().charAt(0).toUpperCase();
  }

  /**
   * Resolved, authenticated object URL for an image/video attachment.
   * Returns '' until the blob has finished loading — the template only
   * renders the <img>/<video> once this is non-empty.
   */
  getAttachmentUrl(attachment: PostAttachment): string {
    return this.attachmentObjectUrls.get(attachment.id) ?? '';
  }

  getAuthorAvatarUrl(): string {
    // Profile avatars are currently served as plain static URLs (not
    // through /api/Attachments/{id}), so this can stay as a direct
    // resolved URL. If avatars stop rendering too, they'll need the
    // same authenticated-blob treatment as post attachments above.
    return resolveMediaUrl(this.post?.author?.avatarUrl);
  }

  /*
   * IMAGE / VIDEO DETECTION
   * =====================================================
   * The backend doesn't always populate `contentType` on
   * attachments (it's nullable in the DTO). Relying on contentType
   * alone silently drops images to the generic "file" branch, or
   * hides them entirely, whenever it's null/empty.
   *
   * We now trust contentType when it's present, and fall back to
   * sniffing the file extension from fileName otherwise.
   */

  private getExtension(attachment: PostAttachment): string {
    const source = attachment.fileName || attachment.fileUrl || '';
    const match = source
      .split('?')[0]
      .split('#')[0]
      .match(/\.([a-zA-Z0-9]+)$/);
    return match ? match[1].toLowerCase() : '';
  }

  isImage(attachment: PostAttachment): boolean {
    if (attachment.contentType) {
      return attachment.contentType.startsWith('image/');
    }
    return IMAGE_EXTENSIONS.includes(this.getExtension(attachment));
  }

  isVideo(attachment: PostAttachment): boolean {
    if (attachment.contentType) {
      return attachment.contentType.startsWith('video/');
    }
    return VIDEO_EXTENSIONS.includes(this.getExtension(attachment));
  }

  isPdf(attachment: PostAttachment): boolean {
    if (attachment.contentType) {
      return attachment.contentType === 'application/pdf';
    }
    return this.getExtension(attachment) === 'pdf';
  }

  getOtherAttachmentIcon(attachment: PostAttachment): string {
    const contentType = attachment.contentType ?? '';

    if (contentType.includes('word') || contentType.includes('document')) {
      return 'description';
    }

    if (contentType.includes('excel') || contentType.includes('spreadsheet')) {
      return 'table_chart';
    }

    if (contentType.includes('zip') || contentType.includes('compressed')) {
      return 'folder_zip';
    }

    return 'insert_drive_file';
  }

  formatDate(date: string): string {
    const created = new Date(date);

    if (Number.isNaN(created.getTime())) {
      return '';
    }

    const now = Date.now();

    const difference = now - created.getTime();

    const minute = 60 * 1000;

    const hour = 60 * minute;

    const day = 24 * hour;

    if (difference < minute) {
      return 'just now';
    }

    if (difference < hour) {
      return `${Math.floor(difference / minute)}m ago`;
    }

    if (difference < day) {
      return `${Math.floor(difference / hour)}h ago`;
    }

    if (difference < 7 * day) {
      return `${Math.floor(difference / day)}d ago`;
    }

    return created.toLocaleDateString(undefined, {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  }

  trackByAttachmentId(_: number, attachment: PostAttachment): string {
    return attachment.id;
  }
}

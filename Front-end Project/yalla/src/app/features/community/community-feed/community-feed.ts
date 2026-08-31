import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
  inject,
  signal,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { SessionService } from '../../../core/auth/session.service';
import { CommunityDetail, FeedPost, ReportTargetType } from '../../../core/models/feed.models';
import { FeedApiService } from '../../../core/services/feed-api.service';
import { environment } from '../../../../environments/environment';
import { ActiveTab } from '../community/community.component';

@Component({
  selector: 'app-community-feed',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe],
  templateUrl: './community-feed.html',
  styleUrl: './community-feed.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommunityFeedComponent {
  readonly session = inject(SessionService);
  private readonly feedApi = inject(FeedApiService);

  readonly ReportTargetType = ReportTargetType;

  // ── Inputs ─────────────────────────────────────────────────────────────────
  @Input() community: CommunityDetail | null = null;
  @Input() posts: FeedPost[] = [];
  @Input() canModerate = false;
  @Input() canAnnounce = false;
  @Input() isOwner = false;
  @Input() loading = false;

  // ── Outputs ────────────────────────────────────────────────────────────────
  @Output() postCreated = new EventEmitter<void>();
  @Output() postDeleted = new EventEmitter<string>();
  @Output() postsUpdated = new EventEmitter<FeedPost[]>();
  @Output() openComments = new EventEmitter<FeedPost>();
  @Output() commentCountChanged = new EventEmitter<{ postId: string; count: number }>();
  @Output() confirmDialogRequest = new EventEmitter<{
    title: string;
    message: string;
    confirmText?: string;
    confirmAction: () => void;
  }>();
  @Output() feedbackRequest = new EventEmitter<{ text: string; type: 'success' | 'error' }>();
  @Output() tabSwitchRequest = new EventEmitter<ActiveTab>();
  @Output() shareRequested = new EventEmitter<void>();
  @Output() reportRequested = new EventEmitter<{ type: number; id: string; name?: string }>();

  // ── Local State ────────────────────────────────────────────────────────────
  readonly postText = new FormControl('', { nonNullable: true });
  readonly pendingFile = signal<File | null>(null);
  readonly pendingPreviewUrl = signal<string | null>(null);
  readonly announcementMode = signal<boolean>(false);
  readonly failedMediaUrls = signal<Set<string>>(new Set());

  // ── File Handling ──────────────────────────────────────────────────────────
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
    const file = input.files[0];
    if (!file.type.startsWith('image/') && !file.type.startsWith('video/')) {
      this.feedbackRequest.emit({
        text: 'Only photo and video attachments are allowed.',
        type: 'error',
      });
      return;
    }
    this.pendingFile.set(file);
    if (file.type.startsWith('image/')) {
      const reader = new FileReader();
      reader.onload = (e) => this.pendingPreviewUrl.set(e.target?.result as string);
      reader.readAsDataURL(file);
    } else {
      this.pendingPreviewUrl.set(URL.createObjectURL(file));
    }
  }

  removePendingFile(): void {
    this.pendingFile.set(null);
    this.pendingPreviewUrl.set(null);
  }

  triggerAnnouncement(): void {
    if (!this.canAnnounce) return;
    this.announcementMode.set(true);
  }

  // ── Create Post ────────────────────────────────────────────────────────────
  createPost(): void {
    const comm = this.community;
    const text = this.postText.value.trim();
    if (!comm || (!text && !this.pendingFile())) return;

    const file = this.pendingFile();
    if (file) {
      this.feedApi.uploadAttachment(file).subscribe({
        next: (attachment) => this.submitPost(comm.id, text, [attachment.id]),
        error: () =>
          this.feedbackRequest.emit({ text: 'Failed to upload media attachment.', type: 'error' }),
      });
    } else {
      this.submitPost(comm.id, text, []);
    }
  }

  private submitPost(groupId: string, content: string, attachmentIds: string[]): void {
    const isAnnounce = this.announcementMode();
    this.feedApi.createPost(groupId, { content, attachmentIds }).subscribe({
      next: (post) => {
        this.postText.setValue('');
        this.removePendingFile();
        if (isAnnounce) {
          this.feedApi.pinPost(post.id).subscribe({
            next: () => {
              this.feedbackRequest.emit({
                text: 'Announcement published and pinned!',
                type: 'success',
              });
              this.announcementMode.set(false);
              this.postCreated.emit();
            },
            error: () => {
              this.feedbackRequest.emit({
                text: 'Post created but could not be pinned.',
                type: 'error',
              });
              this.announcementMode.set(false);
              this.postCreated.emit();
            },
          });
        } else {
          this.feedbackRequest.emit({ text: 'Post created successfully.', type: 'success' });
          this.postCreated.emit();
        }
      },
      error: () => this.feedbackRequest.emit({ text: 'Failed to create post.', type: 'error' }),
    });
  }

  // ── Post Actions ───────────────────────────────────────────────────────────
  toggleLike(post: FeedPost): void {
    const op = post.isLikedByCurrentUser
      ? this.feedApi.unlike(post.id)
      : this.feedApi.like(post.id);

    op.subscribe({
      next: () => {
        const updated = this.posts.map((p) =>
          p.id === post.id
            ? {
                ...p,
                isLikedByCurrentUser: !post.isLikedByCurrentUser,
                likeCount: post.likeCount + (post.isLikedByCurrentUser ? -1 : 1),
              }
            : p,
        );
        this.postsUpdated.emit(updated);
      },
    });
  }

  deletePost(postId: string): void {
    this.confirmDialogRequest.emit({
      title: 'Delete Post',
      message: 'Are you sure you want to delete this post? This action cannot be undone.',
      confirmText: 'Delete',
      confirmAction: () => {
        this.feedApi.deletePost(postId).subscribe({
          next: () => {
            this.postDeleted.emit(postId);
            this.feedbackRequest.emit({ text: 'Post deleted.', type: 'success' });
          },
          error: () => this.feedbackRequest.emit({ text: 'Failed to delete post.', type: 'error' }),
        });
      },
    });
  }

  togglePin(post: FeedPost): void {
    if (!this.canAnnounce) return;
    const op = post.isPinned ? this.feedApi.unpinPost(post.id) : this.feedApi.pinPost(post.id);
    op.subscribe({
      next: () => {
        const updated = this.posts.map((p) =>
          p.id === post.id ? { ...p, isPinned: !post.isPinned } : p,
        );
        this.postsUpdated.emit(updated);
        this.feedbackRequest.emit({
          text: post.isPinned ? 'Post unpinned.' : 'Post pinned to top.',
          type: 'success',
        });
      },
      error: () =>
        this.feedbackRequest.emit({
          text: post.isPinned ? 'Failed to unpin post.' : 'Failed to pin post.',
          type: 'error',
        }),
    });
  }

  openReportModal(type: number, id: string, name?: string): void {
    this.reportRequested.emit({ type, id, name });
  }

  // ── Media Helpers ──────────────────────────────────────────────────────────
  resolveMediaUrl(url?: string | null): string {
    if (!url) return '';
    if (url.startsWith('http://') || url.startsWith('https://')) return url;
    const base = environment.bff.baseUrl.replace(/\/$/, '');
    return `${base}/${url.replace(/^\//, '')}`;
  }

  getAttachmentMediaUrl(attachment: {
    id?: string;
    fileUrl?: string | null;
    filePath?: string | null;
  }): string {
    const base = environment.bff.baseUrl.replace(/\/$/, '');
    if (attachment.fileUrl) return this.resolveMediaUrl(attachment.fileUrl);
    if (attachment.filePath) return this.resolveMediaUrl(attachment.filePath);
    if (attachment.id) return `${base}/api/Attachments/${attachment.id}`;
    return '';
  }

  isImage(attachment: {
    fileName?: string | null;
    fileUrl?: string | null;
    filePath?: string | null;
    contentType?: string | null;
  }): boolean {
    if (attachment.contentType?.startsWith('image/')) return true;
    const name = (
      attachment.fileName ||
      attachment.fileUrl ||
      attachment.filePath ||
      ''
    ).toLowerCase();
    return /\.(jpg|jpeg|png|gif|webp|bmp|svg|avif)$/i.test(name);
  }

  isVideo(attachment: {
    fileName?: string | null;
    fileUrl?: string | null;
    filePath?: string | null;
    contentType?: string | null;
  }): boolean {
    if (attachment.contentType?.startsWith('video/')) return true;
    const name = (
      attachment.fileName ||
      attachment.fileUrl ||
      attachment.filePath ||
      ''
    ).toLowerCase();
    return /\.(mp4|webm|ogg|mov|m4v|mkv)$/i.test(name);
  }

  onMediaError(url: string): void {
    if (!url) return;
    this.failedMediaUrls.update((set) => new Set(set).add(url));
  }

  hasMediaError(url: string): boolean {
    return this.failedMediaUrls().has(url);
  }

  currentUserId(): string | undefined {
    return this.session.session()?.user?.id;
  }
}

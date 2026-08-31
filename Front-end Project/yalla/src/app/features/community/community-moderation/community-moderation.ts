import {
  ChangeDetectionStrategy,
  Component,
  computed,
  EventEmitter,
  Input,
  Output,
  signal,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { inject } from '@angular/core';
import {
  CommunityDetail,
  FeedPost,
  Report,
  ReportStatus,
  ReportTargetType,
} from '../../../core/models/feed.models';
import { FeedApiService } from '../../../core/services/feed-api.service';
import { ActiveTab } from '../community/community.component';

type ModerationFilter = 'All' | 'Pending' | 'Resolved' | 'Dismissed';

@Component({
  selector: 'app-community-moderation',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './community-moderation.html',
  styleUrl: './community-moderation.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommunityModerationComponent {
  private readonly feedApi = inject(FeedApiService);

  readonly ReportStatus = ReportStatus;
  readonly ReportTargetType = ReportTargetType;

  // ── Inputs ─────────────────────────────────────────────────────────────────
  @Input() communityId = '';
  @Input() community: CommunityDetail | null = null;
  @Input() reports: Report[] = [];
  @Input() posts: FeedPost[] = [];
  @Input() canModerate = false;

  // ── Outputs ────────────────────────────────────────────────────────────────
  @Output() reportsChanged = new EventEmitter<void>();
  @Output() postsChanged = new EventEmitter<void>();
  @Output() membersChanged = new EventEmitter<void>();
  @Output() openComments = new EventEmitter<FeedPost>();
  @Output() tabSwitchRequest = new EventEmitter<ActiveTab>();
  @Output() confirmDialogRequest = new EventEmitter<{
    title: string;
    message: string;
    confirmText?: string;
    confirmAction: () => void;
  }>();
  @Output() feedbackRequest = new EventEmitter<{ text: string; type: 'success' | 'error' }>();

  // ── Local State ────────────────────────────────────────────────────────────
  readonly moderationFilter = signal<ModerationFilter>('All');

  // ── Computed ───────────────────────────────────────────────────────────────
  readonly filteredReports = computed(() => {
    const f = this.moderationFilter();
    return this.reports.filter((r) => {
      const status = this.statusNum(r.status);
      if (f === 'Pending') return status === ReportStatus.Pending;
      if (f === 'Resolved') return status === ReportStatus.ActionTaken;
      if (f === 'Dismissed') return status === ReportStatus.Dismissed;
      return true;
    });
  });

  readonly pendingCount = computed(
    () => this.reports.filter((r) => this.statusNum(r.status) === ReportStatus.Pending).length,
  );

  // ── Actions ────────────────────────────────────────────────────────────────
  dismissReport(report: Report): void {
    this.feedApi.resolveReport(report.id, ReportStatus.Dismissed).subscribe({
      next: () => {
        this.feedbackRequest.emit({ text: 'Report dismissed.', type: 'success' });
        this.reportsChanged.emit();
      },
      error: () => this.feedbackRequest.emit({ text: 'Failed to dismiss report.', type: 'error' }),
    });
  }

  removeReportedContent(report: Report): void {
    const targetType = this.targetNum(report.targetType);
    this.confirmDialogRequest.emit({
      title: 'Remove Reported Content',
      message: 'Are you sure you want to permanently remove this content?',
      confirmText: 'Remove Content',
      confirmAction: () => {
        const deleteOp =
          targetType === ReportTargetType.Post
            ? this.feedApi.deletePost(report.targetId)
            : this.feedApi.deleteComment(report.targetId);

        deleteOp.subscribe({
          next: () => {
            this.feedApi.resolveReport(report.id, ReportStatus.ActionTaken).subscribe({
              next: () => {
                this.feedbackRequest.emit({
                  text: 'Reported content removed and report resolved.',
                  type: 'success',
                });
                this.reportsChanged.emit();
                this.postsChanged.emit();
              },
            });
          },
          error: () =>
            this.feedbackRequest.emit({
              text: 'Failed to remove reported content.',
              type: 'error',
            }),
        });
      },
    });
  }

  removeReportedUser(report: Report): void {
    const comm = this.community;
    if (!comm) return;
    const authorId = report.contentAuthor?.id || report.reportedBy?.id;
    if (!authorId) {
      this.feedbackRequest.emit({ text: 'User identifier is not available.', type: 'error' });
      return;
    }

    this.confirmDialogRequest.emit({
      title: 'Remove User from Community',
      message: `Are you sure you want to remove this user from ${comm.name}?`,
      confirmText: 'Remove User',
      confirmAction: () => {
        this.feedApi.removeMember(comm.id, authorId).subscribe({
          next: () => {
            this.feedApi.resolveReport(report.id, ReportStatus.ActionTaken).subscribe({
              next: () => {
                this.feedbackRequest.emit({
                  text: 'User removed and report resolved.',
                  type: 'success',
                });
                this.reportsChanged.emit();
                this.membersChanged.emit();
              },
            });
          },
          error: () =>
            this.feedbackRequest.emit({
              text: 'Failed to remove user from community.',
              type: 'error',
            }),
        });
      },
    });
  }

  viewReportedContent(report: Report): void {
    this.tabSwitchRequest.emit('tab-posts');
    const targetType = this.targetNum(report.targetType);
    if (targetType === ReportTargetType.Post) {
      setTimeout(() => {
        const el = document.getElementById(`post-${report.targetId}`);
        if (el) el.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }, 100);
    } else {
      const post = this.posts.find((p) => p.id === report.postId);
      if (post) this.openComments.emit(post);
    }
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  statusNum(status?: ReportStatus | number | string): number {
    if (typeof status === 'number') return status;
    if (status === 'Pending' || status === '1') return ReportStatus.Pending;
    if (status === 'ActionTaken' || status === 'Resolved' || status === '2')
      return ReportStatus.ActionTaken;
    if (status === 'Dismissed' || status === '3') return ReportStatus.Dismissed;
    return 1;
  }

  targetNum(type?: ReportTargetType | number | string): number {
    if (typeof type === 'number') return type;
    if (type === 'Post' || type === '1') return ReportTargetType.Post;
    if (type === 'Comment' || type === '2') return ReportTargetType.Comment;
    return 1;
  }

  statusLabel(status?: ReportStatus | number | string): string {
    const n = this.statusNum(status);
    if (n === ReportStatus.ActionTaken) return 'Resolved';
    if (n === ReportStatus.Dismissed) return 'Dismissed';
    return 'Pending';
  }
}

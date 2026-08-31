import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { SessionService } from '../../../core/auth/session.service';
import {
  CommunityDetail,
  FeedComment,
  FeedPost,
  GroupMember,
  GroupRole,
  Report,
  ReportStatus,
  ReportTargetType,
} from '../../../core/models/feed.models';
import { FeedApiService } from '../../../core/services/feed-api.service';
import { CommentsPanelComponent } from '../../home/comments-panel/comments-panel';
import { NavbarComponent } from '../../common/navbar/navbar/navbar';
import { SidePanelComponent } from '../../common/side-panel/side-panel';
import { ToastComponent } from '../../common/toast/toast';
import { environment } from '../../../../environments/environment';

import { CommunityHeaderComponent } from '../community-header/community-header';
import { CommunityFeedComponent } from '../community-feed/community-feed';
import { CommunityMembersComponent } from '../community-members/community-members';
import { CommunityModerationComponent } from '../community-moderation/community-moderation';
import { CommunityAnalyticsComponent } from '../community-analytics/community-analytics';

export type ActiveTab = 'tab-posts' | 'tab-members' | 'tab-moderation' | 'tab-analytics';

@Component({
  selector: 'app-community',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    DatePipe,
    CommentsPanelComponent,
    NavbarComponent,
    SidePanelComponent,
    ToastComponent,
    CommunityHeaderComponent,
    CommunityFeedComponent,
    CommunityMembersComponent,
    CommunityModerationComponent,
    CommunityAnalyticsComponent,
  ],
  templateUrl: './community.component.html',
  styleUrl: './community.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommunityComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly feedApi = inject(FeedApiService);
  readonly session = inject(SessionService);
  private readonly auth = inject(AuthService);

  readonly GroupRole = GroupRole;
  readonly ReportStatus = ReportStatus;
  readonly ReportTargetType = ReportTargetType;

  // ── State ──────────────────────────────────────────────────────────────────
  readonly communityId = signal<string>('');
  readonly community = signal<CommunityDetail | null>(null);
  readonly members = signal<GroupMember[]>([]);
  readonly posts = signal<FeedPost[]>([]);
  readonly reports = signal<Report[]>([]);

  readonly activeTab = signal<ActiveTab>('tab-posts');
  readonly navDrawerOpen = signal<boolean>(false);
  readonly loading = signal<boolean>(false);
  readonly feedbackMessage = signal<{ text: string; type: 'success' | 'error' } | null>(null);

  readonly commentsPost = signal<FeedPost | null>(null);

  readonly confirmDialog = signal<{
    title: string;
    message: string;
    confirmText?: string;
    confirmAction: () => void;
  } | null>(null);

  // ── Permissions ────────────────────────────────────────────────────────────
  readonly isOwner = computed(
    () => this.getRoleNumber(this.community()?.currentUserRole) === GroupRole.Owner,
  );
  readonly isAdmin = computed(
    () => this.getRoleNumber(this.community()?.currentUserRole) === GroupRole.Admin,
  );
  readonly isJoined = computed(
    () =>
      this.community()?.currentUserRole !== null && this.community()?.currentUserRole !== undefined,
  );
  readonly canModerate = computed(() => this.isOwner() || this.isAdmin());
  readonly canAnnounce = computed(() => this.isOwner() || this.isAdmin());

  // ── Derived ────────────────────────────────────────────────────────────────
  readonly pendingReportsCount = computed(
    () =>
      this.reports().filter((r) => this.getReportStatusNumber(r.status) === ReportStatus.Pending)
        .length,
  );

  readonly totalCommentsCount = computed(() =>
    this.posts().reduce((acc, p) => acc + (p.commentCount || 0), 0),
  );

  // ── Lifecycle ──────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      if (id) {
        this.communityId.set(id);
        this.loadCommunity(id);
      }
    });
  }

  // ── Data Loading ───────────────────────────────────────────────────────────
  loadCommunity(id: string): void {
    this.loading.set(true);
    this.feedApi.getCommunityDetails(id).subscribe({
      next: (comm) => {
        this.community.set(comm);
        this.loadPosts(id);
        this.loadMembers(id);
        if (this.getRoleNumber(comm.currentUserRole) >= GroupRole.Admin) {
          this.loadReports();
        }
        this.loading.set(false);
      },
      error: () => {
        this.showFeedback('Failed to load community details.', 'error');
        this.loading.set(false);
      },
    });
  }

  loadPosts(id: string): void {
    this.feedApi.getGroupFeed(id).subscribe({
      next: (posts) => this.posts.set(posts),
      error: () => this.showFeedback('Failed to load community posts.', 'error'),
    });
  }

  loadMembers(id: string): void {
    this.feedApi.getCommunityMembers(id).subscribe({
      next: (members) => this.members.set(members),
      error: () => this.showFeedback('Failed to load community members.', 'error'),
    });
  }

  loadReports(): void {
    this.feedApi.getReports().subscribe({
      next: (reports) => this.reports.set(reports),
      error: () => {},
    });
  }

  // ── Tab Navigation ─────────────────────────────────────────────────────────
  switchTab(tab: ActiveTab): void {
    if (tab === 'tab-moderation' && !this.canModerate()) return;
    this.activeTab.set(tab);
  }

  // ── Membership ─────────────────────────────────────────────────────────────
  toggleJoin(): void {
    const comm = this.community();
    if (!comm) return;
    if (this.isJoined()) {
      this.feedApi.leaveCommunity(comm.id).subscribe({
        next: () => {
          this.showFeedback('You have left the community.', 'success');
          this.loadCommunity(comm.id);
        },
        error: () => this.showFeedback('Failed to leave community.', 'error'),
      });
    } else {
      this.feedApi.joinCommunity(comm.id).subscribe({
        next: () => {
          this.showFeedback('You have joined the community!', 'success');
          this.loadCommunity(comm.id);
        },
        error: () => this.showFeedback('Failed to join community.', 'error'),
      });
    }
  }

  shareCommunity(): void {
    void navigator.clipboard?.writeText(window.location.href);
    this.showFeedback('Community link copied to clipboard!', 'success');
  }

  // ── Post Actions (delegated up from CommunityFeedComponent) ───────────────
  onPostCreated(): void {
    const id = this.communityId();
    if (id) this.loadPosts(id);
  }

  onPostDeleted(postId: string): void {
    this.posts.update((list) => list.filter((p) => p.id !== postId));
  }

  onPostsUpdated(posts: FeedPost[]): void {
    this.posts.set(posts);
  }

  onOpenComments(post: FeedPost): void {
    this.commentsPost.set(post);
  }

  onCommentCountChanged(payload: { postId: string; count: number }): void {
    this.posts.update((list) =>
      list.map((p) => (p.id === payload.postId ? { ...p, commentCount: payload.count } : p)),
    );
  }

  onConfirmDialogRequest(dialog: {
    title: string;
    message: string;
    confirmText?: string;
    confirmAction: () => void;
  }): void {
    this.confirmDialog.set(dialog);
  }

  onFeedbackRequest(payload: { text: string; type: 'success' | 'error' }): void {
    this.showFeedback(payload.text, payload.type);
  }

  // ── Member Management (delegated from CommunityMembersComponent) ───────────
  onMemberChanged(): void {
    const id = this.communityId();
    if (id) {
      this.loadMembers(id);
      this.loadCommunity(id);
    }
  }

  // ── Moderation (delegated from CommunityModerationComponent) ──────────────
  onReportsChanged(): void {
    this.loadReports();
  }

  onModerationPostsChanged(): void {
    const id = this.communityId();
    if (id) this.loadPosts(id);
  }

  onModerationMembersChanged(): void {
    const id = this.communityId();
    if (id) {
      this.loadMembers(id);
      this.loadCommunity(id);
    }
  }

  // ── Comments ───────────────────────────────────────────────────────────────
  closeComments(): void {
    this.commentsPost.set(null);
  }

  handleCommentCountChanged(count: number): void {
    const post = this.commentsPost();
    if (post) {
      this.posts.update((list) =>
        list.map((p) => (p.id === post.id ? { ...p, commentCount: count } : p)),
      );
    }
  }

  // ── Auth ───────────────────────────────────────────────────────────────────
  logout(): void {
    this.auth.logout().subscribe({
      next: () => this.router.navigateByUrl('/login'),
      error: () => {
        this.auth.clearSession();
        this.router.navigateByUrl('/login');
      },
    });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  getRoleNumber(role?: GroupRole | number | string | null): number {
    if (role === null || role === undefined) return 0;
    if (typeof role === 'number') return role;
    if (role === 'Owner' || role === '3') return GroupRole.Owner;
    if (role === 'Admin' || role === '2') return GroupRole.Admin;
    if (role === 'Member' || role === '1') return GroupRole.Member;
    return 0;
  }

  getReportStatusNumber(status?: ReportStatus | number | string): number {
    if (typeof status === 'number') return status;
    if (status === 'Pending' || status === '1') return ReportStatus.Pending;
    if (status === 'ActionTaken' || status === 'Resolved' || status === '2')
      return ReportStatus.ActionTaken;
    if (status === 'Dismissed' || status === '3') return ReportStatus.Dismissed;
    return 1;
  }

  showFeedback(text: string, type: 'success' | 'error'): void {
    this.feedbackMessage.set({ text, type });
    setTimeout(() => this.feedbackMessage.set(null), 4000);
  }

  resolveMediaUrl(url?: string | null): string {
    if (!url) return '';
    if (url.startsWith('http://') || url.startsWith('https://')) return url;
    const base = environment.bff.baseUrl.replace(/\/$/, '');
    return `${base}/${url.replace(/^\//, '')}`;
  }
}

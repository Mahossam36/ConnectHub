import {
  ChangeDetectionStrategy,
  Component,
  computed,
  EventEmitter,
  inject,
  Input,
  OnChanges,
  Output,
  signal,
  SimpleChanges,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { SessionService } from '../../../core/auth/session.service';
import { GroupMember, GroupRole } from '../../../core/models/feed.models';
import { FeedApiService } from '../../../core/services/feed-api.service';
import { environment } from '../../../../environments/environment';

type RoleFilter = 'All Roles' | 'Admins' | 'Members';

@Component({
  selector: 'app-community-members',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './community-members.html',
  styleUrl: './community-members.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommunityMembersComponent implements OnChanges {
  readonly session = inject(SessionService);
  private readonly feedApi = inject(FeedApiService);

  readonly GroupRole = GroupRole;

  // ── Inputs ─────────────────────────────────────────────────────────────────
  @Input() communityId = '';
  @Input() members: GroupMember[] = [];
  @Input() isOwner = false;

  // ── Outputs ────────────────────────────────────────────────────────────────
  @Output() memberChanged = new EventEmitter<void>();
  @Output() confirmDialogRequest = new EventEmitter<{
    title: string;
    message: string;
    confirmText?: string;
    confirmAction: () => void;
  }>();
  @Output() feedbackRequest = new EventEmitter<{ text: string; type: 'success' | 'error' }>();

  // ── Local State ────────────────────────────────────────────────────────────
  readonly membersSearch = new FormControl('', { nonNullable: true });
  readonly searchFilter = signal('');
  readonly roleFilter = signal<RoleFilter>('All Roles');
  readonly activeMenuMemberId = signal<string | null>(null);

  constructor() {
    this.membersSearch.valueChanges
      .pipe(debounceTime(250), distinctUntilChanged())
      .subscribe((val) => this.searchFilter.set(val));
  }

  ngOnChanges(_changes: SimpleChanges): void {
    // reset menu when members reload
    this.activeMenuMemberId.set(null);
  }

  // ── Filtered Lists ─────────────────────────────────────────────────────────
  readonly filteredMembers = computed(() => {
    const q = this.searchFilter().trim().toLowerCase();
    const rf = this.roleFilter();
    return this.members.filter((m) => {
      const nameMatch = (m.user.displayName + ' ' + (m.user.userName || ''))
        .toLowerCase()
        .includes(q);
      if (!nameMatch) return false;
      const role = this.roleNum(m.role);
      if (rf === 'Admins') return role === GroupRole.Admin;
      if (rf === 'Members') return role === GroupRole.Member;
      return true;
    });
  });

  readonly ownerMembers = computed(() =>
    this.filteredMembers().filter((m) => this.roleNum(m.role) === GroupRole.Owner),
  );
  readonly adminMembers = computed(() =>
    this.filteredMembers().filter((m) => this.roleNum(m.role) === GroupRole.Admin),
  );
  readonly normalMembers = computed(() =>
    this.filteredMembers().filter((m) => this.roleNum(m.role) === GroupRole.Member),
  );

  // ── Actions ────────────────────────────────────────────────────────────────
  toggleMemberMenu(memberId: string): void {
    this.activeMenuMemberId.update((curr) => (curr === memberId ? null : memberId));
  }

  canManageMember(member: GroupMember): boolean {
    if (!this.isOwner) return false;
    if (this.roleNum(member.role) === GroupRole.Owner) return false;
    return member.user.id !== this.session.session()?.user?.id;
  }

  addAdmin(member: GroupMember): void {
    this.activeMenuMemberId.set(null);
    this.feedApi.changeMemberRole(this.communityId, member.user.id, GroupRole.Admin).subscribe({
      next: () => {
        this.feedbackRequest.emit({
          text: `${member.user.displayName} is now an Admin.`,
          type: 'success',
        });
        this.memberChanged.emit();
      },
      error: () =>
        this.feedbackRequest.emit({ text: 'Failed to assign admin role.', type: 'error' }),
    });
  }

  removeAdmin(member: GroupMember): void {
    this.activeMenuMemberId.set(null);
    this.feedApi.changeMemberRole(this.communityId, member.user.id, GroupRole.Member).subscribe({
      next: () => {
        this.feedbackRequest.emit({
          text: `${member.user.displayName} is now a Member.`,
          type: 'success',
        });
        this.memberChanged.emit();
      },
      error: () =>
        this.feedbackRequest.emit({ text: 'Failed to remove admin role.', type: 'error' }),
    });
  }

  removeMember(member: GroupMember): void {
    this.activeMenuMemberId.set(null);
    this.confirmDialogRequest.emit({
      title: 'Remove Member',
      message: `Are you sure you want to remove ${member.user.displayName} from this community?`,
      confirmText: 'Remove',
      confirmAction: () => {
        this.feedApi.removeMember(this.communityId, member.user.id).subscribe({
          next: () => {
            this.feedbackRequest.emit({
              text: `${member.user.displayName} removed from community.`,
              type: 'success',
            });
            this.memberChanged.emit();
          },
          error: () =>
            this.feedbackRequest.emit({ text: 'Failed to remove member.', type: 'error' }),
        });
      },
    });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  roleNum(role?: GroupRole | number | string | null): number {
    if (role === null || role === undefined) return 0;
    if (typeof role === 'number') return role;
    if (role === 'Owner' || role === '3') return GroupRole.Owner;
    if (role === 'Admin' || role === '2') return GroupRole.Admin;
    if (role === 'Member' || role === '1') return GroupRole.Member;
    return 0;
  }

  resolveMediaUrl(url?: string | null): string {
    if (!url) return '';
    if (url.startsWith('http://') || url.startsWith('https://')) return url;
    const base = environment.bff.baseUrl.replace(/\/$/, '');
    return `${base}/${url.replace(/^\//, '')}`;
  }

  setRoleFilter(value: string): void {
    this.roleFilter.set(value as RoleFilter);
  }
}

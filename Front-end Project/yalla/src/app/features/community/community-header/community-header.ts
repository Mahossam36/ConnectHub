import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CommunityDetail, GroupRole } from '../../../core/models/feed.models';
import { environment } from '../../../../environments/environment';
import { ActiveTab } from '../community/community.component';

@Component({
  selector: 'app-community-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './community-header.html',
  styleUrl: './community-header.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommunityHeaderComponent {
  @Input() community: CommunityDetail | null = null;
  @Input() activeTab: ActiveTab = 'tab-posts';
  @Input() isJoined = false;
  @Input() canModerate = false;
  @Input() canAnnounce = false;

  @Output() tabChanged = new EventEmitter<ActiveTab>();
  @Output() joinToggled = new EventEmitter<void>();
  @Output() shared = new EventEmitter<void>();

  resolveMediaUrl(url?: string | null): string {
    if (!url) return '';
    if (url.startsWith('http://') || url.startsWith('https://')) return url;
    const base = environment.bff.baseUrl.replace(/\/$/, '');
    return `${base}/${url.replace(/^\//, '')}`;
  }

  getRoleLabel(role?: GroupRole | number | string | null): string {
    if (role === null || role === undefined || role === 0) return 'Guest';
    const num =
      typeof role === 'number'
        ? role
        : role === 'Owner' || role === '3'
          ? GroupRole.Owner
          : role === 'Admin' || role === '2'
            ? GroupRole.Admin
            : role === 'Member' || role === '1'
              ? GroupRole.Member
              : 0;
    if (num === GroupRole.Owner) return 'Owner';
    if (num === GroupRole.Admin) return 'Admin';
    if (num === GroupRole.Member) return 'Member';
    return 'Guest';
  }
}

import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CommunityDetail, FeedPost, GroupMember } from '../../../core/models/feed.models';

@Component({
  selector: 'app-community-analytics',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './community-analytics.html',
  styleUrl: './community-analytics.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommunityAnalyticsComponent {
  @Input() community: CommunityDetail | null = null;
  @Input() posts: FeedPost[] = [];
  @Input() members: GroupMember[] = [];
  @Input() totalCommentsCount = 0;
  @Input() pendingReportsCount = 0;

  /** Static placeholder bars (heights in %) for weekly activity chart */
  readonly weeklyBars = [
    { day: 'Mon', height: 40 },
    { day: 'Tue', height: 60 },
    { day: 'Wed', height: 35 },
    { day: 'Thu', height: 80 },
    { day: 'Fri', height: 95 },
    { day: 'Sat', height: 50 },
    { day: 'Sun', height: 70 },
  ];

  get totalMembers(): number {
    return this.community?.memberCount ?? this.members.length ?? 0;
  }

  get totalPosts(): number {
    return this.posts.length;
  }
}

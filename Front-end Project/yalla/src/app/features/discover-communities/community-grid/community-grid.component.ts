import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { Community, JoinState } from '../../../core/models/feed.models';
import { CommunityCardComponent } from '../community-card/community-card.component';

@Component({
  selector: 'app-community-grid',
  standalone: true,
  imports: [CommonModule, CommunityCardComponent],
  templateUrl: './community-grid.component.html',
  styleUrl: './community-grid.component.scss',
})
export class CommunityGridComponent {
  @Input() heading = '';
  @Input() subtitle = '';
  @Input() communities: Community[] = [];
  @Input() joinStates: Record<string, JoinState> = {};
  @Input() showLoadMore = true;
  @Output() join = new EventEmitter<string>();
  @Output() loadMore = new EventEmitter<void>();
}

import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { Community, JoinState } from '../../../core/models/feed.models';

@Component({
  selector: 'app-community-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './community-card.component.html',
  styleUrl: './community-card.component.scss'
})
export class CommunityCardComponent {
  @Input({ required: true }) community!: Community;
  @Input() joinState: JoinState = 'none';

  @Output() join = new EventEmitter<string>();

  onJoin(): void {
  if (this.community.currentUserRole == null && this.joinState === 'none') {  
  this.join.emit(this.community.id);
}
  }
}

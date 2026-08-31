import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { Community } from '../../../core/models/feed.models';

@Component({
  selector: 'app-featured-community-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './featured-community-card.component.html',
  styleUrl: './featured-community-card.component.scss'
})
export class FeaturedCommunityCardComponent {
  @Input({ required: true }) community!: Community;
  @Output() join = new EventEmitter<string>();

  onJoin(): void {
    this.join.emit(this.community.id);
  }
}

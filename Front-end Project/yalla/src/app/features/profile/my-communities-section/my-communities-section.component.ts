import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { Community } from '../../../core/models/feed.models';
// Was importing CommunityCardComponent from the discover-communities folder (unstyled
// Tailwind duplicate) while the template used a non-existent <app-my-community-card>
// tag — this would not have compiled. Pointing at the already-styled card that
// features/communities/discover-communities.component.ts and my-communities.component.ts
// already use, which also supports the Owner/Admin/Member role badge from the mockup.
import { CommunityCardComponent } from '../../communities/community-shared.component';

@Component({
  selector: 'app-my-communities-section',
  standalone: true,
  imports: [CommonModule, CommunityCardComponent],
  templateUrl: './my-communities-section.component.html',
  styleUrl: './my-communities-section.component.scss',
})
export class MyCommunitiesSectionComponent {
  @Input() heading = 'My Communities';
  @Input() communities: Community[] = [];
  @Output() selectCommunity = new EventEmitter<string>();
}

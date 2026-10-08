import { Component, Input } from '@angular/core';

import { CommonModule } from '@angular/common';

import { Community } from '../../../core/models/feed.models';

import { CommunityCardComponent } from '../../communities/community-shared.component';

@Component({
  selector: 'app-my-communities-section',

  standalone: true,

  imports: [
    CommonModule,
    CommunityCardComponent,
  ],

  templateUrl: './my-communities-section.component.html',

  styleUrl: './my-communities-section.component.scss',
})
export class MyCommunitiesSectionComponent {

  @Input()
  heading = 'My Communities';

  @Input()
  communities: Community[] = [];

}

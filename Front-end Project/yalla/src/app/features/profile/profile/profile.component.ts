import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AuthenticatedUser } from '../../../core/models/auth.models';
import { Community } from '../../../core/models/feed.models';

import { SessionService } from '../../../core/auth/session.service';
import { FeedApiService } from '../../../core/services/feed-api.service';

import { ProfileHeaderComponent } from '../profile-header/profile-header.component';
import { MyCommunitiesSectionComponent } from '../my-communities-section/my-communities-section.component';

@Component({
  selector: 'app-profile',

  standalone: true,

  imports: [
    CommonModule,
    ProfileHeaderComponent,
    MyCommunitiesSectionComponent,
  ],

  templateUrl: './profile.component.html',

  styleUrl: './profile.component.scss',
})
export class ProfileComponent implements OnInit {

  user = signal<AuthenticatedUser | null>(null);

  myCommunities = signal<Community[]>([]);


  constructor(
    private readonly session: SessionService,
    private readonly feedApi: FeedApiService
  ) {}


  ngOnInit(): void {

    this.user.set(
      this.session.user()
    );


    this.feedApi
      .getCommunities()
      .subscribe((communities) => {

        this.myCommunities.set(
          communities.filter(
            community =>
              community.currentUserRole != null
          )
        );

      });

  }


  onEditProfile(): void {

    // TODO:
    // Open edit profile flow.

  }


  onShareProfile(): void {

    // TODO:
    // Share profile link.

  }

}

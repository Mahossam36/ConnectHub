import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { AuthenticatedUser } from '../../../core/models/auth.models';

// AuthenticatedUser currently only has id/email/displayName/avatarUrl - it has no
// username handle or bio field yet. Widening it here rather than inventing a
// separate profile model; move this onto AuthenticatedUser (or a dedicated
// ProfileDetails interface) in auth.models.ts once the API returns these fields.
export type ProfileUser = AuthenticatedUser & {
  username?: string;
  bio?: string;
};

@Component({
  selector: 'app-profile-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-header.component.html',
  styleUrl: './profile-header.component.scss',
})
export class ProfileHeaderComponent {
  @Input({ required: true }) user!: ProfileUser;
  @Output() editProfile = new EventEmitter<void>();
  @Output() share = new EventEmitter<void>();
}

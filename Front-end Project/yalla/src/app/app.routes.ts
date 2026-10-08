import { Routes } from '@angular/router';

import { authGuard } from './core/guards/auth.guard';

import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { NotificationsComponent } from '../app/features/notifications/notification/notifications.component';
import { HomeComponent } from './features/home/feed/feed.component';
import { MainLayoutComponent } from './main-layout/main-layout';
import { MyCommunitiesComponent } from './features/communities/my-communities.component';
import { DiscoverCommunitiesComponent } from './features/communities/discover-communities.component';
import { CommunityComponent } from './features/community/community/community.component';
import { ProfileComponent } from './features/profile/profile/profile.component';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent,
  },

  {
    path: 'register',
    component: RegisterComponent,
  },

  // CommunityComponent renders its own <app-navbar>/<app-side-panel>
  // (it's a self-contained page), so it must NOT be nested under
  // MainLayoutComponent — that would double up the navbar/side panel.
  {
    path: 'community/:id',
    component: CommunityComponent,
    canActivate: [authGuard],
  },

  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],

    children: [
      {
        path: 'home',
        component: HomeComponent,
      },
      {
        path: 'discover',
        component: DiscoverCommunitiesComponent,
      },
      {
        path: 'communities',
        component: MyCommunitiesComponent,
      },
      {
        path: 'profile',
        component: ProfileComponent,
      },
      {
        path: 'notifications',
        component: NotificationsComponent,
      },
    ],
  },

  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login',
  },

  {
    path: '**',
    redirectTo: 'login',
  },
];

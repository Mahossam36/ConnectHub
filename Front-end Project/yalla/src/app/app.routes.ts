import { Routes } from '@angular/router';

import { authGuard } from './core/guards/auth.guard';

import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';

import { HomeComponent } from './features/home/Home/home.component';
import { MainLayoutComponent } from './main-layout/main-layout';

// NEW: pages that were built but never wired into routing.
import { DiscoverCommunitiesComponent } from './features/communities/discover-communities.component';
import { MyCommunitiesComponent } from './features/communities/my-communities.component';
import { NotificationsComponent } from './features/notifications/notification/notifications.component';
import { ProfileComponent } from './features/profile/profile/profile.component';
import { CommunityComponent } from './features/community/community/community.component';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent,
  },

  {
    path: 'register',
    component: RegisterComponent,
  },

  {
    // CommunityComponent renders its own <app-navbar>/<app-side-panel>, so it
    // sits outside MainLayoutComponent to avoid a duplicated nav shell.
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
        path: 'notifications',
        component: NotificationsComponent,
      },
      {
        path: 'profile',
        component: ProfileComponent,
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

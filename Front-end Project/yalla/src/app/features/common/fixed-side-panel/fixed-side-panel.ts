import { Component } from '@angular/core';

import { Router, RouterLink, RouterLinkActive } from '@angular/router';

import { NavigationEnd } from '@angular/router';

import { filter } from 'rxjs';

@Component({
  selector: 'app-fixed-side-panel',

  standalone: true,

  imports: [RouterLink, RouterLinkActive],

  templateUrl: './fixed-side-panel.html',

  styleUrl: './fixed-side-panel.scss',
})
export class FixedSidePanelComponent {
  // =====================================================
  // COMMUNITY STATE
  // =====================================================

  communitiesExpanded = false;

  // =====================================================
  // ROUTE STATE
  // =====================================================

  isCommunitiesPage = false;

  isCommunitiesSectionActive = false;

  // =====================================================
  // CONSTRUCTOR
  // =====================================================

  constructor(private readonly router: Router) {
    this.updateRouteState(this.router.url);

    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event) => {
        const navigationEnd = event as NavigationEnd;

        this.updateRouteState(navigationEnd.urlAfterRedirects);
      });
  }

  // =====================================================
  // ROUTE STATE
  // =====================================================

  private updateRouteState(url: string): void {
    const cleanUrl = url.split('?')[0];

    this.isCommunitiesPage = cleanUrl === '/communities' || cleanUrl.startsWith('/communities/');

    this.isCommunitiesSectionActive = this.isCommunitiesPage;
  }

  // =====================================================
  // COMMUNITIES TOGGLE
  // =====================================================

  toggleCommunities(): void {
    this.communitiesExpanded = !this.communitiesExpanded;
  }
}

import {
  Component,
  EventEmitter,
  Input,
  Output,
} from '@angular/core';

import {
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
} from '@angular/router';

import { filter } from 'rxjs';


@Component({
  selector: 'app-side-panel',

  standalone: true,

  imports: [
    RouterLink,
    RouterLinkActive,
  ],

  templateUrl: './side-panel.html',
  styleUrl: './side-panel.scss',
})
export class SidePanelComponent {

  // =====================================================
  // INPUT
  // =====================================================

  @Input()
  isOpen = false;


  // =====================================================
  // OUTPUTS
  // =====================================================

  @Output()
  closed = new EventEmitter<void>();


  @Output()
  logoutClicked = new EventEmitter<void>();


  // =====================================================
  // COMMUNITIES
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

  constructor(
    private readonly router: Router,
  ) {

    this.updateRouteState(this.router.url);

    this.router.events
      .pipe(
        filter(
          event => event instanceof NavigationEnd
        ),
      )
      .subscribe(
        event => {

          const navigationEnd =
            event as NavigationEnd;

          this.updateRouteState(
            navigationEnd.urlAfterRedirects
          );

        }
      );

  }


  // =====================================================
  // ROUTE STATE
  // =====================================================

  private updateRouteState(url: string): void {

    const cleanUrl = url.split('?')[0];

    this.isCommunitiesPage =
      cleanUrl === '/communities' ||
      cleanUrl.startsWith('/communities/');


    this.isCommunitiesSectionActive =
      this.isCommunitiesPage;

  }


  // =====================================================
  // COMMUNITIES TOGGLE
  // =====================================================

  toggleCommunities(): void {

    this.communitiesExpanded =
      !this.communitiesExpanded;

  }


  // =====================================================
  // CLOSE
  // =====================================================

  closePanel(): void {

    this.closed.emit();

  }

}

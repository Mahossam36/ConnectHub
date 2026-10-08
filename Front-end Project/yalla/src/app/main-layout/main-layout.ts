import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { RouterOutlet } from '@angular/router';

import { NavbarComponent } from '../features/common/navbar/navbar/navbar';
import { SidePanelComponent } from '../features/common/side-panel/side-panel';
import { FixedSidePanelComponent } from '../features/common/fixed-side-panel/fixed-side-panel';
import { CreateCommunityModalComponent } from '../features/communities/community-shared.component';
import { AuthService } from '../core/auth/auth.service';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    RouterOutlet,
    NavbarComponent,
    SidePanelComponent,
    FixedSidePanelComponent,
    CreateCommunityModalComponent,
  ],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss',
})
export class MainLayoutComponent {
  sidePanelOpen = false;

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  logout(): void {
    this.auth.logout().subscribe({
      next: () => this.router.navigateByUrl('/login'),
      error: () => {
        this.auth.clearSession();
        this.router.navigateByUrl('/login');
      },
    });
  }
}

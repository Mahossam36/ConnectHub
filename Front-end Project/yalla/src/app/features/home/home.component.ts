import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { SessionService } from '../../core/auth/session.service';
@Component({ selector: 'app-home', templateUrl: './home.component.html', styleUrl: './home.component.scss' })
export class HomeComponent { constructor(readonly session: SessionService, private readonly auth: AuthService, private readonly router: Router) {} logout(): void { this.auth.logout().subscribe({ next: () => this.router.navigateByUrl('/login'), error: () => { this.auth.clearSession(); this.router.navigateByUrl('/login'); } }); } }

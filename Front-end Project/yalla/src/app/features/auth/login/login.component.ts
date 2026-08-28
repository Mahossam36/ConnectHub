import { Component, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { ApiError } from '../../../core/models/auth.models';

@Component({ selector: 'app-login', imports: [ReactiveFormsModule, RouterLink], templateUrl: './login.component.html', styleUrl: './login.component.scss' })
export class LoginComponent {
  readonly loading = signal(false); readonly error = signal<string | null>(null); readonly passwordVisible = signal(false); readonly form;
  constructor(private readonly fb: FormBuilder, readonly auth: AuthService, private readonly router: Router) {
    this.form = this.fb.nonNullable
      .group({
        email: ['', [Validators.required, Validators.email]],
        password: ['', Validators.required]
      });
  }
  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched(); return;
    } this.loading.set(true);
    this.error.set(null);
    this.auth.login(this.form.getRawValue()).subscribe(
      { next: () => this.router.navigateByUrl('/home'), error: (e: ApiError) => { this.loading.set(false); this.error.set(e.message); } });
  }
  google(): void { try { this.auth.startGoogleAuthentication(); } catch(e) { this.error.set((e as ApiError).message); } }
  togglePassword(): void { this.passwordVisible.update(value => !value); }
}

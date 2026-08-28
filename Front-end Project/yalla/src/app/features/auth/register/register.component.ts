import { Component, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { ApiError } from '../../../core/models/auth.models';

@Component({ selector: 'app-register', imports: [ReactiveFormsModule, RouterLink], templateUrl: './register.component.html', styleUrl: './register.component.scss' })
export class RegisterComponent {
  readonly loading = signal(false); readonly error = signal<string | null>(null); readonly passwordVisible = signal(false); readonly form;
  constructor(private readonly fb: FormBuilder, readonly auth: AuthService, private readonly router: Router) { this.form = this.fb.nonNullable.group({ firstName: ['', [Validators.required, Validators.maxLength(100)]], lastName: ['', [Validators.required, Validators.maxLength(100)]], email: ['', [Validators.required, Validators.email]], password: ['', [Validators.required, Validators.minLength(6)]] }); }
  submit(): void { if(this.form.invalid){this.form.markAllAsTouched();return;} this.loading.set(true);this.error.set(null);this.auth.register(this.form.getRawValue()).subscribe({next:()=>this.router.navigateByUrl('/home'),error:(e:ApiError)=>{this.loading.set(false);this.error.set(e.message);}}); }
  google(): void { try { this.auth.startGoogleAuthentication(); } catch(e) { this.error.set((e as ApiError).message); } }
  togglePassword(): void { this.passwordVisible.update(value => !value); }
}

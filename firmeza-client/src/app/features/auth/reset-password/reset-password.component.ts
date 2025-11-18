import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ResetPasswordRequest } from '../../../core/models/auth.models';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss'
})
export class ResetPasswordComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
    private readonly notifications: NotificationService
  ) {}

  loading = false;
  errorMessage = '';
  token = '';
  userId = '';

  form = this.fb.group(
    {
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    },
    { validators: this.passwordsMatch }
  );

  ngOnInit(): void {
    this.route.queryParamMap.subscribe((params) => {
      this.token = params.get('token') ?? '';
      this.userId = params.get('userId') ?? '';
      const email = params.get('email');
      if (email) {
        this.form.controls.email.setValue(email);
      }
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (!this.token || !this.userId) {
      this.errorMessage = 'El enlace de restablecimiento no es válido o expiró. Solicítalo nuevamente.';
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    const payload: ResetPasswordRequest = {
      userId: this.userId,
      token: this.token,
      email: this.form.value.email?.trim().toLowerCase() ?? '',
      password: this.form.value.password ?? ''
    };

    this.auth.resetPassword(payload).subscribe({
      next: () => {
        this.loading = false;
        this.notifications.show({ type: 'success', text: 'Contraseña restablecida. Ingresa con tu nueva contraseña.' });
        void this.router.navigate(['/login']);
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage = typeof error?.error === 'string' ? error.error : 'No se pudo restablecer la contraseña.';
      }
    });
  }

  private passwordsMatch(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password')?.value;
    const confirm = control.get('confirmPassword')?.value;
    if (!password || !confirm) {
      return null;
    }
    return password === confirm ? null : { passwordMismatch: true };
  }

  get passwordMismatch(): boolean {
    return this.form.hasError('passwordMismatch') && this.form.get('confirmPassword')?.touched === true;
  }
}

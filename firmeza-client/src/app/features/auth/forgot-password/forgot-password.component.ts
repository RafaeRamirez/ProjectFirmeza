import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ForgotPasswordRequest, ForgotPasswordResponse } from '../../../core/models/auth.models';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);

  constructor(
    private readonly auth: AuthService,
    private readonly notifications: NotificationService
  ) {}

  loading = false;
  errorMessage = '';
  response: ForgotPasswordResponse | null = null;

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    const payload: ForgotPasswordRequest = {
      email: this.form.value.email?.trim().toLowerCase() ?? ''
    };

    this.auth.forgotPassword(payload).subscribe({
      next: (res) => {
        this.loading = false;
        this.response = res;
        this.notifications.show({
          type: 'info',
          text: res.message ?? 'Si el correo existe, te enviaremos instrucciones.'
        });
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage = typeof error?.error === 'string' ? error.error : 'No se pudo procesar la solicitud.';
      }
    });
  }
}

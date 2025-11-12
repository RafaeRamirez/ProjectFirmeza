import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { LoginRequest } from '../../../core/models/auth.models';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
    private readonly notifications: NotificationService
  ) {}

  errorMessage = '';
  loading = false;

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    const payload: LoginRequest = {
      email: this.form.value.email?.trim().toLowerCase() ?? '',
      password: this.form.value.password ?? ''
    };

    this.auth.login(payload).subscribe({
      next: () => {
        this.loading = false;
        this.notifications.show({ type: 'success', text: 'Sesión iniciada correctamente.' });
        void this.router.navigate(['/catalogo']);
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage = this.extractError(error);
      }
    });
  }

  private extractError(error: unknown): string {
    if (!error) {
      return 'No se pudo iniciar sesión. Inténtalo más tarde.';
    }
    if (typeof error === 'string') {
      return error;
    }
    const httpError = error as { error?: unknown; message?: string };
    if (httpError.error) {
      if (typeof httpError.error === 'string') {
        return httpError.error;
      }
      if (typeof httpError.error === 'object' && 'title' in (httpError.error as Record<string, unknown>)) {
        return String((httpError.error as Record<string, unknown>)['title']);
      }
    }
    return httpError.message ?? 'No se pudo iniciar sesión.';
  }
}

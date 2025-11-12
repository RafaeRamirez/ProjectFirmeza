import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { RegisterRequest } from '../../../core/models/auth.models';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
    private readonly notifications: NotificationService
  ) {}

  loading = false;
  errorMessage = '';

  form = this.fb.group({
    fullName: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading = true;
    this.errorMessage = '';
    const payload: RegisterRequest = {
      fullName: this.form.value.fullName?.trim() ?? '',
      email: this.form.value.email?.trim().toLowerCase() ?? '',
      phone: this.form.value.phone?.toString().trim() || null,
      password: this.form.value.password ?? ''
    };

    this.auth.register(payload).subscribe({
      next: () => {
        this.loading = false;
        this.notifications.show({
          type: 'success',
          text: 'Tu cuenta fue creada. Ya puedes comprar en el catálogo.'
        });
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
      return 'No se pudo completar el registro.';
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
    return httpError.message ?? 'No se pudo completar el registro.';
  }
}

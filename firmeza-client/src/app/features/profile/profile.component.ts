import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProfileService } from '../../core/services/profile.service';
import { NotificationService } from '../../core/services/notification.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent implements OnInit {
  private readonly fb = inject(FormBuilder);

  constructor(
    private readonly profile: ProfileService,
    private readonly notifications: NotificationService,
    private readonly auth: AuthService
  ) {}

  loading = false;
  saving = false;
  deleting = false;
  errorMessage = '';

  form = this.fb.group({
    fullName: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    phone: ['']
  });

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.loading = true;
    this.errorMessage = '';
    this.profile.getProfile().subscribe({
      next: (data) => {
        this.form.patchValue({
          fullName: data.fullName,
          email: data.email,
          phone: data.phone ?? ''
        });
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'No se pudo cargar tu información. Intenta nuevamente.';
      }
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving = true;
    this.errorMessage = '';
    this.profile
      .updateProfile({
        fullName: this.form.value.fullName?.trim() ?? '',
        email: this.form.value.email?.trim().toLowerCase() ?? '',
        phone: this.form.value.phone?.toString().trim() || null
      })
      .subscribe({
        next: (data) => {
          this.saving = false;
          this.notifications.show({ type: 'success', text: 'Actualizamos tus datos exitosamente.' });
          this.auth.updateProfileSession({ fullName: data.fullName, email: data.email });
          this.form.patchValue({
            fullName: data.fullName,
            email: data.email,
            phone: data.phone ?? ''
          });
        },
        error: (error) => {
          this.saving = false;
          this.errorMessage = this.extractError(error, 'No se pudo actualizar la información.');
        }
      });
  }

  deleteProfile(): void {
    if (!confirm('¿Seguro que deseas eliminar tu cuenta? Esta acción es irreversible.')) {
      return;
    }
    this.deleting = true;
    this.profile.deleteProfile().subscribe({
      next: () => {
        this.deleting = false;
        this.notifications.show({ type: 'success', text: 'Tu cuenta fue eliminada.' });
        this.auth.logout();
      },
      error: (error) => {
        this.deleting = false;
        this.errorMessage = this.extractError(error, 'No se pudo eliminar la cuenta.');
      }
    });
  }

  private extractError(error: unknown, fallback: string): string {
    if (!error) {
      return fallback;
    }
    if (typeof error === 'string') {
      return error;
    }
    const httpError = error as { error?: unknown; message?: string };
    if (typeof httpError.error === 'string' && httpError.error.trim().length > 0) {
      return httpError.error;
    }
    return httpError.message ?? fallback;
  }
}

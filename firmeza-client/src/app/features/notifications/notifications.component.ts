import { Component } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { NotificationsApiService } from '../../core/services/notifications-api.service';
import { ProductRequestNotification, RequestStatus } from '../../core/models/notification.model';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.scss'
})
export class NotificationsComponent {
  notifications: ProductRequestNotification[] = [];
  loading = false;
  error = '';

  constructor(private readonly notificationsApi: NotificationsApiService) {
    this.loadNotifications();
  }

  refresh(): void {
    this.loadNotifications();
  }

  trackById(_: number, item: ProductRequestNotification): string {
    return item.id;
  }

  badgeClass(status: RequestStatus): string {
    switch (status) {
      case 'Approved':
        return 'bg-success';
      case 'Rejected':
        return 'bg-danger';
      default:
        return 'bg-secondary';
    }
  }

  statusLabel(status: RequestStatus): string {
    switch (status) {
      case 'Approved':
        return 'Compra aprobada';
      case 'Rejected':
        return 'Compra rechazada';
      default:
        return 'Pendiente';
    }
  }

  private loadNotifications(): void {
    this.loading = true;
    this.error = '';
    this.notificationsApi.list().subscribe({
      next: (items) => {
        this.notifications = items;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.error =
          typeof err?.error === 'string'
            ? err.error
            : err?.message ?? 'No se pudieron cargar las notificaciones.';
      }
    });
  }
}

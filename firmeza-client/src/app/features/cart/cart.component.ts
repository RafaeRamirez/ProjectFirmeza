import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CartService } from '../../core/services/cart.service';
import { SalesService } from '../../core/services/sales.service';
import { CartItem } from '../../core/models/cart.model';
import { environment } from '../../../environments/environment';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.scss'
})
export class CartComponent {
  readonly items$;
  readonly totals$;
  readonly taxRate = environment.taxRate;

  constructor(
    private readonly cart: CartService,
    private readonly sales: SalesService,
    private readonly notifications: NotificationService
  ) {
    this.items$ = this.cart.items$;
    this.totals$ = this.cart.totals$;
  }

  errorMessage = '';
  successMessage = '';
  checkoutLoading = false;

  updateQuantity(item: CartItem, value: number | string): void {
    const numericValue = typeof value === 'string' ? parseInt(value, 10) : value;
    const sanitized = Number.isNaN(numericValue) ? 1 : numericValue;
    this.cart.updateQuantity(item.product.id, sanitized);
  }

  remove(item: CartItem): void {
    this.cart.remove(item.product.id);
  }

  checkout(): void {
    const snapshot = this.cart.snapshot();
    if (snapshot.length === 0) {
      this.errorMessage = 'Agrega al menos un producto al carrito.';
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';
    this.checkoutLoading = true;
    this.sales.requestApproval(snapshot).subscribe({
      next: (result) => {
        const created = result.requests ?? [];
        const errors = result.errors ?? [];
        this.checkoutLoading = false;
        if (created.length > 0) {
          created.forEach((req) => this.cart.remove(req.productId));
          this.successMessage = `Solicitud enviada. ${created.length} producto(s) quedan pendientes de aprobación del administrador.`;
          this.notifications.show({
            type: 'info',
            text: 'Tu compra fue enviada al administrador. Recibirás una notificación cuando se apruebe.'
          });
        } else {
          this.successMessage = '';
        }

        if (errors.length > 0) {
          const missing = errors.map((e) => e.message).join(' ');
          this.errorMessage = `Algunos productos no pudieron solicitarse: ${missing}`;
        } else {
          this.errorMessage = '';
        }
      },
      error: (error) => {
        this.checkoutLoading = false;
        this.errorMessage =
          typeof error?.error === 'string' ? error.error : error?.message ?? 'No se pudo registrar la solicitud.';
      }
    });
  }
}

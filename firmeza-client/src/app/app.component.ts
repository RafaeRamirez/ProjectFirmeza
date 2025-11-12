import { Component } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { CommonModule, AsyncPipe } from '@angular/common';
import { AuthService } from './core/services/auth.service';
import { CartService } from './core/services/cart.service';
import { NotificationService } from './core/services/notification.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, AsyncPipe],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  readonly currentYear = new Date().getFullYear();
  readonly authState$;
  readonly cartItems$;
  readonly message$;

  constructor(
    private readonly auth: AuthService,
    private readonly cart: CartService,
    private readonly router: Router,
    private readonly notifications: NotificationService
  ) {
    this.authState$ = this.auth.authState$;
    this.cartItems$ = this.cart.totalItems$;
    this.message$ = this.notifications.message$;
  }

  logout(): void {
    this.auth.logout();
  }

  navigateToCart(): void {
    void this.router.navigate(['/carrito']);
  }

  closeMessage(): void {
    this.notifications.clear();
  }
}

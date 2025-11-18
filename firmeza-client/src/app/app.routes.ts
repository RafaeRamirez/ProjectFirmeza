import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { CatalogComponent } from './features/catalog/catalog.component';
import { CartComponent } from './features/cart/cart.component';
import { NotificationsComponent } from './features/notifications/notifications.component';
import { ForgotPasswordComponent } from './features/auth/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './features/auth/reset-password/reset-password.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'registro', component: RegisterComponent },
  { path: 'recuperar', component: ForgotPasswordComponent },
  { path: 'restablecer', component: ResetPasswordComponent },
  { path: 'catalogo', component: CatalogComponent, canActivate: [authGuard] },
  { path: 'carrito', component: CartComponent, canActivate: [authGuard] },
  { path: 'notificaciones', component: NotificationsComponent, canActivate: [authGuard] },
  { path: '', pathMatch: 'full', redirectTo: 'catalogo' },
  { path: '**', redirectTo: 'catalogo' }
];

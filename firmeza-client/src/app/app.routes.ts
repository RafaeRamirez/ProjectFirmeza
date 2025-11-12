import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { CatalogComponent } from './features/catalog/catalog.component';
import { CartComponent } from './features/cart/cart.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'registro', component: RegisterComponent },
  { path: 'catalogo', component: CatalogComponent, canActivate: [authGuard] },
  { path: 'carrito', component: CartComponent, canActivate: [authGuard] },
  { path: '', pathMatch: 'full', redirectTo: 'catalogo' },
  { path: '**', redirectTo: 'catalogo' }
];

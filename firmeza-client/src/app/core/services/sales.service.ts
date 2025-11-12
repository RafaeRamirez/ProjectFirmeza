import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SaleRequest, SaleResponse } from '../models/sale.model';
import { CartItem } from '../models/cart.model';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class SalesService {
  constructor(private readonly http: HttpClient, private readonly auth: AuthService) {}

  createSale(items: CartItem[]): Observable<SaleResponse> {
    const customerId = this.auth.customerId;
    const payload: SaleRequest = {
      ...(customerId ? { customerId } : {}),
      items: items.map((item) => ({
        productId: item.product.id,
        quantity: item.quantity
      }))
    };

    return this.http.post<SaleResponse>(`${environment.apiBaseUrl}/sales`, payload);
  }
}

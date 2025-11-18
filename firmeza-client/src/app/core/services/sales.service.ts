import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CartItem } from '../models/cart.model';
import { ProductRequestBatchResponse } from '../models/notification.model';

@Injectable({ providedIn: 'root' })
export class SalesService {
  constructor(private readonly http: HttpClient) {}

  requestApproval(items: CartItem[]): Observable<ProductRequestBatchResponse> {
    const payload = {
      items: items.map((item) => ({
        productId: item.product.id,
        quantity: item.quantity
      }))
    };

    return this.http.post<ProductRequestBatchResponse>(`${environment.apiBaseUrl}/notifications`, payload);
  }
}

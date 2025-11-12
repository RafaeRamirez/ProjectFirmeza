import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResponse, Product, ProductQuery } from '../models/product.model';

@Injectable({ providedIn: 'root' })
export class ProductService {
  constructor(private readonly http: HttpClient) {}

  search(params: ProductQuery): Observable<PagedResponse<Product>> {
    let httpParams = new HttpParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value === undefined || value === null || value === '') {
        return;
      }
      httpParams = httpParams.set(key, String(value));
    });

    return this.http.get<PagedResponse<Product>>(`${environment.apiBaseUrl}/products`, {
      params: httpParams
    });
  }
}

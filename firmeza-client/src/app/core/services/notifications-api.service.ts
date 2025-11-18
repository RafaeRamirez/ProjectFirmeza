import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ProductRequestNotification } from '../models/notification.model';

@Injectable({ providedIn: 'root' })
export class NotificationsApiService {
  constructor(private readonly http: HttpClient) {}

  list(): Observable<ProductRequestNotification[]> {
    return this.http.get<ProductRequestNotification[]>(`${environment.apiBaseUrl}/notifications`);
  }
}

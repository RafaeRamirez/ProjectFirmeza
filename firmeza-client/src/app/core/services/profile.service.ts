import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ProfileDto, ProfileUpdateRequest } from '../models/profile.models';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  constructor(private readonly http: HttpClient) {}

  getProfile(): Observable<ProfileDto> {
    return this.http.get<ProfileDto>(`${environment.apiBaseUrl}/auth/profile`);
  }

  updateProfile(payload: ProfileUpdateRequest): Observable<ProfileDto> {
    return this.http.put<ProfileDto>(`${environment.apiBaseUrl}/auth/profile`, payload);
  }

  deleteProfile(): Observable<void> {
    return this.http.delete<void>(`${environment.apiBaseUrl}/auth/profile`);
  }
}

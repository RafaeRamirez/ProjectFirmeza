import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthResponseDto,
  AuthSession,
  ForgotPasswordRequest,
  ForgotPasswordResponse,
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest
} from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly storageKey = 'firmeza.client.auth';
  private readonly authStateSubject = new BehaviorSubject<AuthSession | null>(null);
  readonly authState$ = this.authStateSubject.asObservable();

  constructor(private readonly http: HttpClient, private readonly router: Router) {
    this.restoreSession();
  }

  login(request: LoginRequest): Observable<AuthResponseDto> {
    return this.http
      .post<AuthResponseDto>(`${environment.apiBaseUrl}/auth/login`, request)
      .pipe(tap((response) => this.persistSession(response)));
  }

  register(request: RegisterRequest): Observable<AuthResponseDto> {
    return this.http
      .post<AuthResponseDto>(`${environment.apiBaseUrl}/auth/register`, request)
      .pipe(tap((response) => this.persistSession(response)));
  }

  forgotPassword(request: ForgotPasswordRequest): Observable<ForgotPasswordResponse> {
    return this.http.post<ForgotPasswordResponse>(`${environment.apiBaseUrl}/auth/forgot-password`, request);
  }

  resetPassword(request: ResetPasswordRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/auth/reset-password`, request);
  }

  logout(): void {
    this.clearSession();
    void this.router.navigate(['/login']);
  }

  get token(): string | null {
    return this.authStateSubject.value?.token ?? null;
  }

  get customerId(): string | null {
    return this.authStateSubject.value?.customerId ?? null;
  }

  get customerName(): string | null {
    return this.authStateSubject.value?.customerName ?? this.authStateSubject.value?.email ?? null;
  }

  isAuthenticated(): boolean {
    const session = this.authStateSubject.value;
    if (!session) {
      return false;
    }
    if (this.isExpired(session.expiresAt)) {
      this.clearSession();
      return false;
    }
    return true;
  }

  markSessionExpired(): void {
    this.clearSession();
    void this.router.navigate(['/login'], { queryParams: { sessionExpired: '1' } });
  }

  private persistSession(response: AuthResponseDto): void {
    const session: AuthSession = {
      token: response.accessToken,
      expiresAt: response.expiresAt,
      email: response.email,
      customerId: response.customerId ?? null,
      customerName: response.customerName ?? null
    };
    localStorage.setItem(this.storageKey, JSON.stringify(session));
    this.authStateSubject.next(session);
  }

  private restoreSession(): void {
    const raw = localStorage.getItem(this.storageKey);
    if (!raw) {
      return;
    }

    try {
      const parsed = JSON.parse(raw) as AuthSession;
      if (this.isExpired(parsed.expiresAt)) {
        this.clearSession();
        return;
      }
      this.authStateSubject.next(parsed);
    } catch {
      this.clearSession();
    }
  }

  private isExpired(expiresAt: string): boolean {
    return new Date(expiresAt).getTime() <= Date.now();
  }

  private clearSession(): void {
    localStorage.removeItem(this.storageKey);
    this.authStateSubject.next(null);
  }
}

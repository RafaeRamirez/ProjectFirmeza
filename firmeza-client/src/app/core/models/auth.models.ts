export interface AuthResponseDto {
  accessToken: string;
  expiresAt: string;
  email: string;
  roles: string[];
  customerId?: string | null;
  customerName?: string | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  phone?: string | null;
  password: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ForgotPasswordResponse {
  emailSent: boolean;
  showResetLink: boolean;
  message?: string | null;
  resetLink?: string | null;
  userId?: string | null;
  token?: string | null;
}

export interface ResetPasswordRequest {
  userId: string;
  email: string;
  token: string;
  password: string;
}

export interface AuthSession {
  token: string;
  expiresAt: string;
  email: string;
  customerId?: string | null;
  customerName?: string | null;
}

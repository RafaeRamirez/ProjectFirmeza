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

export interface AuthSession {
  token: string;
  expiresAt: string;
  email: string;
  customerId?: string | null;
  customerName?: string | null;
}

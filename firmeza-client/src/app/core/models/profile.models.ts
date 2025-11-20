export interface ProfileDto {
  fullName: string;
  email: string;
  phone: string | null;
}

export interface ProfileUpdateRequest {
  fullName: string;
  email: string;
  phone: string | null;
}

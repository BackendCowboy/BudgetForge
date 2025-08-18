export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiry: Date;
  refreshTokenExpiry: Date;
  user: UserInfo;
}

export interface UserInfo {
  id: number;  // int in C#, not string
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  roles: string[];
  lastLoginAt?: Date;
}

export interface RefreshTokenRequest {
  accessToken: string;
  refreshToken: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

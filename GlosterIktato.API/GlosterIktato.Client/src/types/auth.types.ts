export interface LoginRequestDto {
  email: string;
  password: string;
}

export interface CompanyDto {
  id: number;
  name: string;
  taxNumber: string;
  isActive?: boolean;
}

export interface UserDto {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  companies: CompanyDto[];
  roles: string[];
}

export interface LoginResponseDto {
  token: string;
  refreshToken: string;
  expiresAt: string;
  user: UserDto;
}


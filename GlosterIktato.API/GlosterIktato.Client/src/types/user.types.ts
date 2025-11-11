// Auto-generated from C# models: User, Role, Company, Supplier
// Rules:
// - Enums as numbers
// - PascalCase property names
// - DateTime -> string
// - int? -> number | undefined

import type { Document } from './document.types';

export interface Company {
	Id: number;
	Name: string;
	TaxNumber: string;
	Address?: string | undefined;
	IsActive: boolean;
	CreatedAt: string;

	Users: User[];
	Documents: Document[];
}

export interface Supplier {
	Id: number;
	Name: string;
	TaxNumber?: string | undefined;
	Address?: string | undefined;
	ContactPerson?: string | undefined;
	Email?: string | undefined;
	Phone?: string | undefined;
	IsActive: boolean;
	CreatedAt: string;

	Documents: Document[];
}

export interface Role {
	Id: number;
	Name: string;
	Description: string;

	UserRoles: UserRole[];
}

export interface UserRole {
	UserId: number;
	User: User;
	RoleId: number;
	Role: Role;
}

export interface User {
	Id: number;
	Email: string;
	PasswordHash: string;
	FirstName: string;
	LastName: string;
	IsActive: boolean;
	CreatedAt: string;
	LastLoginAt?: string | undefined;

	CompanyId?: number | undefined;
	Company?: Company | undefined;
	UserRoles: UserRole[];
}



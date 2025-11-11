// Auto-generated from C# models: Document, DocumentType
// Rules:
// - Enums as numbers
// - PascalCase property names
// - DateTime -> string
// - int? -> number | undefined

import type { Company, Supplier, User } from './user.types';

export interface DocumentType {
	Id: number;
	Name: string;
	Code: string;
	IsActive: boolean;
	Documents: Document[];
}

export interface Document {
	Id: number;
	RegistrationNumber: string;
	FileName: string;
	OriginalFileName: string;
	FilePath: string;
	Status: string;
	CreatedAt: string;
	ModifiedAt?: string | undefined;

	CompanyId: number;
	Company: Company;

	DocumentTypeId: number;
	DocumentType: DocumentType;

	SupplierId?: number | undefined;
	Supplier?: Supplier | undefined;

	CreatedByUserId: number;
	CreatedBy: User;

	AssignedToUserId?: number | undefined;
	AssignedTo?: User | undefined;
}



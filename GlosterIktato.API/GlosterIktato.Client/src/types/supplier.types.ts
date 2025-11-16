/**
 * Supplier types and interfaces
 */

/**
 * Supplier DTO
 */
export interface SupplierDto {
	id: number;
	name: string;
	taxNumber?: string | null;
	address?: string | null;
	contactPerson?: string | null;
	email?: string | null;
	phone?: string | null;
}

/**
 * Supplier list item for autocomplete
 */
export interface SupplierListItemDto {
	id: number;
	name: string;
	taxNumber?: string | null;
}


/**
 * User types and interfaces
 */

/**
 * Basic user DTO
 */
export interface UserDto {
	id: number;
	email: string;
	firstName: string;
	lastName: string;
	fullName?: string;
	role?: string;
	companyId?: number;
	companyName?: string;
}

/**
 * User with company information
 */
export interface UserWithCompanyDto extends UserDto {
	companyId: number;
	companyName: string;
}

/**
 * User list item for dropdowns/autocomplete
 */
export interface UserListItemDto {
	id: number;
	name: string;
	email: string;
}


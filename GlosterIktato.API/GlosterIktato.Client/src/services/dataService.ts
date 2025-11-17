/**
 * Data service for loading lookup data (companies, document types, users)
 */
import api from './api';

export interface Company {
	id: number;
	name: string;
	taxNumber: string;
}

export interface DocumentType {
	id: number;
	name: string;
	code: string;
}

export interface User {
	id: number;
	firstName: string;
	lastName: string;
	email: string;
}

/**
 * Get companies for the current user
 * @returns List of companies the user has access to
 */
export async function getMyCompanies(): Promise<Company[]> {
	const response = await api.get<Company[]>('/companies/my-companies');
	return response.data;
}

/**
 * Get all active document types
 * @returns List of active document types
 */
export async function getDocumentTypes(): Promise<DocumentType[]> {
	const response = await api.get<DocumentType[]>('/documents/types');
	return response.data;
}

/**
 * Get users by company ID
 * @param companyId - Company ID
 * @returns List of active users for the company
 */
export async function getUsersByCompany(companyId: number): Promise<User[]> {
	const response = await api.get<User[]>(`/users/company/${companyId}`);
	return response.data;
}


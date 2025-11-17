/**
 * Document search service
 */
import api from './api';
import type { DocumentResponseDto, PaginatedResult } from '@/types/document.types';

export interface DocumentSearchParams {
	companyId?: number | null;
	documentTypeId?: number | null;
	status?: string[];
	createdFrom?: string | null;
	createdTo?: string | null;
	issueDateFrom?: string | null;
	issueDateTo?: string | null;
	paymentDeadlineFrom?: string | null;
	paymentDeadlineTo?: string | null;
	grossAmountMin?: number | null;
	grossAmountMax?: number | null;
	currency?: string | null;
	archiveNumber?: string;
	invoiceNumber?: string;
	supplierName?: string;
	comment?: string;
	assignedToUserId?: number | null;
	page?: number;
	pageSize?: number;
}

/**
 * Search documents with filters
 * @param params - Search parameters
 * @returns Paginated search results
 */
export async function searchDocuments(params: DocumentSearchParams): Promise<PaginatedResult<DocumentResponseDto>> {
	// Build query params, excluding null, empty strings, and empty arrays
	const queryParams: Record<string, any> = {
		page: params.page ?? 1,
		pageSize: params.pageSize ?? 20
	};

	Object.entries(params).forEach(([key, value]) => {
		if (value !== null && value !== '' && !(Array.isArray(value) && value.length === 0)) {
			queryParams[key] = value;
		}
	});

	const response = await api.get<PaginatedResult<DocumentResponseDto>>('/documents/search', {
		params: queryParams
	});

	return response.data;
}


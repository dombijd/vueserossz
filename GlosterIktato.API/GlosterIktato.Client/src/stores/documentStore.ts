import { defineStore } from 'pinia';
import { ref } from 'vue';
import api from '../services/api';
import type { Document, DocumentResponseDto, PaginatedResult } from '../types/document.types';

export interface FetchDocumentsParams {
	query?: string;
	companyId?: number;
	documentTypeId?: number;
	page?: number;
	pageSize?: number;
}

export type DocumentCreatePayload = Omit<Document, 'Id' | 'Company' | 'DocumentType' | 'Supplier' | 'CreatedBy' | 'AssignedTo' | 'Documents'>;
export type DocumentUpdatePayload = Partial<Omit<Document, 'Company' | 'DocumentType' | 'Supplier' | 'CreatedBy' | 'AssignedTo' | 'Documents'>> & { Id: number };

export const useDocumentStore = defineStore('documentStore', () => {
	const documents = ref<Document[]>([]);
	const isLoading = ref<boolean>(false);
	const error = ref<string | null>(null);

	async function fetchDocuments(params: FetchDocumentsParams = {}): Promise<void> {
		isLoading.value = true;
		error.value = null;
		try {
			const response = await api.get<Document[]>('/documents', { params });
			documents.value = response.data;
		} catch (err: unknown) {
			error.value = (err as Error)?.message ?? 'Failed to fetch documents';
			throw err;
		} finally {
			isLoading.value = false;
		}
	}

	async function createDocument(payload: DocumentCreatePayload): Promise<Document> {
		isLoading.value = true;
		error.value = null;
		try {
			const response = await api.post<Document>('/documents', payload);
			const created = response.data;
			// prepend new document
			documents.value = [created, ...documents.value];
			return created;
		} catch (err: unknown) {
			error.value = (err as Error)?.message ?? 'Failed to create document';
			throw err;
		} finally {
			isLoading.value = false;
		}
	}

	async function updateDocument(id: number, payload: DocumentUpdatePayload): Promise<Document> {
		isLoading.value = true;
		error.value = null;
		try {
			const response = await api.put<Document>(`/documents/${id}`, payload);
			const updated = response.data;
			documents.value = documents.value.map(d => d.Id === id ? updated : d);
			return updated;
		} catch (err: unknown) {
			error.value = (err as Error)?.message ?? 'Failed to update document';
			throw err;
		} finally {
			isLoading.value = false;
		}
	}

	async function deleteDocument(id: number): Promise<void> {
		isLoading.value = true;
		error.value = null;
		try {
			await api.delete(`/documents/${id}`);
			documents.value = documents.value.filter(d => d.Id !== id);
		} catch (err: unknown) {
			error.value = (err as Error)?.message ?? 'Failed to delete document';
			throw err;
		} finally {
			isLoading.value = false;
		}
	}

	async function fetchMyTasks(page: number = 1, pageSize: number = 20, status?: string): Promise<PaginatedResult<DocumentResponseDto>> {
		isLoading.value = true;
		error.value = null;
		try {
			const params: { page: number; pageSize: number; status?: string } = { page, pageSize };
			if (status) {
				params.status = status;
			}
			const response = await api.get<PaginatedResult<DocumentResponseDto>>('/documents/my-tasks', {
				params
			});
			return response.data;
		} catch (err: unknown) {
			error.value = (err as Error)?.message ?? 'Failed to fetch my tasks';
			throw err;
		} finally {
			isLoading.value = false;
		}
	}

	return {
		documents,
		isLoading,
		error,
		fetchDocuments,
		createDocument,
		updateDocument,
		deleteDocument,
		fetchMyTasks
	};
});



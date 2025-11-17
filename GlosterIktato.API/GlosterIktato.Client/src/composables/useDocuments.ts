import { storeToRefs } from 'pinia';
import { computed } from 'vue';
import { useDocumentStore, type FetchDocumentsParams, type DocumentCreatePayload, type DocumentUpdatePayload } from '../stores/documentStore';
import type { Document } from '../types/document.types';
import { useToast } from './useToast';

export function useDocuments() {
	const store = useDocumentStore();
	const { documents, isLoading, error } = storeToRefs(store);
	const { success, error: toastError } = useToast();

	const hasError = computed(() => Boolean(error.value));

	async function fetch(params?: FetchDocumentsParams) {
		try {
			await store.fetchDocuments(params);
		} catch (e) {
			toastError('Failed to load documents');
			throw e;
		}
	}

	async function create(payload: DocumentCreatePayload): Promise<Document> {
		try {
			const created = await store.createDocument(payload);
			success('Document created');
			return created;
		} catch (e) {
			toastError('Failed to create document');
			throw e;
		}
	}

	async function update(id: number, payload: DocumentUpdatePayload): Promise<Document> {
		try {
			const updated = await store.updateDocument(id, payload);
			success('Document updated');
			return updated;
		} catch (e) {
			toastError('Failed to update document');
			throw e;
		}
	}

	async function remove(id: number): Promise<void> {
		try {
			await store.deleteDocument(id);
			success('Document deleted');
		} catch (e) {
			toastError('Failed to delete document');
			throw e;
		}
	}

	return {
		// state
		documents,
		isLoading,
		error,
		hasError,
		// actions
		fetch,
		create,
		update,
		remove
	};
}



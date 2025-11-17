import { ref } from 'vue';
import api from '@/services/api';
import { useToast } from './useToast';
import { getErrorMessage } from '@/services/api';

export interface WorkflowAdvanceDto {
	assignToUserId?: number;
	comment?: string;
}

export interface WorkflowRejectDto {
	reason: string;
}

export interface WorkflowDelegateDto {
	targetUserId: number;
	comment?: string;
}

export interface WorkflowActionResult {
	success: boolean;
	message: string;
	newStatus?: string;
	newStatusDisplay?: string;
	assignedToUserId?: number;
	assignedToUserName?: string;
}

/**
 * Composable for managing document workflow operations
 */
export function useWorkflow() {
	const { success, error: toastError } = useToast();
	const isLoading = ref(false);

	/**
	 * Advance document to next status
	 */
	async function advance(
		documentId: number,
		dto: WorkflowAdvanceDto = {}
	): Promise<WorkflowActionResult> {
		isLoading.value = true;
		try {
			const response = await api.post<WorkflowActionResult>(
				`/documents/${documentId}/workflow/advance`,
				dto
			);
			
			if (response.data.success) {
				success('Dokumentum továbbküldve');
				return response.data;
			} else {
				const errorMsg = response.data.message || 'Hiba történt a továbbküldés során';
				toastError(errorMsg);
				return response.data;
			}
		} catch (err: any) {
			console.error('Error advancing document:', err);
			const errorMsg = getErrorMessage(err) || 'Hiba történt a továbbküldés során';
			toastError(errorMsg);
			throw err;
		} finally {
			isLoading.value = false;
		}
	}

	/**
	 * Step back document to previous status
	 */
	async function stepBack(
		documentId: number,
		dto: WorkflowAdvanceDto = {}
	): Promise<WorkflowActionResult> {
		isLoading.value = true;
		try {
			const response = await api.post<WorkflowActionResult>(
				`/documents/${documentId}/workflow/stepback`,
				dto
			);
			
			if (response.data.success) {
				success('Dokumentum visszaléptetve');
				return response.data;
			} else {
				const errorMsg = response.data.message || 'Hiba történt a visszaléptetés során';
				toastError(errorMsg);
				return response.data;
			}
		} catch (err: any) {
			console.error('Error stepping back document:', err);
			const errorMsg = getErrorMessage(err) || 'Hiba történt a visszaléptetés során';
			toastError(errorMsg);
			throw err;
		} finally {
			isLoading.value = false;
		}
	}

	/**
	 * Reject document
	 */
	async function reject(
		documentId: number,
		dto: WorkflowRejectDto
	): Promise<WorkflowActionResult> {
		isLoading.value = true;
		try {
			const response = await api.post<WorkflowActionResult>(
				`/documents/${documentId}/workflow/reject`,
				dto
			);
			
			if (response.data.success) {
				success('Dokumentum elutasítva');
				return response.data;
			} else {
				const errorMsg = response.data.message || 'Hiba történt az elutasítás során';
				toastError(errorMsg);
				return response.data;
			}
		} catch (err: any) {
			console.error('Error rejecting document:', err);
			const errorMsg = getErrorMessage(err) || 'Hiba történt az elutasítás során';
			toastError(errorMsg);
			throw err;
		} finally {
			isLoading.value = false;
		}
	}

	/**
	 * Delegate document to another user
	 */
	async function delegate(
		documentId: number,
		dto: WorkflowDelegateDto
	): Promise<WorkflowActionResult> {
		isLoading.value = true;
		try {
			const response = await api.post<WorkflowActionResult>(
				`/documents/${documentId}/workflow/delegate`,
				dto
			);
			
			if (response.data.success) {
				success('Dokumentum átadva');
				return response.data;
			} else {
				const errorMsg = response.data.message || 'Hiba történt az átadás során';
				toastError(errorMsg);
				return response.data;
			}
		} catch (err: any) {
			console.error('Error delegating document:', err);
			const errorMsg = getErrorMessage(err) || 'Hiba történt az átadás során';
			toastError(errorMsg);
			throw err;
		} finally {
			isLoading.value = false;
		}
	}

	/**
	 * Finalize document (advance from Accountant to Done)
	 */
	async function finalize(
		documentId: number,
		dto: WorkflowAdvanceDto = {}
	): Promise<WorkflowActionResult> {
		isLoading.value = true;
		try {
			const response = await api.post<WorkflowActionResult>(
				`/documents/${documentId}/workflow/advance`,
				dto
			);
			
			if (response.data.success) {
				success('Iktatás lezárva');
				return response.data;
			} else {
				const errorMsg = response.data.message || 'Hiba történt a lezárás során';
				toastError(errorMsg);
				return response.data;
			}
		} catch (err: any) {
			console.error('Error finalizing document:', err);
			const errorMsg = getErrorMessage(err) || 'Hiba történt a lezárás során';
			toastError(errorMsg);
			throw err;
		} finally {
			isLoading.value = false;
		}
	}

	return {
		isLoading,
		advance,
		stepBack,
		reject,
		delegate,
		finalize
	};
}


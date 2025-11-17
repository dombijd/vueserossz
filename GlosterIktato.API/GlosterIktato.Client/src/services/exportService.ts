/**
 * Document export service
 */
import api from './api';
import { downloadBlob, generateFileName } from '../utils/file.utils';

export interface ExportToExcelParams {
	documentIds: number[];
}

export interface ExportToPdfZipParams {
	documentIds: number[];
	includeRelated?: boolean;
}

/**
 * Export documents to Excel
 * @param params - Export parameters
 * @returns Promise that resolves when download starts
 */
export async function exportToExcel(params: ExportToExcelParams): Promise<void> {
	if (!params.documentIds || params.documentIds.length === 0) {
		throw new Error('Legalább egy dokumentum ID megadása kötelező');
	}

	const response = await api.post(
		'/documents/export/excel',
		{ documentIds: params.documentIds },
		{ responseType: 'blob' }
	);

	const blob = new Blob([response.data], {
		type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
	});

	const filename = generateFileName('dokumentumok_export', 'xlsx');
	downloadBlob(blob, filename);
}

/**
 * Export documents to PDF ZIP
 * @param params - Export parameters
 * @returns Promise that resolves when download starts
 */
export async function exportToPdfZip(params: ExportToPdfZipParams): Promise<void> {
	if (!params.documentIds || params.documentIds.length === 0) {
		throw new Error('Legalább egy dokumentum ID megadása kötelező');
	}

	const response = await api.post(
		'/documents/export/pdf-zip',
		{
			documentIds: params.documentIds,
			includeRelated: params.includeRelated ?? false
		},
		{ responseType: 'blob' }
	);

	const blob = new Blob([response.data], { type: 'application/zip' });
	const filename = generateFileName('dokumentumok_pdf', 'zip');
	downloadBlob(blob, filename);
}


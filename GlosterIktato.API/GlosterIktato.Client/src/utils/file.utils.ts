/**
 * File download utilities
 */

/**
 * Downloads a blob as a file
 * @param blob - The blob to download
 * @param filename - The filename for the downloaded file
 */
export function downloadBlob(blob: Blob, filename: string): void {
	const url = window.URL.createObjectURL(blob);
	const link = document.createElement('a');
	link.href = url;
	link.download = filename;
	document.body.appendChild(link);
	link.click();
	document.body.removeChild(link);
	window.URL.revokeObjectURL(url);
}

/**
 * Generates a filename with date suffix
 * @param baseName - Base name for the file
 * @param extension - File extension (e.g., 'xlsx', 'zip')
 * @returns Formatted filename with date
 */
export function generateFileName(baseName: string, extension: string): string {
	const dateStr = new Date().toISOString().split('T')[0];
	return `${baseName}_${dateStr}.${extension}`;
}


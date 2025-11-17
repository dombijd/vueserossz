/**
 * Currency formatting utilities
 */

/**
 * Format amount as currency using Hungarian locale
 * @param amount - The amount to format
 * @param currency - Currency code (default: 'HUF')
 * @returns Formatted currency string
 */
export function formatCurrency(amount: number, currency: string = 'HUF'): string {
	return new Intl.NumberFormat('hu-HU', {
		style: 'currency',
		currency: currency,
	}).format(amount);
}


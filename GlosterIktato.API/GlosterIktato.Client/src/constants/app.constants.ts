/**
 * Application-wide constants
 */

/**
 * Currency options for select dropdowns
 */
export const CURRENCY_OPTIONS = [
	{ label: 'HUF', value: 'HUF' },
	{ label: 'EUR', value: 'EUR' },
	{ label: 'USD', value: 'USD' },
] as const;

/**
 * Supported currency codes
 */
export type CurrencyCode = typeof CURRENCY_OPTIONS[number]['value'];

/**
 * Document type codes
 */
export const DOCUMENT_TYPE_CODES = {
	INVOICE: 'SZLA',
	CONTRACT: 'SZ',
	REQUEST: 'TIG',
} as const;

/**
 * Pagination defaults
 */
export const PAGINATION_DEFAULTS = {
	PAGE_SIZE: 20,
	MAX_VISIBLE_PAGES: 7,
} as const;


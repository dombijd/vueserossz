/**
 * Date formatting utilities
 */

/**
 * Format date to Hungarian locale with date and time
 * @param dateString - ISO date string
 * @returns Formatted date string (YYYY.MM.DD HH:mm) or '-' if invalid
 */
export function formatDateTime(dateString: string | null | undefined): string {
  if (!dateString) return '-';
  const date = new Date(dateString);
  if (isNaN(date.getTime())) return '-';

  return date.toLocaleDateString('hu-HU', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  });
}

/**
 * Format date to Hungarian locale with date only
 * @param dateString - ISO date string
 * @returns Formatted date string (YYYY.MM.DD) or '-' if invalid
 */
export function formatDate(dateString: string | null | undefined): string {
  if (!dateString) return '-';
  const date = new Date(dateString);
  if (isNaN(date.getTime())) return '-';

  return date.toLocaleDateString('hu-HU', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  });
}

/**
 * Format date to Hungarian locale with short month name
 * @param dateString - ISO date string
 * @returns Formatted date string (YYYY. Month DD., HH:mm) or '-' if invalid
 */
export function formatDateShort(dateString: string | null | undefined): string {
  if (!dateString) return '-';
  const date = new Date(dateString);
  if (isNaN(date.getTime())) return '-';

  return date.toLocaleString('hu-HU', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
}

/**
 * Format time only
 * @param dateString - ISO date string
 * @returns Formatted time string (HH:mm) or '-' if invalid
 */
export function formatTime(dateString: string | null | undefined): string {
  if (!dateString) return '-';
  const date = new Date(dateString);
  if (isNaN(date.getTime())) return '-';

  return date.toLocaleTimeString('hu-HU', {
    hour: '2-digit',
    minute: '2-digit'
  });
}

/**
 * Get relative time (e.g., "2 napja", "3 órája")
 * @param dateString - ISO date string
 * @returns Relative time string or '-' if invalid
 */
export function formatRelativeTime(dateString: string | null | undefined): string {
  if (!dateString) return '-';
  const date = new Date(dateString);
  if (isNaN(date.getTime())) return '-';

  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMs / 3600000);
  const diffDays = Math.floor(diffMs / 86400000);

  if (diffMins < 1) return 'most';
  if (diffMins < 60) return `${diffMins} perce`;
  if (diffHours < 24) return `${diffHours} órája`;
  if (diffDays < 30) return `${diffDays} napja`;

  return formatDate(dateString);
}

/**
 * Format date for input field (YYYY-MM-DD)
 * @param dateString - ISO date string
 * @returns Formatted date string for input (YYYY-MM-DD) or empty string if invalid
 */
export function formatDateForInput(dateString: string | null | undefined): string {
  if (!dateString) return '';
  const date = new Date(dateString);
  if (isNaN(date.getTime())) return '';
  return date.toISOString().split('T')[0];
}


import { computed, type Ref } from 'vue';
import type { PaginatedResult } from '@/types/document.types';
import { PAGINATION_DEFAULTS } from '@/constants/app.constants';

/**
 * Composable for pagination logic
 */
export function usePagination<T>(paginationData: Ref<PaginatedResult<T> | null>) {
	/**
	 * Calculate visible page numbers for pagination UI
	 * Shows ellipsis (...) when there are many pages
	 */
	const visiblePages = computed(() => {
		if (!paginationData.value) return [];

		const total = paginationData.value.totalPages;
		const current = paginationData.value.page;
		const pages: (number | string)[] = [];
		const maxVisible = PAGINATION_DEFAULTS.MAX_VISIBLE_PAGES;

		if (total <= maxVisible) {
			// Show all pages if 7 or fewer
			for (let i = 1; i <= total; i++) {
				pages.push(i);
			}
		} else {
			// Always show first page
			pages.push(1);

			if (current <= 4) {
				// Near the start
				for (let i = 2; i <= 5; i++) {
					pages.push(i);
				}
				pages.push('...');
				pages.push(total);
			} else if (current >= total - 3) {
				// Near the end
				pages.push('...');
				for (let i = total - 4; i <= total; i++) {
					pages.push(i);
				}
			} else {
				// In the middle
				pages.push('...');
				for (let i = current - 1; i <= current + 1; i++) {
					pages.push(i);
				}
				pages.push('...');
				pages.push(total);
			}
		}

		return pages;
	});

	/**
	 * Check if there's a previous page
	 */
	const hasPrevious = computed(() => {
		return paginationData.value && paginationData.value.page > 1;
	});

	/**
	 * Check if there's a next page
	 */
	const hasNext = computed(() => {
		return paginationData.value && paginationData.value.page < paginationData.value.totalPages;
	});

	return {
		visiblePages,
		hasPrevious,
		hasNext,
	};
}


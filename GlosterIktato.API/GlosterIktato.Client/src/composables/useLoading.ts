import { ref } from 'vue';

/**
 * Composable for managing loading states
 */
export function useLoading(initialState = false) {
	const isLoading = ref(initialState);

	/**
	 * Set loading state to true
	 */
	function startLoading() {
		isLoading.value = true;
	}

	/**
	 * Set loading state to false
	 */
	function stopLoading() {
		isLoading.value = false;
	}

	/**
	 * Execute an async function with automatic loading state management
	 * @param fn - Async function to execute
	 * @returns Result of the async function
	 */
	async function withLoading<T>(fn: () => Promise<T>): Promise<T> {
		try {
			startLoading();
			return await fn();
		} finally {
			stopLoading();
		}
	}

	return {
		isLoading,
		startLoading,
		stopLoading,
		withLoading,
	};
}


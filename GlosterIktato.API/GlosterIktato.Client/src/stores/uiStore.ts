import { defineStore } from 'pinia';
import { ref } from 'vue';

/**
 * UI store for managing UI state (sidebar, modals, etc.)
 */
export const useUIStore = defineStore('uiStore', () => {
	const sidebarCollapsed = ref<boolean>(false);

	function toggleSidebar(): void {
		sidebarCollapsed.value = !sidebarCollapsed.value;
	}

	function setSidebarCollapsed(collapsed: boolean): void {
		sidebarCollapsed.value = collapsed;
	}

	return {
		// state
		sidebarCollapsed,
		// actions
		toggleSidebar,
		setSidebarCollapsed
	};
});

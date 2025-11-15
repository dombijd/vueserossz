<template>
	<div class="flex h-screen w-screen overflow-hidden bg-gray-50">
		<!-- Sidebar -->
		<SideNav
			:collapsed="sidebarCollapsed"
			@toggle-collapse="uiStore.toggleSidebar"
			class="hidden md:block"
		/>

		<!-- Mobile sidebar overlay -->
		<div
			v-if="!sidebarCollapsed"
			class="fixed inset-0 z-40 bg-black bg-opacity-50 md:hidden"
			@click="uiStore.setSidebarCollapsed(true)"
		></div>

		<!-- Mobile sidebar -->
		<div
			class="fixed inset-y-0 left-0 z-50 w-64 transform bg-white shadow-lg transition-transform duration-200 ease-in-out md:hidden"
			:class="sidebarCollapsed ? '-translate-x-full' : 'translate-x-0'"
		>
			<SideNav
				:collapsed="false"
				@toggle-collapse="uiStore.toggleSidebar"
			/>
		</div>

		<!-- Main content area -->
		<div class="flex min-w-0 flex-1 flex-col">
			<!-- TopBar (fixed at top) -->
			<TopBar
				class="fixed top-0 left-0 right-0 z-50"
				:class="sidebarCollapsed ? 'md:left-16' : 'md:left-64'"
			/>

			<!-- Content area with padding and max-width -->
			<main
				class="min-h-0 flex-1 overflow-auto p-4 md:p-6 lg:p-8"
				style="margin-top: 64px;"
			>
				<div class="mx-auto max-w-7xl">
					<slot />
				</div>
			</main>
		</div>
	</div>
</template>

<script setup lang="ts">
import { watch } from 'vue';
import { storeToRefs } from 'pinia';
import { useRoute } from 'vue-router';
import SideNav from './SideNav.vue';
import TopBar from './TopBar.vue';
import { useUIStore } from '@/stores/uiStore';

/**
 * AppLayout component - Main application layout wrapper
 */
const uiStore = useUIStore();
const { sidebarCollapsed } = storeToRefs(uiStore);
const route = useRoute();

// Auto-collapse sidebar on mobile when route changes
watch(
	() => route.path,
	() => {
		if (window.innerWidth < 768) {
			uiStore.setSidebarCollapsed(true);
		}
	}
);
</script>
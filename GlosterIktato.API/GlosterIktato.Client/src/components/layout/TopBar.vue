<template>
	<div class="relative">
		<header
			class="flex h-16 items-center justify-between border-b border-gray-200 bg-white px-4 shadow-sm transition-all duration-200"
			:class="{ 'md:pl-16': sidebarCollapsed, 'md:pl-64': !sidebarCollapsed }"
		>
		<!-- Left: Logo with home icon -->
		<div class="flex items-center gap-3">
			<button
				type="button"
				class="md:hidden inline-flex items-center justify-center rounded-md p-2 text-gray-600 hover:bg-gray-100 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 transition-colors"
				@click="$emit('toggle-sidebar')"
				aria-label="Toggle sidebar"
			>
				<FontAwesomeIcon :icon="['fas', 'bars']" class="h-5 w-5" />
			</button>
			<router-link
				to="/dashboard"
				class="flex items-center gap-2 text-lg font-semibold text-gray-900 hover:text-blue-600 transition-colors"
				aria-label="Go to dashboard"
			>
				<FontAwesomeIcon :icon="['fas', 'home']" class="h-5 w-5" />
				<span>GlosterIktato</span>
			</router-link>
		</div>

		<!-- Center: Company selector (only if user has multiple companies) -->
		<div v-if="hasMultipleCompanies" class="hidden md:flex min-w-[220px] max-w-xs flex-1 items-center justify-center px-4">
			<BaseSelect
				:model-value="selectedCompanyId"
				:options="companyOptions"
				placeholder="Select company..."
				@update:modelValue="onCompanyChange"
				class="w-full"
			/>
		</div>
		<div v-else class="hidden md:flex flex-1"></div>

		<!-- Right: Notifications and user menu -->
		<nav class="flex items-center gap-2">
			<!-- Notifications bell with badge -->
			<div class="relative">
				<button
					type="button"
					class="relative inline-flex items-center justify-center rounded-md p-2 text-gray-600 hover:bg-gray-100 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 transition-colors"
					@click="showNotifications = !showNotifications"
					aria-label="Notifications"
					ref="notificationsButtonRef"
				>
					<FontAwesomeIcon :icon="['fas', 'bell']" class="h-5 w-5" />
					<span
						v-if="notificationCount > 0"
						class="absolute top-0 right-0 flex h-4 w-4 items-center justify-center rounded-full bg-red-500 text-xs font-bold text-white"
						aria-label="Notification count"
					>
						{{ notificationCount > 9 ? '9+' : notificationCount }}
					</span>
				</button>

				<!-- Notifications dropdown -->
				<Transition
					enter-active-class="transition ease-out duration-100"
					enter-from-class="transform opacity-0 scale-95"
					enter-to-class="transform opacity-100 scale-100"
					leave-active-class="transition ease-in duration-75"
					leave-from-class="transform opacity-100 scale-100"
					leave-to-class="transform opacity-0 scale-95"
				>
					<div
						v-if="showNotifications"
						ref="notificationsRef"
						class="absolute right-0 mt-2 w-80 origin-top-right rounded-md bg-white shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none z-50 max-h-96 overflow-y-auto"
						role="menu"
					>
						<div class="px-4 py-3 border-b border-gray-200">
							<p class="text-sm font-semibold text-gray-900">Notifications</p>
						</div>
						<div class="py-2">
							<p v-if="notificationCount === 0" class="px-4 py-8 text-center text-sm text-gray-500">
								No notifications
							</p>
							<!-- Notification items can be added here -->
						</div>
					</div>
				</Transition>
			</div>

			<!-- User menu dropdown -->
			<div class="relative">
				<button
					type="button"
					class="inline-flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-100 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 transition-colors"
					@click="showUserMenu = !showUserMenu"
					aria-label="User menu"
					ref="userMenuButtonRef"
				>
					<FontAwesomeIcon :icon="['fas', 'user']" class="h-5 w-5" />
					<span class="hidden sm:inline-block truncate max-w-[120px]">
						{{ userDisplayName }}
					</span>
				</button>

				<!-- User menu dropdown -->
				<Transition
					enter-active-class="transition ease-out duration-100"
					enter-from-class="transform opacity-0 scale-95"
					enter-to-class="transform opacity-100 scale-100"
					leave-active-class="transition ease-in duration-75"
					leave-from-class="transform opacity-100 scale-100"
					leave-to-class="transform opacity-0 scale-95"
				>
					<div
						v-if="showUserMenu"
						ref="userMenuRef"
						class="absolute right-0 mt-2 w-56 origin-top-right rounded-md bg-white shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none z-50"
						role="menu"
						aria-orientation="vertical"
					>
						<div class="px-4 py-3 border-b border-gray-200">
							<p class="text-sm font-medium text-gray-900 truncate">
								{{ userDisplayName }}
							</p>
							<p class="text-xs text-gray-500 truncate">
								{{ userEmail }}
							</p>
						</div>
						<div class="py-1">
							<button
								type="button"
								class="flex w-full items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 transition-colors"
								@click="handleLogout"
								role="menuitem"
							>
								<FontAwesomeIcon :icon="['fas', 'right-from-bracket']" class="h-4 w-4" />
								<span>Logout</span>
							</button>
						</div>
					</div>
				</Transition>
			</div>
		</nav>
	</header>
	</div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { storeToRefs } from 'pinia';
import { useRouter } from 'vue-router';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import BaseSelect from '../base/BaseSelect.vue';
import { useAuthStore } from '@/stores/authStore';
import { useUIStore } from '@/stores/uiStore';
import type { CompanyDto } from '@/types/auth.types';

/**
 * TopBar component props
 */
interface TopBarProps {
	/** Selected company ID */
	selectedCompanyId?: number | null;
	/** Notification count */
	notificationCount?: number;
}

const props = withDefaults(defineProps<TopBarProps>(), {
	selectedCompanyId: null,
	notificationCount: 0
});

const emit = defineEmits<{
	/** Emitted when company selection changes */
	(e: 'update:selectedCompanyId', value: number | null): void;
	/** Emitted when sidebar toggle is clicked (mobile) */
	(e: 'toggle-sidebar'): void;
}>();

const authStore = useAuthStore();
const uiStore = useUIStore();
const router = useRouter();

const { sidebarCollapsed } = storeToRefs(uiStore);

const showUserMenu = ref(false);
const showNotifications = ref(false);
const userMenuRef = ref<HTMLElement | null>(null);
const userMenuButtonRef = ref<HTMLElement | null>(null);
const notificationsRef = ref<HTMLElement | null>(null);
const notificationsButtonRef = ref<HTMLElement | null>(null);

// Mock user data - replace with actual user from authStore
const userEmail = computed(() => authStore.userEmail || 'user@example.com');
const userDisplayName = computed(() => {
	// This should come from authStore.user when implemented
	return userEmail.value.split('@')[0];
});

// Mock companies - replace with actual companies from authStore
const companies = computed<CompanyDto[]>(() => {
	// This should come from authStore.user?.companies when implemented
	return [];
});

const hasMultipleCompanies = computed(() => companies.value.length > 1);

const companyOptions = computed(() => {
	return companies.value.map((c) => ({
		label: c.name,
		value: c.id
	}));
});

const selectedCompanyId = computed(() => props.selectedCompanyId);

function onCompanyChange(val: unknown) {
	const num = typeof val === 'number' ? val : null;
	emit('update:selectedCompanyId', num);
}

function handleLogout() {
	authStore.logout();
	router.push({ name: 'login' });
	showUserMenu.value = false;
}

function handleClickOutside(e: MouseEvent) {
	const target = e.target as Node;

	if (showUserMenu.value && userMenuRef.value && userMenuButtonRef.value) {
		if (!userMenuRef.value.contains(target) && !userMenuButtonRef.value.contains(target)) {
			showUserMenu.value = false;
		}
	}

	if (showNotifications.value && notificationsRef.value && notificationsButtonRef.value) {
		if (!notificationsRef.value.contains(target) && !notificationsButtonRef.value.contains(target)) {
			showNotifications.value = false;
		}
	}
}

function handleEscape(e: KeyboardEvent) {
	if (e.key === 'Escape') {
		showUserMenu.value = false;
		showNotifications.value = false;
	}
}

onMounted(() => {
	document.addEventListener('mousedown', handleClickOutside);
	document.addEventListener('keydown', handleEscape);
});

onBeforeUnmount(() => {
	document.removeEventListener('mousedown', handleClickOutside);
	document.removeEventListener('keydown', handleEscape);
});
</script>
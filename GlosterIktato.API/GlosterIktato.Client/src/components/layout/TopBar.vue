<template>
	<header class="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-6 sticky top-0 z-[100]">
		<div class="flex items-center gap-4">
			<!-- Mobile Menu Toggle -->
			<button @click="$emit('toggle-sidebar')" class="md:hidden bg-transparent border-none text-2xl text-gray-600 cursor-pointer p-2 rounded-lg transition-colors duration-200 hover:bg-gray-50">
				<font-awesome-icon icon="bars" />
			</button>

			<!-- Page Title (optional) -->
			<h1 class="text-xl md:text-2xl font-semibold text-gray-800 m-0">{{ pageTitle }}</h1>
		</div>

		<div class="flex items-center gap-4">
			<!-- Notifications (placeholder) -->
			<button class="relative bg-transparent border-none text-xl text-gray-600 cursor-pointer p-2 rounded-lg transition-colors duration-200 hover:bg-gray-50" title="Értesítések">
				<font-awesome-icon icon="bell" />
				<span v-if="notificationCount > 0" class="absolute top-0 right-0 bg-red-500 text-white text-[0.65rem] font-semibold px-1.5 py-0.5 rounded-full min-w-[18px] text-center">{{ notificationCount }}</span>
			</button>

			<!-- User Menu -->
			<div ref="userMenuRef" class="relative cursor-pointer" @click="toggleUserMenu">
				<div class="flex items-center gap-3 px-3 py-2 rounded-lg transition-colors duration-200 hover:bg-gray-50">
					<div class="w-10 h-10 rounded-full bg-gradient-to-br from-indigo-500 to-purple-600 text-white flex items-center justify-center font-semibold text-sm">
						{{ userInitials }}
					</div>
					<div class="hidden md:flex flex-col gap-0.5">
						<span class="font-semibold text-gray-800 text-sm">{{ userName }}</span>
						<span class="text-xs text-gray-500">{{ primaryRole }}</span>
					</div>
					<font-awesome-icon icon="chevron-down" class="hidden md:block text-xs text-gray-400 transition-transform duration-200 group-hover:translate-y-0.5" />
				</div>

				<!-- Dropdown Menu -->
				<div v-if="showUserMenu" class="absolute top-full right-0 mt-2 bg-white border border-gray-200 rounded-lg shadow-lg min-w-[220px] z-[1000]">
					<div class="p-4 flex flex-col gap-1">
						<strong class="text-gray-800 text-sm">{{ userName }}</strong>
						<span class="text-gray-500 text-xs">{{ userEmail }}</span>
					</div>

					<div class="h-px bg-gray-200 my-2"></div>

					<a href="#" class="flex items-center gap-3 px-4 py-3 text-gray-600 no-underline transition-colors duration-200 hover:bg-gray-50" @click.prevent="goToProfile">
						<font-awesome-icon icon="user" class="text-base text-gray-500" />
						<span>Profil</span>
					</a>

					<a href="#" class="flex items-center gap-3 px-4 py-3 text-gray-600 no-underline transition-colors duration-200 hover:bg-gray-50" @click.prevent="goToSettings">
						<font-awesome-icon icon="gear" class="text-base text-gray-500" />
						<span>Beállítások</span>
					</a>

					<div class="h-px bg-gray-200 my-2"></div>

					<a href="#" class="flex items-center gap-3 px-4 py-3 text-red-600 no-underline transition-colors duration-200 hover:bg-gray-50" @click.prevent="handleLogout">
						<font-awesome-icon icon="right-from-bracket" class="text-base text-red-600" />
						<span>Kijelentkezés</span>
					</a>
				</div>
			</div>
		</div>
	</header>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/authStore';
import { useToast } from '@/composables/useToast';

defineEmits<{
	'toggle-sidebar': []
}>();

const router = useRouter();
const authStore = useAuthStore();
const { success } = useToast();

// State
const showUserMenu = ref<boolean>(false);
const notificationCount = ref<number>(0); // Placeholder
const userMenuRef = ref<HTMLElement | null>(null);

// Computed
const userName = computed(() => authStore.userName || 'User');
const userEmail = computed(() => authStore.user?.email || '');
const primaryRole = computed(() => {
	const roles = authStore.userRoles;
	if (roles.includes('Admin')) return 'Admin';
	if (roles.includes('ElevatedApprover')) return 'Felső Jóváhagyó';
	if (roles.includes('Approver')) return 'Jóváhagyó';
	if (roles.includes('Accountant')) return 'Könyvelő';
	return 'Felhasználó';
});
const userInitials = computed(() => {
	const user = authStore.user;
	if (!user) return 'U';
	const first = user.firstName?.charAt(0) || '';
	const last = user.lastName?.charAt(0) || '';
	return (first + last).toUpperCase() || 'U';
});
const pageTitle = computed(() => {
	// TODO: Get from route meta or store
	return 'Dashboard';
});

// Methods
function toggleUserMenu() {
	showUserMenu.value = !showUserMenu.value;
}

function closeUserMenu() {
	showUserMenu.value = false;
}

function goToProfile() {
	closeUserMenu();
	router.push('/profile');
}

function goToSettings() {
	closeUserMenu();
	router.push('/settings');
}

async function handleLogout() {
	closeUserMenu();
	authStore.logout();
	success('Sikeresen kijelentkezett');
	router.push('/login');
}

// Close dropdown when clicking outside
function handleClickOutside(event: MouseEvent) {
	const target = event.target as HTMLElement;
	if (userMenuRef.value && !userMenuRef.value.contains(target)) {
		closeUserMenu();
	}
}

onMounted(() => {
	document.addEventListener('click', handleClickOutside);
});

onUnmounted(() => {
	document.removeEventListener('click', handleClickOutside);
});
</script>

<template>
	<header class="top-bar">
		<div class="top-bar-left">
			<!-- Mobile Menu Toggle -->
			<button @click="$emit('toggle-sidebar')" class="menu-toggle">
				<font-awesome-icon icon="bars" />
			</button>

			<!-- Page Title (optional) -->
			<h1 class="page-title">{{ pageTitle }}</h1>
		</div>

		<div class="top-bar-right">
			<!-- Notifications (placeholder) -->
			<button class="icon-button" title="Értesítések">
				<font-awesome-icon icon="bell" />
				<span v-if="notificationCount > 0" class="badge">{{ notificationCount }}</span>
			</button>

			<!-- User Menu -->
			<div class="user-menu" @click="toggleUserMenu">
				<div class="user-info">
					<div class="user-avatar">
						{{ userInitials }}
					</div>
					<div class="user-details">
						<span class="user-name">{{ userName }}</span>
						<span class="user-role">{{ primaryRole }}</span>
					</div>
					<font-awesome-icon icon="chevron-down" class="dropdown-icon" />
				</div>

				<!-- Dropdown Menu -->
				<div v-if="showUserMenu" class="user-dropdown">
					<div class="dropdown-header">
						<strong>{{ userName }}</strong>
						<span class="user-email">{{ userEmail }}</span>
					</div>

					<div class="dropdown-divider"></div>

					<a href="#" class="dropdown-item" @click.prevent="goToProfile">
						<font-awesome-icon icon="user" />
						<span>Profil</span>
					</a>

					<a href="#" class="dropdown-item" @click.prevent="goToSettings">
						<font-awesome-icon icon="gear" />
						<span>Beállítások</span>
					</a>

					<div class="dropdown-divider"></div>

					<a href="#" class="dropdown-item logout" @click.prevent="handleLogout">
						<font-awesome-icon icon="right-from-bracket" />
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
	if (!target.closest('.user-menu')) {
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

<style scoped>
.top-bar {
	height: 64px;
	background: white;
	border-bottom: 1px solid #e2e8f0;
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 0 1.5rem;
	position: sticky;
	top: 0;
	z-index: 100;
}

.top-bar-left {
	display: flex;
	align-items: center;
	gap: 1rem;
}

.menu-toggle {
	display: none;
	background: none;
	border: none;
	font-size: 1.5rem;
	color: #4a5568;
	cursor: pointer;
	padding: 0.5rem;
	border-radius: 8px;
	transition: background 0.2s;
}

.menu-toggle:hover {
	background: #f7fafc;
}

.page-title {
	font-size: 1.25rem;
	font-weight: 600;
	color: #2d3748;
	margin: 0;
}

.top-bar-right {
	display: flex;
	align-items: center;
	gap: 1rem;
}

.icon-button {
	position: relative;
	background: none;
	border: none;
	font-size: 1.25rem;
	color: #4a5568;
	cursor: pointer;
	padding: 0.5rem;
	border-radius: 8px;
	transition: background 0.2s;
}

.icon-button:hover {
	background: #f7fafc;
}

.badge {
	position: absolute;
	top: 0;
	right: 0;
	background: #f56565;
	color: white;
	font-size: 0.65rem;
	font-weight: 600;
	padding: 0.15rem 0.4rem;
	border-radius: 10px;
	min-width: 18px;
	text-align: center;
}

.user-menu {
	position: relative;
	cursor: pointer;
}

.user-info {
	display: flex;
	align-items: center;
	gap: 0.75rem;
	padding: 0.5rem 0.75rem;
	border-radius: 8px;
	transition: background 0.2s;
}

.user-info:hover {
	background: #f7fafc;
}

.user-avatar {
	width: 40px;
	height: 40px;
	border-radius: 50%;
	background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
	color: white;
	display: flex;
	align-items: center;
	justify-content: center;
	font-weight: 600;
	font-size: 0.9rem;
}

.user-details {
	display: flex;
	flex-direction: column;
	gap: 0.15rem;
}

.user-name {
	font-weight: 600;
	color: #2d3748;
	font-size: 0.9rem;
}

.user-role {
	font-size: 0.75rem;
	color: #718096;
}

.dropdown-icon {
	font-size: 0.75rem;
	color: #a0aec0;
	transition: transform 0.2s;
}

.user-menu:hover .dropdown-icon {
	transform: translateY(2px);
}

.user-dropdown {
	position: absolute;
	top: calc(100% + 0.5rem);
	right: 0;
	background: white;
	border: 1px solid #e2e8f0;
	border-radius: 8px;
	box-shadow: 0 10px 25px rgba(0, 0, 0, 0.1);
	min-width: 220px;
	z-index: 1000;
}

.dropdown-header {
	padding: 1rem;
	display: flex;
	flex-direction: column;
	gap: 0.25rem;
}

.dropdown-header strong {
	color: #2d3748;
	font-size: 0.95rem;
}

.user-email {
	color: #718096;
	font-size: 0.85rem;
}

.dropdown-divider {
	height: 1px;
	background: #e2e8f0;
	margin: 0.5rem 0;
}

.dropdown-item {
	display: flex;
	align-items: center;
	gap: 0.75rem;
	padding: 0.75rem 1rem;
	color: #4a5568;
	text-decoration: none;
	transition: background 0.2s;
}

.dropdown-item:hover {
	background: #f7fafc;
}

.dropdown-item svg {
	font-size: 1rem;
	color: #718096;
}

.dropdown-item.logout {
	color: #e53e3e;
}

.dropdown-item.logout svg {
	color: #e53e3e;
}

@media (max-width: 768px) {
	.menu-toggle {
		display: block;
	}

	.page-title {
		font-size: 1rem;
	}

	.user-details {
		display: none;
	}

	.dropdown-icon {
		display: none;
	}
}
</style>
import api, { getErrorMessage } from '@/services/api';
import type { CompanyDto, LoginResponseDto, UserDto } from '@/types/auth.types';
import { defineStore } from 'pinia';
import { ref, computed } from 'vue';

export interface LoginPayload {
	email: string;
	password: string;
}

export const useAuthStore = defineStore('authStore', () => {
	// State
	const token = ref<string | null>(localStorage.getItem('auth_token'));
	const user = ref<UserDto | null>(null);
	const isLoading = ref<boolean>(false);
	const error = ref<string | null>(null);

	// Load user from localStorage on init
	const storedUser = localStorage.getItem('auth_user');
	if (storedUser) {
		try {
			user.value = JSON.parse(storedUser);
		} catch (e) {
			console.error('Failed to parse stored user', e);
			localStorage.removeItem('auth_user');
		}
	}

	// Computed
	const isAuthenticated = computed<boolean>(() => Boolean(token.value && user.value));
	const userName = computed<string>(() => 
		user.value ? `${user.value.firstName} ${user.value.lastName}` : ''
	);
	const userRoles = computed<string[]>(() => user.value?.roles || []);
	const isAdmin = computed<boolean>(() => userRoles.value.includes('Admin'));
	const userCompanies = computed<CompanyDto[]>(() => user.value?.companies || []);

	// Actions
	async function login(payload: LoginPayload): Promise<void> {
		isLoading.value = true;
		error.value = null;

		try {
			// POST /api/auth/login
			const response = await api.post<LoginResponseDto>('/auth/login', payload);
			const { token: authToken, user: userInfo } = response.data;

			// Save to state
			token.value = authToken;
			user.value = userInfo;

			// Save to localStorage
			localStorage.setItem('auth_token', authToken);
			localStorage.setItem('auth_user', JSON.stringify(userInfo));

			console.log('Login successful:', userInfo.email);
		} catch (err: any) {
			const errorMessage = getErrorMessage(err) || 'Invalid credentials';
			error.value = errorMessage;
			console.error('Login failed:', errorMessage);
			throw new Error(errorMessage);
		} finally {
			isLoading.value = false;
		}
	}

	async function refreshUser(): Promise<void> {
		if (!token.value) return;

		try {
			// Optional: GET /api/auth/me endpoint to refresh user info
			const response = await api.get<UserDto>('/auth/me');
			user.value = response.data;
			localStorage.setItem('auth_user', JSON.stringify(response.data));
		} catch (err) {
			console.error('Failed to refresh user info:', err);
			// If refresh fails, logout
			logout();
		}
	}

	function logout(): void {
		token.value = null;
		user.value = null;
		error.value = null;

		localStorage.removeItem('auth_token');
		localStorage.removeItem('auth_user');

		console.log('✅ Logout successful');
	}

	function clearError(): void {
		error.value = null;
	}

	return {
		// State
		token,
		user,
		isLoading,
		error,
		// Getters
		isAuthenticated,
		userName,
		userRoles,
		isAdmin,
		userCompanies,
		// Actions
		login,
		logout,
		refreshUser,
		clearError
	};
});



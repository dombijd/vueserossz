import { defineStore } from 'pinia';
import { ref, computed } from 'vue';

export interface LoginPayload {
	email: string;
	password: string;
}

export const useAuthStore = defineStore('authStore', () => {
	const token = ref<string | null>(localStorage.getItem('auth_token'));
	const userEmail = ref<string | null>(localStorage.getItem('auth_email'));

	const isAuthenticated = computed<boolean>(() => Boolean(token.value));

	async function login(payload: LoginPayload): Promise<void> {
		// TODO: Replace with real authentication API call and proper token handling
		// Mock login for now
		if (payload.email && payload.password) {
			token.value = 'mock-token';
			userEmail.value = payload.email;
			localStorage.setItem('auth_token', token.value);
			localStorage.setItem('auth_email', userEmail.value);
		} else {
			throw new Error('Invalid credentials');
		}
	}

	function logout(): void {
		token.value = null;
		userEmail.value = null;
		localStorage.removeItem('auth_token');
		localStorage.removeItem('auth_email');
	}

	return {
		// state
		token,
		userEmail,
		// getters
		isAuthenticated,
		// actions
		login,
		logout
	};
});



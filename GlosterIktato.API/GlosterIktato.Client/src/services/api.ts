import axios, { AxiosError, type AxiosInstance } from 'axios'

export const baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5156/api';

// Create axios instance
const api: AxiosInstance = axios.create({
	baseURL,
	headers: {
		'Content-Type': 'application/json',
	},
	timeout: 30000,
});

// ============================================================
// REQUEST INTERCEPTOR
// ============================================================

api.interceptors.request.use(
	(config) => {
		// Add auth token if available
		const token = localStorage.getItem('auth_token');
		if (token) {
			config.headers.Authorization = `Bearer ${token}`;
		}

		// For FormData, remove Content-Type header to let axios set it automatically with boundary
		if (config.data instanceof FormData) {
			delete config.headers['Content-Type'];
		}

		return config;
	},
	(error) => {
		return Promise.reject(error);
	}
);

// ============================================================
// RESPONSE INTERCEPTOR
// ============================================================

api.interceptors.response.use(
	(response) => {
		// Success response - return as is
		return response;
	},
	(error: AxiosError) => {
		// Error handling
		if (error.response) {
			// Server responded with error status
			const status = error.response.status;
			const data = error.response.data as any;

			switch (status) {
				case 401:
					// Unauthorized - clear token and redirect to login
					// Don't clear tokens for login endpoint failures (they're expected)
					const isLoginRequest = error.config?.url?.includes('/auth/login');
					if (!isLoginRequest) {
						console.error('❌ Unauthorized - redirecting to login');
						localStorage.removeItem('auth_token');
						localStorage.removeItem('auth_user');
						
						// Only redirect if not already on login page
						if (window.location.pathname !== '/login') {
							window.location.href = '/login';
						}
					}
					break;

				case 403:
					// Forbidden
					console.error('❌ Forbidden - insufficient permissions');
					break;

				case 404:
					// Not found
					console.error('❌ Resource not found');
					break;

				case 500:
					// Internal server error
					console.error('❌ Server error:', data?.message || 'Internal server error');
					break;

				default:
					console.error(`❌ API Error [${status}]:`, data?.message || error.message);
			}
		} else if (error.request) {
			// Request was made but no response received
			const requestUrl = error.config?.url || 'unknown';
			const fullUrl = `${baseURL}${requestUrl.startsWith('/') ? '' : '/'}${requestUrl}`;
			
			console.error('❌ Network error - no response from server');
			console.error('Request URL:', requestUrl);
			console.error('Full URL:', fullUrl);
			console.error('Base URL:', baseURL);
			console.error('Error code:', error.code);
			console.error('Full error:', error);
			
			// Provide helpful error message
			if (error.code === 'ERR_NETWORK' || error.code === 'ECONNREFUSED') {
				console.error('💡 Tip: Make sure the API server is running on', baseURL);
				console.error('💡 Check if the server is accessible at:', fullUrl);
			}
		} else {
			// Something else happened
			console.error('❌ Request setup error:', error.message);
		}

		return Promise.reject(error);
	}
);

// ============================================================
// EXPORTS
// ============================================================

export default api;

// Helper function to extract error message
export function getErrorMessage(error: any): string {
	if (error.response?.data?.message) {
		return error.response.data.message;
	}
	
	// Network errors
	if (error.code === 'ERR_NETWORK' || error.code === 'ECONNREFUSED') {
		return `Nem sikerült csatlakozni a szerverhez. Ellenőrizze, hogy a backend API fut-e a következő címen: ${baseURL}`;
	}
	
	if (error.message) {
		return error.message;
	}
	return 'An unexpected error occurred';
}

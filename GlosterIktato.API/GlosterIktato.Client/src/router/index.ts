import { createRouter, createWebHistory } from 'vue-router';
import LoginPage from '@/components/features/LoginPage.vue';
import LandingPage from '@/components/features/LandingPage.vue';
import DocumentUploadPage from '@/components/features/DocumentUploadPage.vue';
import { useAuthStore } from '@/stores/authStore';

const router = createRouter({
	history: createWebHistory(import.meta.env.BASE_URL),
	routes: [
		{
			path: '/',
			redirect: '/dashboard'
		},
		{
			path: '/login',
			name: 'login',
			component: LoginPage,
			meta: { requiresAuth: false }
		},
		{
			path: '/dashboard',
			name: 'dashboard',
			component: LandingPage,
			meta: { requiresAuth: true }
		},
		// Document routes
		{
			path: '/documents',
			name: 'documents',
			component: LandingPage, // TODO: Replace with DocumentsPage
			meta: { requiresAuth: true }
		},
		{
			path: '/documents/upload',
			name: 'documents-upload',
			component: DocumentUploadPage,
			meta: { requiresAuth: true }
		},
		{
			path: '/documents/:id',
			name: 'document-detail',
			component: LandingPage, // TODO: Replace with DocumentDetailPage
			meta: { requiresAuth: true }
		},
		// Settings
		{
			path: '/settings',
			name: 'settings',
			component: LandingPage, // TODO: Replace with SettingsPage
			meta: { requiresAuth: true }
		},
		// Profile
		{
			path: '/profile',
			name: 'profile',
			component: LandingPage, // TODO: Replace with ProfilePage
			meta: { requiresAuth: true }
		},
		// Admin routes
		{
			path: '/admin/users',
			name: 'admin-users',
			component: LandingPage, // TODO: Replace with AdminUsersPage
			meta: { requiresAuth: true, requiresAdmin: true }
		},
		{
			path: '/admin/user-groups',
			name: 'admin-user-groups',
			component: LandingPage, // TODO: Replace with AdminUserGroupsPage
			meta: { requiresAuth: true, requiresAdmin: true }
		},
		// 404
		{
			path: '/:pathMatch(.*)*',
			name: 'not-found',
			redirect: '/dashboard'
		}
	]
});

// ============================================================
// NAVIGATION GUARDS
// ============================================================

router.beforeEach((to, from, next) => {
	const auth = useAuthStore();

	// Check if route requires authentication
	if (to.meta?.requiresAuth) {
		if (!auth.isAuthenticated) {
			// Not authenticated - redirect to login
			console.log('🔒 Route requires auth - redirecting to login');
			return next({
				name: 'login',
				query: { redirect: to.fullPath }
			});
		}

		// Check if route requires admin
		if (to.meta?.requiresAdmin && !auth.isAdmin) {
			// Not admin - redirect to dashboard
			console.log('🔒 Route requires admin - access denied');
			return next({ name: 'dashboard' });
		}
	}

	// If already authenticated and trying to access login page
	if (to.name === 'login' && auth.isAuthenticated) {
		console.log('✅ Already authenticated - redirecting to dashboard');
		return next({ name: 'dashboard' });
	}

	// Allow navigation
	next();
});

router.afterEach((to) => {
	// Update page title
	document.title = `${to.meta?.title || 'Dashboard'} | Iktatórendszer`;
});

export default router;
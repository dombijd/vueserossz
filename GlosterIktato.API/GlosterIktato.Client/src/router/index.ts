import { createRouter, createWebHistory } from 'vue-router';
import LoginPage from '@/components/features/LoginPage.vue';
import LandingPage from '@/components/features/LandingPage.vue';
import DashboardPage from '@/components/features/DashboardPage.vue';
import DocumentUploadPage from '@/components/features/DocumentUploadPage.vue';
import DocumentsListPage from '@/components/features/DocumentsListPage.vue';
import DocumentDetailPage from '@/components/features/DocumentDetailPage.vue';
import SuppliersPage from '@/components/features/SuppliersPage.vue';
import UsersPage from '@/components/features/admin/UsersPage.vue';
import CompaniesPage from '@/components/features/admin/CompaniesPage.vue';
import UserGroupsPage from '@/components/features/admin/UserGroupsPage.vue';
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
			component: DashboardPage,
			meta: { requiresAuth: true }
		},
		// Document routes
		{
			path: '/documents',
			name: 'documents',
			component: DocumentsListPage,
			meta: { requiresAuth: true, title: 'Aktuális Ügyeim' }
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
			component: DocumentDetailPage,
			meta: { requiresAuth: true, title: 'Dokumentum karton' }
		},
		{
			path: '/search',
			name: 'search',
			component: () => import('@/components/features/SearchPage.vue'),
			meta: { requiresAuth: true, title: 'Dokumentum keresés' }
		},
		{
			path: '/suppliers',
			name: 'suppliers',
			component: SuppliersPage,
			meta: { requiresAuth: true, title: 'Szállítók kezelése' }
		},
		// Settings
		{
			path: '/settings',
			name: 'settings',
			component: DashboardPage, // TODO: Replace with SettingsPage
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
			component: UsersPage,
			meta: { requiresAuth: true, requiresAdmin: true, title: 'Felhasználók kezelése' }
		},
		{
			path: '/admin/companies',
			name: 'admin-companies',
			component: CompaniesPage,
			meta: { requiresAuth: true, requiresAdmin: true, title: 'Cégek kezelése' }
		},
		{
			path: '/admin/user-groups',
			name: 'admin-user-groups',
			component: UserGroupsPage,
			meta: { requiresAuth: true, requiresAdmin: true, title: 'Felhasználói Csoportok kezelése' }
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
	document.title = `${to.meta?.title || 'Összefoglaló'} | Gloster Iktatórendszer`;
});

export default router;
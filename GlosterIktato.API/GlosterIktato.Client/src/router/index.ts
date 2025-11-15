import { createRouter, createWebHistory } from 'vue-router'
import LoginPage from '@/components/features/LoginPage.vue'
import LandingPage from '@/components/features/LandingPage.vue'
import { useAuthStore } from '@/stores/authStore'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', redirect: '/login' },
    { path: '/login', name: 'login', component: LoginPage },
    { path: '/dashboard', name: 'dashboard', component: LandingPage, meta: { requiresAuth: true } },
    // example protected placeholders
    { path: '/documents', name: 'documents', component: LandingPage, meta: { requiresAuth: true } },
    { path: '/settings', name: 'settings', component: LandingPage, meta: { requiresAuth: true } },
  ]
})

router.beforeEach((to) => {
  const auth = useAuthStore()
  if (to.meta?.requiresAuth && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }
  if (to.name === 'login' && auth.isAuthenticated) {
    return { name: 'dashboard' }
  }
  return true
})

export default router
<template>
	<AppLayout
		:nav-items="navItems"
		:companies="companies"
		:selected-company-id="selectedCompanyId"
		@update:selectedCompanyId="(val) => selectedCompanyId = val"
		@open-user-menu="logout"
	>
		<div class="space-y-4">
			<h1 class="text-2xl font-semibold text-gray-900">Welcome</h1>
			<p class="text-gray-600">This is a protected landing page. You are logged in as <span class="font-medium">{{ auth.userEmail }}</span>.</p>
			<div class="flex gap-2">
				<BaseButton variant="secondary" :left-icon="['fas','file']">New Document</BaseButton>
				<BaseButton variant="success" :left-icon="['fas','arrow-rotate-right']">Refresh</BaseButton>
				<BaseButton variant="danger" :left-icon="['fas','right-from-bracket']" @click="logout">Logout</BaseButton>
			</div>
		</div>
	</AppLayout>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import AppLayout from '../layout/AppLayout.vue';
import BaseButton from '../base/BaseButton.vue';
import type { Company } from '../../types/user.types';
import { useAuthStore } from '../../stores/authStore';
import { useRouter } from 'vue-router';

const router = useRouter();
const auth = useAuthStore();

const navItems = ref([
	{ label: 'Dashboard', to: '/dashboard', icon: ['fas','gauge'], exact: true },
	{ label: 'Documents', icon: ['fas','file'], children: [
		{ label: 'All Documents', to: '/documents' },
		{ label: 'Create', to: '/documents/new' }
	]},
	{ label: 'Settings', to: '/settings', icon: ['fas','gear'] }
]);

const companies = ref<Company[]>([
	{ Id: 1, Name: 'Acme Inc.', TaxNumber: '12345678', IsActive: true, CreatedAt: new Date().toISOString(), Users: [], Documents: [] },
	{ Id: 2, Name: 'Globex', TaxNumber: '98765432', IsActive: true, CreatedAt: new Date().toISOString(), Users: [], Documents: [] }
]);
const selectedCompanyId = ref<number | null>(1);

function logout() {
	auth.logout();
	router.replace('/login');
}
</script>



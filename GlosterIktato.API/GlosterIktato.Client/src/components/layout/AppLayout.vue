<template>
	<div class="flex h-screen w-screen overflow-hidden bg-gray-50">
		<SideNav
			v-model:collapsed="collapsed"
			:items="navItems"
		/>

		<div class="flex min-w-0 flex-1 flex-col">
			<TopBar
				:logo-text="logoText"
				:logo-src="logoSrc"
				:companies="companies"
				:selected-company-id="selectedCompanyId"
				@update:selectedCompanyId="val => $emit('update:selectedCompanyId', val)"
				@open-notifications="$emit('open-notifications')"
				@open-user-menu="$emit('open-user-menu')"
				@toggle-sidebar="collapsed = !collapsed"
			/>

			<main class="min-h-0 flex-1 overflow-auto p-4">
				<slot />
			</main>
		</div>
	</div>
	<!-- optional footer slot -->
	<slot name="footer" />
</template>

<script setup lang="ts">
import { ref } from 'vue';
import SideNav from './SideNav.vue';
import TopBar from './TopBar.vue';
import type { Company } from '../../types/user.types';
import type { IconDefinition, IconProp } from '@fortawesome/fontawesome-svg-core';

interface NavItem {
	label: string;
	to?: string;
	icon?: IconProp | IconDefinition | [string, string] | string;
	children?: NavItem[];
	exact?: boolean;
}

interface AppLayoutProps {
	navItems?: NavItem[];
	companies?: Company[];
	selectedCompanyId?: number | null;
	logoText?: string;
	logoSrc?: string;
	defaultCollapsed?: boolean;
}

const props = withDefaults(defineProps<AppLayoutProps>(), {
	navItems: () => [],
	companies: () => [],
	selectedCompanyId: null,
	logoText: 'App',
	defaultCollapsed: false
});

defineEmits<{
	(e: 'update:selectedCompanyId', value: number | null): void;
	(e: 'open-notifications'): void;
	(e: 'open-user-menu'): void;
}>();

const collapsed = ref<boolean>(props.defaultCollapsed);
const navItems = props.navItems;
const companies = props.companies;
const selectedCompanyId = props.selectedCompanyId;
const logoText = props.logoText;
const logoSrc = props.logoSrc;
</script>



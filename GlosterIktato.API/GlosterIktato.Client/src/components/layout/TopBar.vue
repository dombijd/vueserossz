<template>
	<header class="flex h-14 items-center justify-between border-b border-gray-200 bg-white px-4">
		<!-- Left: Logo -->
		<div class="flex items-center gap-2">
			<button
				v-if="showSidebarToggle"
				type="button"
				class="inline-flex items-center justify-center rounded-md p-2 text-gray-600 hover:bg-gray-100 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500"
				@click="$emit('toggle-sidebar')"
				:aria-label="'Toggle sidebar'"
			>
				<FontAwesomeIcon :icon="['fas','bars']" class="h-5 w-5" />
			</button>
			<div class="flex items-center gap-2">
				<img v-if="logoSrc" :src="logoSrc" alt="Logo" class="h-7 w-auto" />
				<span v-else class="text-base font-semibold text-gray-800">{{ logoText }}</span>
			</div>
		</div>

		<!-- Center: Company selector -->
		<div class="min-w-[220px] max-w-xs w-full px-4">
			<BaseSelect
				:model-value="selectedCompanyId"
				:options="companyOptions"
				placeholder="Select company..."
				@update:modelValue="onCompanyChange"
			/>
		</div>

		<!-- Right: Actions -->
		<nav class="flex items-center gap-2">
			<BaseButton
				variant="ghost"
				size="sm"
				:aria-label="'Notifications'"
				@click="$emit('open-notifications')"
				:right-icon="['fas','bell']"
			>
				Notifications
			</BaseButton>
			<BaseButton
				variant="ghost"
				size="sm"
				:aria-label="'User menu'"
				@click="$emit('open-user-menu')"
				:right-icon="['fas','user']"
			>
				Account
			</BaseButton>
		</nav>
	</header>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import BaseButton from '../base/BaseButton.vue';
import BaseSelect from '../base/BaseSelect.vue';
import type { Company } from '../../types/user.types';

interface TopBarProps {
	logoText?: string;
	logoSrc?: string;
	companies?: Company[];
	selectedCompanyId?: number | null;
	showSidebarToggle?: boolean;
}

const props = withDefaults(defineProps<TopBarProps>(), {
	logoText: 'App',
	companies: () => [],
	selectedCompanyId: null,
	showSidebarToggle: true
});

const emit = defineEmits<{
	(e: 'update:selectedCompanyId', value: number | null): void;
	(e: 'open-notifications'): void;
	(e: 'open-user-menu'): void;
	(e: 'toggle-sidebar'): void;
}>();

const companyOptions = computed(() => {
	return (props.companies ?? []).map((c) => ({
		label: c.Name,
		value: c.Id
	}));
});

function onCompanyChange(val: unknown) {
	const num = typeof val === 'number' ? val : null;
	emit('update:selectedCompanyId', num);
}
</script>



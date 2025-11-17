<template>
	<div class="border-b border-gray-200">
		<nav class="-mb-px flex space-x-8" aria-label="Tabs">
			<button
				v-for="tab in tabs"
				:key="tab.value"
				@click="$emit('update:modelValue', tab.value)"
				:class="[
					modelValue === tab.value
						? 'border-blue-500 text-blue-600'
						: 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700',
					'group inline-flex items-center border-b-2 py-4 px-1 text-sm font-medium transition-colors'
				]"
			>
				<font-awesome-icon
					v-if="tab.icon"
					:icon="tab.icon"
					:class="[
						modelValue === tab.value
							? 'text-blue-500'
							: 'text-gray-400 group-hover:text-gray-500',
						'-ml-0.5 mr-2 h-5 w-5'
					]"
				/>
				<span>{{ tab.label }}</span>
				<span
					v-if="tab.count !== undefined"
					:class="[
						modelValue === tab.value
							? 'bg-blue-100 text-blue-600'
							: 'bg-gray-100 text-gray-900',
						'ml-2 rounded-full px-2.5 py-0.5 text-xs font-medium'
					]"
				>
					{{ tab.count }}
				</span>
			</button>
		</nav>
	</div>
</template>

<script setup lang="ts">
export interface Tab {
	value: string;
	label: string;
	icon?: [string, string];
	count?: number;
}

interface Props {
	modelValue: string;
	tabs: Tab[];
}

defineProps<Props>();

defineEmits<{
	'update:modelValue': [value: string];
}>();
</script>


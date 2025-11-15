<template>
	<div
		:class="cardClass"
		class="bg-white rounded-lg shadow-sm"
		:aria-label="title || 'Card'"
	>
		<div v-if="title || $slots.header" class="px-6 py-4 border-b border-gray-200">
			<slot name="header">
				<h3 v-if="title" class="text-lg font-semibold text-gray-900">
					{{ title }}
				</h3>
			</slot>
		</div>
		<div class="px-6 py-4">
			<slot />
		</div>
		<div v-if="$slots.footer" class="px-6 py-4 border-t border-gray-200">
			<slot name="footer" />
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

/**
 * BaseCard component props
 */
interface BaseCardProps {
	/** Optional title text displayed in header */
	title?: string;
	/** Whether to show border around card */
	bordered?: boolean;
}

const props = withDefaults(defineProps<BaseCardProps>(), {
	bordered: true
});

const cardClass = computed(() => {
	const classes = ['bg-white', 'rounded-lg', 'shadow-sm'];
	if (props.bordered) {
		classes.push('border', 'border-gray-200');
	}
	return classes.join(' ');
});
</script>


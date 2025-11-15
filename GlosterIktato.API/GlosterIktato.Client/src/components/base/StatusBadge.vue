<template>
	<span
		:class="badgeClass"
		class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-medium"
		:aria-label="`Status: ${status}`"
	>
		<FontAwesomeIcon :icon="statusIcon" class="h-3 w-3 shrink-0" />
		<span>{{ status }}</span>
	</span>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { DocumentStatus, getStatusColor, getStatusIcon } from '@/types/document.types';
import type { IconDefinition } from '@fortawesome/fontawesome-svg-core';

/**
 * StatusBadge component props
 */
interface StatusBadgeProps {
	/** Document status to display */
	status: DocumentStatus | string;
}

const props = defineProps<StatusBadgeProps>();

const badgeClass = computed(() => {
	return getStatusColor(props.status);
});

const statusIcon = computed<IconDefinition | [string, string]>(() => {
	return getStatusIcon(props.status);
});
</script>


<template>
	<span
		:class="badgeClass"
		class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-medium"
		:aria-label="`Status: ${status}`"
	>
		<font-awesome-icon :icon="statusIcon" class="h-3 w-3 shrink-0" />
		<span>{{ displayName }}</span>
	</span>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { DocumentStatus, getStatusColor, getStatusIcon, getStatusDisplayName } from '@/types/document.types';
import type { IconDefinition } from '@/types/fontawesome.types';

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

const displayName = computed(() => {
	return getStatusDisplayName(props.status);
});
</script>


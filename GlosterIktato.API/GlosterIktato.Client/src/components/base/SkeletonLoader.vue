<template>
	<div class="animate-pulse">
		<!-- Table Skeleton -->
		<div v-if="type === 'table'" class="space-y-3">
			<div class="h-10 bg-gray-200 rounded"></div>
			<div v-for="i in rows" :key="i" class="h-16 bg-gray-100 rounded"></div>
		</div>

		<!-- Card Skeleton -->
		<div v-else-if="type === 'card'" class="space-y-4">
			<div class="h-6 bg-gray-200 rounded w-1/4"></div>
			<div class="h-4 bg-gray-100 rounded w-3/4"></div>
			<div class="h-4 bg-gray-100 rounded w-1/2"></div>
		</div>

		<!-- List Skeleton -->
		<div v-else-if="type === 'list'" class="space-y-2">
			<div v-for="i in rows" :key="i" class="h-12 bg-gray-100 rounded"></div>
		</div>

		<!-- Custom -->
		<div v-else>
			<slot />
		</div>
	</div>
</template>

<script setup lang="ts">
interface Props {
	type?: 'table' | 'card' | 'list' | 'custom';
	rows?: number;
}

withDefaults(defineProps<Props>(), {
	type: 'custom',
	rows: 5,
});
</script>


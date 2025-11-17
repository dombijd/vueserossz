<script setup lang="ts">
import ToastContainer from '@/components/base/ToastContainer.vue';
import ErrorBoundary from '@/components/base/ErrorBoundary.vue';
</script>

<template>
	<ErrorBoundary>
		<router-view />
		<!-- Root app renders active route -->
		<ToastContainer />
	</ErrorBoundary>
</template>

<style scoped></style>

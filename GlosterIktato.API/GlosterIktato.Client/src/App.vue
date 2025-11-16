<script setup lang="ts">
import ToastContainer from '@/components/base/ToastContainer.vue';
</script>

<template>
	<router-view />
	<!-- Root app renders active route -->
	<ToastContainer />
</template>

<style scoped></style>

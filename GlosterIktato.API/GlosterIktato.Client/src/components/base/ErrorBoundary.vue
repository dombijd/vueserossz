<template>
	<div v-if="hasError" class="min-h-screen flex items-center justify-center bg-gray-50">
		<BaseCard class="max-w-md">
			<div class="text-center py-8">
				<font-awesome-icon
					:icon="['fas', 'exclamation-triangle']"
					class="text-5xl text-red-500 mb-4"
				/>
				<h2 class="text-xl font-semibold text-gray-900 mb-2">
					Váratlan hiba történt
				</h2>
				<p class="text-gray-600 mb-4">
					{{ errorMessage }}
				</p>
				<div class="flex gap-2 justify-center">
					<BaseButton variant="primary" @click="reload">
						Oldal újratöltése
					</BaseButton>
					<BaseButton variant="secondary" @click="goHome">
						Főoldal
					</BaseButton>
				</div>
			</div>
		</BaseCard>
	</div>
	<slot v-else />
</template>

<script setup lang="ts">
import { ref, onErrorCaptured } from 'vue';
import { useRouter } from 'vue-router';
import BaseCard from './BaseCard.vue';
import BaseButton from './BaseButton.vue';

const router = useRouter();
const hasError = ref(false);
const errorMessage = ref('');

onErrorCaptured((err: Error) => {
	hasError.value = true;
	errorMessage.value = err.message || 'Ismeretlen hiba';
	console.error('Error boundary caught:', err);
	return false; // Prevent error propagation
});

function reload() {
	window.location.reload();
}

function goHome() {
	router.push('/dashboard');
}
</script>


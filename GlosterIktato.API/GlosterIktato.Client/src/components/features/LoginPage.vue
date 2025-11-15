<template>
	<div class="min-h-screen flex items-center justify-center bg-gradient-to-br from-indigo-500 to-purple-600 p-8">
		<div class="w-full max-w-[450px]">
			<div class="bg-white rounded-2xl shadow-2xl p-12 sm:p-8">
				<!-- Logo / Header -->
				<div class="text-center mb-8">
					<h1 class="text-2xl sm:text-3xl font-bold text-gray-900 mb-2">Iktatórendszer</h1>
					<p class="text-gray-500 text-sm sm:text-base">Jelentkezzen be a folytatáshoz</p>
				</div>

				<!-- Login Form -->
				<form @submit.prevent="handleSubmit" class="flex flex-col gap-6">
					<!-- Email Input -->
					<div class="flex flex-col gap-2">
						<label for="email" class="font-semibold text-gray-700 text-sm">Email cím</label>
						<BaseInput
							id="email"
							v-model="email"
							type="email"
							placeholder="pelda@gloster.hu"
							:disabled="isLoading"
							required
						/>
					</div>

					<!-- Password Input -->
					<div class="flex flex-col gap-2">
						<label for="password" class="font-semibold text-gray-700 text-sm">Jelszó</label>
						<BaseInput
							id="password"
							v-model="password"
							type="password"
							placeholder="••••••••"
							:disabled="isLoading"
							required
						/>
					</div>

					<!-- Error Message -->
					<div v-if="errorMessage" class="flex items-center gap-2 px-4 py-3 bg-red-100 border border-red-300 rounded-lg text-red-700 text-sm">
						<font-awesome-icon icon="exclamation-circle" />
						<span>{{ errorMessage }}</span>
					</div>

					<!-- Submit Button -->
					<BaseButton
						type="submit"
						variant="primary"
						:disabled="isLoading"
						class="w-full py-3.5 text-base font-semibold mt-2"
					>
						<font-awesome-icon v-if="isLoading" icon="spinner" spin />
						<span v-else>Bejelentkezés</span>
					</BaseButton>
				</form>

				<!-- Demo Credentials Info -->
				<div class="mt-8 pt-8 border-t border-gray-200">
					<p class="text-sm font-semibold text-gray-600 mb-3">Demo belépési adatok:</p>
					<div class="flex flex-col gap-2">
						<div class="text-xs text-gray-500 px-2 py-2 bg-gray-50 rounded-md cursor-pointer transition-all duration-200 hover:bg-gray-100 hover:text-gray-700">
							<strong class="text-gray-700 mr-2">Admin:</strong> admin@gloster.hu / admin123
						</div>
						<div class="text-xs text-gray-500 px-2 py-2 bg-gray-50 rounded-md cursor-pointer transition-all duration-200 hover:bg-gray-100 hover:text-gray-700">
							<strong class="text-gray-700 mr-2">Iktató:</strong> iktato@gloster.hu / iktato123
						</div>
						<div class="text-xs text-gray-500 px-2 py-2 bg-gray-50 rounded-md cursor-pointer transition-all duration-200 hover:bg-gray-100 hover:text-gray-700">
							<strong class="text-gray-700 mr-2">Jóváhagyó:</strong> jóváhagyó@gloster.hu / jovahagyo123
						</div>
						<div class="text-xs text-gray-500 px-2 py-2 bg-gray-50 rounded-md cursor-pointer transition-all duration-200 hover:bg-gray-100 hover:text-gray-700">
							<strong class="text-gray-700 mr-2">Vezető:</strong> vezeto@gloster.hu / vezeto123
						</div>
						<div class="text-xs text-gray-500 px-2 py-2 bg-gray-50 rounded-md cursor-pointer transition-all duration-200 hover:bg-gray-100 hover:text-gray-700">
							<strong class="text-gray-700 mr-2">Könyvelő:</strong> konyvelő@gloster.hu / konyvelo123
						</div>
					</div>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/authStore';
import { useToast } from '@/composables/useToast';
import BaseInput from '@/components/base/BaseInput.vue';
import BaseButton from '@/components/base/BaseButton.vue';

const router = useRouter();
const authStore = useAuthStore();
const { success, error: toastError } = useToast();

// Form state
const email = ref<string>('');
const password = ref<string>('');
const errorMessage = ref<string>('');

const isLoading = ref<boolean>(false);

async function handleSubmit() {
	// Clear previous error
	errorMessage.value = '';
	authStore.clearError();

	// Validation
	if (!email.value || !password.value) {
		errorMessage.value = 'Kérem adja meg az email címét és jelszavát';
		return;
	}

	isLoading.value = true;

	try {
		// Login API call
		await authStore.login({
			email: email.value,
			password: password.value
		});

		// Success
		success(`Üdvözöljük, ${authStore.userName}!`);

		// Redirect to dashboard
		const redirectPath = (router.currentRoute.value.query.redirect as string) || '/dashboard';
		router.push(redirectPath);
	} catch (err: any) {
		// Error handling
		errorMessage.value = err.message || 'Bejelentkezés sikertelen';
		toastError(errorMessage.value);
	} finally {
		isLoading.value = false;
	}
}

// Quick login helper (for demo)
function quickLogin(demoEmail: string, demoPassword: string) {
	email.value = demoEmail;
	password.value = demoPassword;
	handleSubmit();
}
</script>

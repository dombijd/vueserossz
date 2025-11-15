<template>
	<div class="login-page">
		<div class="login-container">
			<div class="login-card">
				<!-- Logo / Header -->
				<div class="login-header">
					<h1>Iktatórendszer</h1>
					<p>Jelentkezzen be a folytatáshoz</p>
				</div>

				<!-- Login Form -->
				<form @submit.prevent="handleSubmit" class="login-form">
					<!-- Email Input -->
					<div class="form-group">
						<label for="email">Email cím</label>
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
					<div class="form-group">
						<label for="password">Jelszó</label>
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
					<div v-if="errorMessage" class="error-message">
						<font-awesome-icon icon="exclamation-circle" />
						<span>{{ errorMessage }}</span>
					</div>

					<!-- Submit Button -->
					<BaseButton
						type="submit"
						variant="primary"
						:disabled="isLoading"
						class="login-button"
					>
						<font-awesome-icon v-if="isLoading" icon="spinner" spin />
						<span v-else>Bejelentkezés</span>
					</BaseButton>
				</form>

				<!-- Demo Credentials Info -->
				<div class="demo-info">
					<p class="demo-title">Demo belépési adatok:</p>
					<div class="demo-credentials">
						<div class="demo-item">
							<strong>Admin:</strong> admin@gloster.hu / admin123
						</div>
						<div class="demo-item">
							<strong>Iktató:</strong> iktato@gloster.hu / iktato123
						</div>
						<div class="demo-item">
							<strong>Jóváhagyó:</strong> jóváhagyó@gloster.hu / jovahagyo123
						</div>
						<div class="demo-item">
							<strong>Vezető:</strong> vezeto@gloster.hu / vezeto123
						</div>
						<div class="demo-item">
							<strong>Könyvelő:</strong> konyvelő@gloster.hu / konyvelo123
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

<style scoped>
.login-page {
	min-height: 100vh;
	display: flex;
	align-items: center;
	justify-content: center;
	background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
	padding: 2rem;
}

.login-container {
	width: 100%;
	max-width: 450px;
}

.login-card {
	background: white;
	border-radius: 16px;
	box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
	padding: 3rem;
}

.login-header {
	text-align: center;
	margin-bottom: 2rem;
}

.login-header h1 {
	font-size: 2rem;
	font-weight: 700;
	color: #1a202c;
	margin-bottom: 0.5rem;
}

.login-header p {
	color: #718096;
	font-size: 0.95rem;
}

.login-form {
	display: flex;
	flex-direction: column;
	gap: 1.5rem;
}

.form-group {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
}

.form-group label {
	font-weight: 600;
	color: #2d3748;
	font-size: 0.9rem;
}

.error-message {
	display: flex;
	align-items: center;
	gap: 0.5rem;
	padding: 0.75rem 1rem;
	background: #fed7d7;
	border: 1px solid #fc8181;
	border-radius: 8px;
	color: #c53030;
	font-size: 0.9rem;
}

.login-button {
	width: 100%;
	padding: 0.875rem;
	font-size: 1rem;
	font-weight: 600;
	margin-top: 0.5rem;
}

.demo-info {
	margin-top: 2rem;
	padding-top: 2rem;
	border-top: 1px solid #e2e8f0;
}

.demo-title {
	font-size: 0.85rem;
	font-weight: 600;
	color: #4a5568;
	margin-bottom: 0.75rem;
}

.demo-credentials {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
}

.demo-item {
	font-size: 0.8rem;
	color: #718096;
	padding: 0.5rem;
	background: #f7fafc;
	border-radius: 6px;
	cursor: pointer;
	transition: all 0.2s;
}

.demo-item:hover {
	background: #edf2f7;
	color: #2d3748;
}

.demo-item strong {
	color: #2d3748;
	margin-right: 0.5rem;
}

@media (max-width: 640px) {
	.login-card {
		padding: 2rem;
	}

	.login-header h1 {
		font-size: 1.5rem;
	}
}
</style>
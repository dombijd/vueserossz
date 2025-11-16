<template>
	<div
		class="fixed top-4 right-4 z-[9999] flex flex-col gap-2 pointer-events-none"
		aria-live="polite"
		aria-atomic="true"
	>
		<TransitionGroup
			name="toast"
			tag="div"
			enter-active-class="transition-all duration-300 ease-in-out"
			leave-active-class="transition-all duration-300 ease-in-out"
			enter-from-class="opacity-0 translate-x-full"
			leave-to-class="opacity-0 translate-x-full"
			move-class="transition-transform duration-300 ease-in-out"
		>
			<div
				v-for="toast in toasts"
				:key="toast.id"
				:class="toastClass(toast.type)"
				class="pointer-events-auto min-w-[300px] max-w-md shadow-lg rounded-lg p-4 flex items-start gap-3"
				role="alert"
			>
				<FontAwesomeIcon
					:icon="toastIcon(toast.type)"
					:class="iconClass(toast.type)"
					class="shrink-0 mt-0.5"
				/>
				<div class="flex-1 min-w-0">
					<p class="text-sm font-medium">{{ toast.message }}</p>
				</div>
				<button
					@click="remove(toast.id)"
					type="button"
					class="shrink-0 text-gray-400 hover:text-gray-600 transition-colors"
					aria-label="Close notification"
				>
					<FontAwesomeIcon :icon="['fas', 'times']" class="h-4 w-4" />
				</button>
			</div>
		</TransitionGroup>
	</div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import { useToast, type ToastType } from '@/composables/useToast';

const { toasts, remove } = useToast();

function toastClass(type: ToastType): string {
	const base = 'border';
	switch (type) {
		case 'success':
			return `${base} border-emerald-200 bg-emerald-50 text-emerald-800`;
		case 'error':
			return `${base} border-rose-200 bg-rose-50 text-rose-800`;
		case 'info':
			return `${base} border-blue-200 bg-blue-50 text-blue-800`;
		default:
			return `${base} border-gray-200 bg-white text-gray-800`;
	}
}

function iconClass(type: ToastType): string {
	switch (type) {
		case 'success':
			return 'text-emerald-600';
		case 'error':
			return 'text-rose-600';
		case 'info':
			return 'text-blue-600';
		default:
			return 'text-gray-600';
	}
}

function toastIcon(type: ToastType): [string, string] {
	switch (type) {
		case 'success':
			return ['fas', 'check-circle'];
		case 'error':
			return ['fas', 'exclamation-circle'];
		case 'info':
			return ['fas', 'info-circle'];
		default:
			return ['fas', 'info-circle'];
	}
}
</script>


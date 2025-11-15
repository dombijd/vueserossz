<template>
	<Teleport to="body">
		<Transition
			enter-active-class="transition-opacity duration-200"
			enter-from-class="opacity-0"
			enter-to-class="opacity-100"
			leave-active-class="transition-opacity duration-200"
			leave-from-class="opacity-100"
			leave-to-class="opacity-0"
		>
			<div
				v-if="modelValue"
				class="fixed inset-0 z-50 overflow-y-auto"
				@click.self="handleBackdropClick"
				role="dialog"
				aria-modal="true"
				:aria-labelledby="title ? `${modalId}-title` : undefined"
			>
				<!-- Backdrop -->
				<div class="fixed inset-0 bg-black bg-opacity-50 transition-opacity" aria-hidden="true"></div>

				<!-- Modal container -->
				<div class="flex min-h-full items-center justify-center p-4">
					<Transition
						enter-active-class="transition-all duration-200"
						enter-from-class="opacity-0 scale-95 translate-y-4"
						enter-to-class="opacity-100 scale-100 translate-y-0"
						leave-active-class="transition-all duration-200"
						leave-from-class="opacity-100 scale-100 translate-y-0"
						leave-to-class="opacity-0 scale-95 translate-y-4"
					>
						<div
							v-if="modelValue"
							ref="modalRef"
							:class="modalSizeClass"
							class="relative bg-white rounded-lg shadow-xl w-full max-w-full"
							@click.stop
						>
							<!-- Header -->
							<div
								v-if="title || $slots.header"
								class="flex items-center justify-between px-6 py-4 border-b border-gray-200"
							>
								<slot name="header">
									<h2
										v-if="title"
										:id="`${modalId}-title`"
										class="text-xl font-semibold text-gray-900"
									>
										{{ title }}
									</h2>
								</slot>
								<button
									@click="handleClose"
									class="ml-4 rounded-md p-2 text-gray-400 hover:bg-gray-100 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-colors"
									aria-label="Close modal"
								>
									<FontAwesomeIcon :icon="['fas', 'times']" class="h-5 w-5" />
								</button>
							</div>

							<!-- Body -->
							<div class="px-6 py-4 overflow-y-auto max-h-[calc(100vh-200px)]">
								<slot />
							</div>

							<!-- Footer -->
							<div
								v-if="$slots.footer"
								class="flex items-center justify-end gap-3 px-6 py-4 border-t border-gray-200"
							>
								<slot name="footer" />
							</div>
						</div>
					</Transition>
				</div>
			</div>
		</Transition>
	</Teleport>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';

/**
 * Modal size variants
 */
type ModalSize = 'sm' | 'md' | 'lg' | 'full';

/**
 * BaseModal component props
 */
interface BaseModalProps {
	/** Whether modal is visible (v-model) */
	modelValue: boolean;
	/** Optional title text displayed in header */
	title?: string;
	/** Modal size variant */
	size?: ModalSize;
}

const props = withDefaults(defineProps<BaseModalProps>(), {
	size: 'md'
});

const emit = defineEmits<{
	/** Emitted when modal visibility changes (v-model) */
	(e: 'update:modelValue', value: boolean): void;
	/** Emitted when modal is closed */
	(e: 'close'): void;
}>();

const modalRef = ref<HTMLElement | null>(null);
const modalId = `modal-${Math.random().toString(36).slice(2)}`;

const modalSizeClass = computed(() => {
	switch (props.size) {
		case 'sm':
			return 'max-w-md';
		case 'lg':
			return 'max-w-3xl';
		case 'full':
			return 'max-w-full mx-4';
		case 'md':
		default:
			return 'max-w-xl';
	}
});

function handleClose() {
	emit('update:modelValue', false);
	emit('close');
}

function handleBackdropClick() {
	handleClose();
}

function handleEscape(e: KeyboardEvent) {
	if (e.key === 'Escape' && props.modelValue) {
		handleClose();
	}
}

onMounted(() => {
	document.addEventListener('keydown', handleEscape);
	if (props.modelValue) {
		document.body.style.overflow = 'hidden';
	}
});

onBeforeUnmount(() => {
	document.removeEventListener('keydown', handleEscape);
	document.body.style.overflow = '';
});

watch(
	() => props.modelValue,
	(isOpen) => {
		if (isOpen) {
			document.body.style.overflow = 'hidden';
			// Focus trap: focus first focusable element
			setTimeout(() => {
				const firstFocusable = modalRef.value?.querySelector(
					'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
				) as HTMLElement;
				firstFocusable?.focus();
			}, 100);
		} else {
			document.body.style.overflow = '';
		}
	}
);
</script>


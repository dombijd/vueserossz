<template>
	<BaseModal
		:model-value="modelValue"
		@update:model-value="handleModelValueUpdate"
		@close="handleClose"
		:title="title"
		size="sm"
	>
		<div class="space-y-4">
			<p class="text-sm text-gray-600">{{ message }}</p>
		</div>
		<template #footer>
			<BaseButton variant="secondary" @click="handleCancel">
				{{ cancelText }}
			</BaseButton>
			<BaseButton
				:variant="variant"
				@click="handleConfirm"
				:loading="loading"
			>
				{{ confirmText }}
			</BaseButton>
		</template>
	</BaseModal>
</template>

<script setup lang="ts">
import BaseModal from './BaseModal.vue';
import BaseButton from './BaseButton.vue';

interface Props {
	modelValue: boolean;
	title: string;
	message: string;
	confirmText?: string;
	cancelText?: string;
	variant?: 'primary' | 'danger' | 'success';
	loading?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	confirmText: 'Megerősítés',
	cancelText: 'Mégse',
	variant: 'primary',
	loading: false,
});

const emit = defineEmits<{
	'update:modelValue': [value: boolean];
	confirm: [];
	cancel: [];
}>();

function handleModelValueUpdate(value: boolean) {
	emit('update:modelValue', value);
}

function handleClose() {
	// BaseModal close event kezelése (pl. Escape vagy backdrop click)
	emit('cancel');
	emit('update:modelValue', false);
}

function handleCancel() {
	emit('cancel');
	emit('update:modelValue', false);
}

function handleConfirm() {
	emit('confirm');
	// Ne zárjuk be automatikusan, hadd kezelje a parent
}
</script>


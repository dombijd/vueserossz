<template>
	<BaseModal 
		:model-value="modelValue" 
		@update:model-value="$emit('update:modelValue', $event)"
		title="Dokumentum elutasítása" 
		size="md"
	>
		<div class="space-y-4">
			<p class="text-sm text-gray-600">
				Kérjük, adja meg az elutasítás indoklását (minimum 10 karakter):
			</p>
			<textarea
				v-model="reason"
				rows="4"
				class="block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
				placeholder="Indoklás..."
				required
			></textarea>
			<p v-if="error" class="text-sm text-rose-600">{{ error }}</p>
		</div>
		<template #footer>
			<BaseButton variant="secondary" @click="handleCancel">Mégse</BaseButton>
			<BaseButton variant="danger" @click="handleConfirm" :disabled="!isValidReason">
				Elutasítás
			</BaseButton>
		</template>
	</BaseModal>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import BaseModal from '@/components/base/BaseModal.vue';
import BaseButton from '@/components/base/BaseButton.vue';

interface Props {
	modelValue: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update:modelValue': [value: boolean];
	confirm: [reason: string];
}>();

const reason = ref('');
const error = ref('');

const MIN_REASON_LENGTH = 10;

const isValidReason = computed(() => {
	const trimmed = reason.value.trim();
	return trimmed.length >= MIN_REASON_LENGTH;
});

watch(() => props.modelValue, (newValue) => {
	if (!newValue) {
		reason.value = '';
		error.value = '';
	}
});

watch(reason, () => {
	// Töröljük a hibát, amikor a felhasználó elkezd gépelni
	if (error.value) {
		error.value = '';
	}
});

function handleCancel() {
	emit('update:modelValue', false);
}

function handleConfirm() {
	const trimmed = reason.value.trim();
	
	if (!trimmed) {
		error.value = 'Indoklás kötelező';
		return;
	}
	
	if (trimmed.length < MIN_REASON_LENGTH) {
		error.value = `Az indoklásnak legalább ${MIN_REASON_LENGTH} karakternek kell lennie`;
		return;
	}
	
	emit('confirm', trimmed);
	emit('update:modelValue', false);
}
</script>


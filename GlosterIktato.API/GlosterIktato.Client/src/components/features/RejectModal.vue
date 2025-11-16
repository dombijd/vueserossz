<template>
	<BaseModal 
		:model-value="modelValue" 
		@update:model-value="$emit('update:modelValue', $event)"
		title="Dokumentum elutasítása" 
		size="md"
	>
		<div class="space-y-4">
			<p class="text-sm text-gray-600">
				Kérjük, adja meg az elutasítás indoklását:
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
			<BaseButton variant="danger" @click="handleConfirm" :disabled="!reason.trim()">
				Elutasítás
			</BaseButton>
		</template>
	</BaseModal>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
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

watch(() => props.modelValue, (newValue) => {
	if (!newValue) {
		reason.value = '';
		error.value = '';
	}
});

function handleCancel() {
	emit('update:modelValue', false);
}

function handleConfirm() {
	if (!reason.value.trim()) {
		error.value = 'Indoklás kötelező';
		return;
	}
	emit('confirm', reason.value.trim());
	emit('update:modelValue', false);
}
</script>


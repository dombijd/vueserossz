<template>
	<BaseModal 
		:model-value="modelValue" 
		@update:model-value="$emit('update:modelValue', $event)"
		title="Dokumentum átadása" 
		size="md"
	>
		<div class="space-y-4">
			<p class="text-sm text-gray-600">
				Válassza ki, hogy kinek szeretné átadni a dokumentumot:
			</p>
			<BaseSelect
				v-model="selectedUserId"
				:options="userOptions"
				label="Felhasználó"
				placeholder="Válassz felhasználót"
				required
			/>
		</div>
		<template #footer>
			<BaseButton variant="secondary" @click="handleCancel">Mégse</BaseButton>
			<BaseButton variant="primary" @click="handleConfirm" :disabled="!selectedUserId">
				Átadás
			</BaseButton>
		</template>
	</BaseModal>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import BaseModal from '@/components/base/BaseModal.vue';
import BaseButton from '@/components/base/BaseButton.vue';
import BaseSelect from '@/components/base/BaseSelect.vue';

interface User {
	id: number;
	email: string;
	firstName: string;
	lastName: string;
}

interface Props {
	modelValue: boolean;
	users: User[];
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update:modelValue': [value: boolean];
	confirm: [userId: number];
}>();

const selectedUserId = ref<number | null>(null);

const userOptions = computed(() => {
	return props.users.map(u => ({
		label: `${u.firstName} ${u.lastName} (${u.email})`,
		value: u.id
	}));
});

watch(() => props.modelValue, (newValue) => {
	if (!newValue) {
		selectedUserId.value = null;
	}
});

function handleCancel() {
	emit('update:modelValue', false);
}

function handleConfirm() {
	if (!selectedUserId.value) {
		return;
	}
	emit('confirm', selectedUserId.value);
	emit('update:modelValue', false);
}
</script>


<template>
	<BaseModal 
		:model-value="modelValue" 
		@update:model-value="$emit('update:modelValue', $event)"
		title="Dokumentum megtekintése" 
		size="full"
	>
		<div class="h-[calc(100vh-200px)]">
			<iframe
				:src="pdfUrl"
				class="w-full h-full border-0"
				frameborder="0"
			></iframe>
		</div>
	</BaseModal>
</template>

<script setup lang="ts">
import BaseModal from '@/components/base/BaseModal.vue';

interface Props {
	modelValue: boolean;
	pdfUrl: string;
}

defineProps<Props>();

defineEmits<{
	'update:modelValue': [value: boolean];
}>();
</script>


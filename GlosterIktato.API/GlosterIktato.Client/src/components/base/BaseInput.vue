<template>
	<div class="w-full">
		<label v-if="label" :for="inputId" class="mb-1 block text-sm font-medium text-gray-700">
			{{ label }}
			<span v-if="required" class="text-rose-600">*</span>
		</label>
		<div class="relative">
			<input
				:id="inputId"
				:name="name"
				:type="type"
				:inputmode="inputMode"
				:placeholder="placeholder"
				:disabled="disabled"
				:step="step"
				:class="inputClass"
				:value="displayValue"
				@input="onInput"
				@blur="$emit('blur')"
				@focus="$emit('focus')"
				:aria-invalid="Boolean(error) ? 'true' : 'false'"
				:aria-describedby="error ? `${inputId}-error` : undefined"
			/>
		</div>
		<p
			v-if="error"
			:id="`${inputId}-error`"
			class="mt-1 text-sm text-rose-600"
			role="alert"
		>
			{{ error }}
		</p>
	</div>
	<!-- helper text slot -->
	<slot name="help" />
</template>

<script setup lang="ts">
import { computed } from 'vue';

type InputType = 'text' | 'number' | 'email' | 'password';

interface BaseInputProps<TValue = string | number | null> {
	modelValue: TValue;
	type?: InputType;
	label?: string;
	name?: string;
	id?: string;
	placeholder?: string;
	disabled?: boolean;
	required?: boolean;
	error?: string;
	step?: string | number;
}

const props = withDefaults(defineProps<BaseInputProps>(), {
	type: 'text',
	modelValue: ''
});

const emit = defineEmits<{
	(e: 'update:modelValue', value: string | number | null): void;
	(e: 'blur'): void;
	(e: 'focus'): void;
}>();

const inputId = computed(() => props.id ?? `in-${Math.random().toString(36).slice(2)}`);

const inputMode = computed(() => {
	switch (props.type) {
		case 'email': return 'email';
		case 'number': return 'numeric';
		default: return undefined;
	}
});

const displayValue = computed(() => {
	if (props.type === 'number') {
		return props.modelValue ?? '';
	}
	return (props.modelValue ?? '') as string | number;
});

const inputClass = computed(() => {
	const base = 'block w-full rounded-md border bg-white px-3 py-2 text-sm shadow-sm placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-offset-0 disabled:cursor-not-allowed disabled:bg-gray-100 disabled:text-gray-500';
	const normal = 'border-gray-300 focus:ring-blue-500';
	const danger = 'border-rose-300 focus:ring-rose-500';
	return [base, props.error ? danger : normal].join(' ');
});

function onInput(e: Event) {
	const target = e.target as HTMLInputElement;
	if (props.type === 'number') {
		const value = target.value;
		if (value === '') {
			emit('update:modelValue', null);
		} else {
			const parsed = Number(value);
			emit('update:modelValue', Number.isNaN(parsed) ? null : parsed);
		}
	} else {
		emit('update:modelValue', target.value);
	}
}
</script>



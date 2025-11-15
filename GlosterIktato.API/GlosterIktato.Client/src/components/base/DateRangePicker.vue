<template>
	<div class="w-full">
		<label v-if="label" :for="startInputId" class="mb-1 block text-sm font-medium text-gray-700">
			{{ label }}
		</label>
		<div class="flex flex-col sm:flex-row gap-3">
			<!-- Start date input -->
			<div class="flex-1">
				<label
					:for="startInputId"
					class="block text-xs font-medium text-gray-500 mb-1"
				>
					From
				</label>
				<input
					:id="startInputId"
					:name="startInputId"
					type="date"
					:value="displayStartValue"
					@input="handleStartChange"
					:class="inputClass"
					:aria-label="`Start date${label ? ` for ${label}` : ''}`"
					:aria-describedby="error ? `${startInputId}-error` : undefined"
					:aria-invalid="Boolean(error) ? 'true' : 'false'"
				/>
			</div>

			<!-- End date input -->
			<div class="flex-1">
				<label
					:for="endInputId"
					class="block text-xs font-medium text-gray-500 mb-1"
				>
					To
				</label>
				<input
					:id="endInputId"
					:name="endInputId"
					type="date"
					:value="displayEndValue"
					@input="handleEndChange"
					:class="inputClass"
					:min="modelValueStart"
					:aria-label="`End date${label ? ` for ${label}` : ''}`"
					:aria-describedby="error ? `${endInputId}-error` : undefined"
					:aria-invalid="Boolean(error) ? 'true' : 'false'"
				/>
			</div>
		</div>

		<!-- Error message -->
		<p
			v-if="error"
			:id="`${startInputId}-error`"
			class="mt-1 text-sm text-rose-600"
			role="alert"
		>
			{{ error }}
		</p>

		<!-- Validation message -->
		<p
			v-if="validationError"
			class="mt-1 text-sm text-amber-600"
			role="alert"
		>
			{{ validationError }}
		</p>
	</div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';

/**
 * DateRangePicker component props
 */
interface DateRangePickerProps {
	/** Start date value (v-model) */
	modelValueStart?: string | null;
	/** End date value (v-model) */
	modelValueEnd?: string | null;
	/** Label for the date range picker */
	label?: string;
	/** Error message to display */
	error?: string;
}

const props = withDefaults(defineProps<DateRangePickerProps>(), {
	modelValueStart: null,
	modelValueEnd: null
});

const emit = defineEmits<{
	/** Emitted when start date changes (v-model) */
	(e: 'update:modelValueStart', value: string | null): void;
	/** Emitted when end date changes (v-model) */
	(e: 'update:modelValueEnd', value: string | null): void;
}>();

const startInputId = `date-start-${Math.random().toString(36).slice(2)}`;
const endInputId = `date-end-${Math.random().toString(36).slice(2)}`;
const validationError = ref<string | null>(null);

const displayStartValue = computed(() => {
	if (!props.modelValueStart) return '';
	// Ensure format is YYYY-MM-DD for date input
	return props.modelValueStart.split('T')[0];
});

const displayEndValue = computed(() => {
	if (!props.modelValueEnd) return '';
	// Ensure format is YYYY-MM-DD for date input
	return props.modelValueEnd.split('T')[0];
});

const inputClass = computed(() => {
	const base = 'block w-full rounded-md border bg-white px-3 py-2 text-sm shadow-sm placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-offset-0 disabled:cursor-not-allowed disabled:bg-gray-100 disabled:text-gray-500';
	const normal = 'border-gray-300 focus:ring-blue-500';
	const danger = 'border-rose-300 focus:ring-rose-500';
	return [base, props.error || validationError.value ? danger : normal].join(' ');
});

function handleStartChange(e: Event) {
	const target = e.target as HTMLInputElement;
	const value = target.value || null;
	emit('update:modelValueStart', value);
	validateRange(value, props.modelValueEnd);
}

function handleEndChange(e: Event) {
	const target = e.target as HTMLInputElement;
	const value = target.value || null;
	emit('update:modelValueEnd', value);
	validateRange(props.modelValueStart, value);
}

function validateRange(start: string | null | undefined, end: string | null | undefined) {
	validationError.value = null;

	if (start && end) {
		const startDate = new Date(start);
		const endDate = new Date(end);

		if (startDate > endDate) {
			validationError.value = 'Start date must be before end date';
		}
	}
}

// Watch for external changes and validate
watch(
	() => [props.modelValueStart, props.modelValueEnd],
	() => {
		validateRange(props.modelValueStart, props.modelValueEnd);
	},
	{ immediate: true }
);
</script>


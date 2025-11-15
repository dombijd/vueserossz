<template>
	<button
		:class="buttonClass"
		:type="nativeType"
		:disabled="isDisabled || isLoading"
		:aria-busy="isLoading ? 'true' : 'false'"
		:aria-disabled="isDisabled || isLoading ? 'true' : 'false'"
	>
		<span class="inline-flex items-center gap-2">
			<FontAwesomeIcon
				v-if="leftIcon && !isLoading"
				:icon="leftIcon"
				class="shrink-0"
				:class="iconSizeClass"
			/>
			<FontAwesomeIcon
				v-if="isLoading"
				:icon="['fas','spinner']"
				class="animate-spin shrink-0"
				:class="iconSizeClass"
			/>
			<span><slot /></span>
			<FontAwesomeIcon
				v-if="rightIcon && !isLoading"
				:icon="rightIcon"
				class="shrink-0"
				:class="iconSizeClass"
			/>
		</span>
	</button>
	<!-- ghost buttons may need a subtle outline for a11y on focus -->
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import type { IconDefinition, IconProp } from '@fortawesome/fontawesome-svg-core';

type ButtonVariant = 'primary' | 'secondary' | 'success' | 'danger' | 'ghost';
type ButtonSize = 'sm' | 'md' | 'lg';
type NativeType = 'button' | 'submit' | 'reset';

interface ButtonProps {
	variant?: ButtonVariant;
	size?: ButtonSize;
	disabled?: boolean;
	loading?: boolean;
	leftIcon?: IconProp | IconDefinition | [string, string] | string;
	rightIcon?: IconProp | IconDefinition | [string, string] | string;
	type?: NativeType;
}

const props = withDefaults(defineProps<ButtonProps>(), {
	variant: 'primary',
	size: 'md',
	disabled: false,
	loading: false,
	type: 'button'
});

const isDisabled = computed(() => props.disabled);
const isLoading = computed(() => props.loading);
const nativeType = computed<NativeType>(() => props.type);

const sizeClass = computed(() => {
	switch (props.size) {
		case 'sm':
			return 'text-sm px-3 py-1.5';
		case 'lg':
			return 'text-base px-5 py-3';
		case 'md':
		default:
			return 'text-sm px-4 py-2';
	}
});

const iconSizeClass = computed(() => {
	switch (props.size) {
		case 'sm':
			return 'h-4 w-4';
		case 'lg':
			return 'h-5 w-5';
		case 'md':
		default:
			return 'h-4 w-4';
	}
});

const variantClass = computed(() => {
	// base interaction + disabled
	const base = 'inline-flex justify-center items-center rounded-md font-medium transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:opacity-60 disabled:cursor-not-allowed';
	const ring = 'focus-visible:ring-blue-500';

	switch (props.variant) {
		case 'secondary':
			return `${base} ${ring} bg-white text-gray-900 border border-gray-300 hover:bg-gray-50`;
		case 'success':
			return `${base} ${ring} bg-emerald-600 text-white hover:bg-emerald-700`;
		case 'danger':
			return `${base} ${ring} bg-rose-600 text-white hover:bg-rose-700`;
		case 'ghost':
			return `${base} ${ring} bg-transparent text-gray-700 hover:bg-gray-100`;
		case 'primary':
		default:
			return `${base} ${ring} bg-blue-600 text-white hover:bg-blue-700`;
	}
});

const buttonClass = computed(() => {
	return [variantClass.value, sizeClass.value].join(' ');
});
</script>



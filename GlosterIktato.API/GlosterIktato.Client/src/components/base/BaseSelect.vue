<template>
	<div class="w-full">
		<label v-if="label" :for="inputId" class="mb-1 block text-sm font-medium text-gray-700">
			{{ label }}
			<span v-if="required" class="text-rose-600">*</span>
		</label>
		<div class="relative">
			<button
				:id="inputId"
				type="button"
				:disabled="disabled"
				:class="triggerClass"
				@click="toggle"
				@keydown.down.prevent="openAndMove(1)"
				@keydown.up.prevent="openAndMove(-1)"
				@keydown.enter.prevent="selectHighlighted"
				@keydown.esc.prevent="close"
				:aria-expanded="open ? 'true' : 'false'"
				:aria-haspopup="'listbox'"
			>
				<span class="truncate text-left">
					{{ selectedLabel ?? placeholder }}
				</span>
				<svg class="h-4 w-4 shrink-0 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
				</svg>
			</button>
			<div
				v-if="open"
				ref="menuRef"
				class="absolute z-20 mt-1 w-full rounded-md border border-gray-200 bg-white shadow-lg"
				role="listbox"
				:aria-activedescendant="highlightedId"
			>
				<div class="p-2">
					<input
						ref="searchRef"
						type="text"
						v-model="query"
						placeholder="Search..."
						class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
						@keydown.down.prevent="move(1)"
						@keydown.up.prevent="move(-1)"
						@keydown.enter.prevent="selectHighlighted"
						@keydown.esc.prevent="close"
					/>
				</div>
				<ul class="max-h-56 overflow-auto py-1 text-sm">
					<li
						v-for="(opt, idx) in filteredOptions"
						:key="getKey(opt, idx)"
						:id="optionId(idx)"
						role="option"
						:aria-selected="isSelected(opt) ? 'true' : 'false'"
						:class="[
							'cursor-pointer px-3 py-2 flex items-center justify-between',
							idx === highlightedIndex ? 'bg-blue-50 text-blue-700' : 'text-gray-700',
							isSelected(opt) ? 'font-semibold' : 'font-normal'
						]"
						@click="onSelect(opt)"
						@mouseenter="highlightedIndex = idx"
					>
						<span class="truncate">{{ getLabel(opt) }}</span>
						<svg v-if="isSelected(opt)" class="h-4 w-4 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
						</svg>
					</li>
					<li v-if="filteredOptions.length === 0" class="px-3 py-2 text-gray-500">
						No results
					</li>
				</ul>
			</div>
		</div>
		<p
			v-if="error"
			class="mt-1 text-sm text-rose-600"
			role="alert"
		>
			{{ error }}
		</p>
	</div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';

type OptionValue = string | number | boolean | null | undefined;
interface SelectOption<TValue extends OptionValue = string> {
	label: string;
	value: TValue;
	disabled?: boolean;
}

interface BaseSelectProps<TValue extends OptionValue = string> {
	modelValue: TValue;
	options: Array<SelectOption<TValue>>;
	label?: string;
	placeholder?: string;
	id?: string;
	name?: string;
	disabled?: boolean;
	required?: boolean;
	error?: string;
	optionKeyField?: keyof SelectOption<TValue>;
}

const props = withDefaults(defineProps<BaseSelectProps>(), {
	placeholder: 'Select...',
	optionKeyField: 'value'
});

const emit = defineEmits<{
	(e: 'update:modelValue', value: OptionValue): void;
	(e: 'open'): void;
	(e: 'close'): void;
}>();

const open = ref(false);
const query = ref('');
const highlightedIndex = ref<number>(-1);
const inputId = computed(() => props.id ?? `sel-${Math.random().toString(36).slice(2)}`);
const highlightedId = computed(() => highlightedIndex.value >= 0 ? optionId(highlightedIndex.value) : undefined);

const menuRef = ref<HTMLElement | null>(null);
const searchRef = ref<HTMLInputElement | null>(null);

const triggerClass = computed(() => {
	const base = 'flex w-full items-center justify-between rounded-md border bg-white px-3 py-2 text-sm shadow-sm placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-offset-0 disabled:cursor-not-allowed disabled:bg-gray-100 disabled:text-gray-500';
	const normal = 'border-gray-300 focus:ring-blue-500';
	const danger = 'border-rose-300 focus:ring-rose-500';
	return [base, props.error ? danger : normal].join(' ');
});

const filteredOptions = computed(() => {
	const q = query.value.trim().toLowerCase();
	if (!q) return props.options;
	return props.options.filter(o => getLabel(o).toLowerCase().includes(q));
});

const selected = computed(() => props.options.find(o => isSelected(o)));
const selectedLabel = computed(() => selected.value ? getLabel(selected.value) : null);

function getLabel<TValue extends OptionValue>(opt: SelectOption<TValue>): string {
	return opt.label;
}
function isSelected<TValue extends OptionValue>(opt: SelectOption<TValue>): boolean {
	return opt.value === props.modelValue;
}
function getKey<TValue extends OptionValue>(opt: SelectOption<TValue>, idx: number): string | number {
	const field = props.optionKeyField as keyof SelectOption<TValue>;
	return (opt[field] as string | number | undefined) ?? idx;
}
function optionId(idx: number): string {
	return `${inputId.value}-opt-${idx}`;
}

function toggle() {
	open.value ? close() : openMenu();
}
function openMenu() {
	if (props.disabled) return;
	open.value = true;
	emit('open');
	// focus search
	queueMicrotask(() => {
		searchRef.value?.focus();
		searchRef.value?.select();
	});
	// set highlighted to selected index
	const idx = props.options.findIndex(o => isSelected(o));
	highlightedIndex.value = idx >= 0 ? idx : (filteredOptions.value.length ? 0 : -1);
}
function close() {
	open.value = false;
	emit('close');
	query.value = '';
	highlightedIndex.value = -1;
}
function move(delta: number) {
	if (!filteredOptions.value.length) return;
	const len = filteredOptions.value.length;
	let next = highlightedIndex.value + delta;
	if (next < 0) next = len - 1;
	if (next >= len) next = 0;
	highlightedIndex.value = next;
	// ensure visibility
	const el = document.getElementById(optionId(next));
	el?.scrollIntoView({ block: 'nearest' });
}
function openAndMove(delta: number) {
	if (!open.value) openMenu();
	move(delta);
}
function selectHighlighted() {
	if (highlightedIndex.value >= 0) {
		onSelect(filteredOptions.value[highlightedIndex.value]);
	}
}
function onSelect<TValue extends OptionValue>(opt: SelectOption<TValue>) {
	if (opt.disabled) return;
	emit('update:modelValue', opt.value);
	close();
}

function handleClickOutside(e: MouseEvent) {
	if (!open.value) return;
	const target = e.target as Node;
	const root = menuRef.value?.parentElement;
	if (root && !root.contains(target)) {
		close();
	}
}

onMounted(() => {
	document.addEventListener('mousedown', handleClickOutside);
});
onBeforeUnmount(() => {
	document.removeEventListener('mousedown', handleClickOutside);
});

watch(() => props.disabled, (val) => {
	if (val) close();
});
</script>



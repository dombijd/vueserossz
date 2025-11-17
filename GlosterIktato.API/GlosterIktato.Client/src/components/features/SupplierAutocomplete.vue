<template>
	<div class="relative">
		<input
			v-model="searchQuery"
			@input="handleSearch"
			@focus="showDropdown = true"
			@blur="handleBlur"
			:disabled="disabled"
			:class="inputClass"
			:placeholder="placeholder"
		/>
		<div
			v-if="showDropdown && (searchResults.length > 0 || showCreateForm)"
			class="absolute z-50 mt-1 w-full bg-white border border-gray-200 rounded-md shadow-lg max-h-60 overflow-auto"
		>
			<div v-if="loading" class="p-3 text-sm text-gray-500">Keresés...</div>
			<div v-else>
				<div
					v-for="supplier in searchResults"
					:key="supplier.id"
					@mousedown="selectSupplier(supplier)"
					class="px-3 py-2 hover:bg-gray-50 cursor-pointer"
				>
					<div class="font-medium text-gray-900">{{ supplier.name }}</div>
					<div class="text-sm text-gray-500">{{ supplier.taxNumber }}</div>
				</div>
				<div
					v-if="searchResults.length === 0 && searchQuery.length > 2"
					class="px-3 py-2 border-t border-gray-200"
				>
					<button
						@mousedown.prevent="showCreateForm = true"
						class="text-sm text-blue-600 hover:text-blue-800 font-medium"
					>
						+ Új szállító hozzáadása
					</button>
				</div>
			</div>
		</div>
		
		<!-- Inline Create Form -->
		<BaseModal v-model="showCreateForm" title="Új szállító hozzáadása" size="md">
			<div class="space-y-4">
				<BaseInput
					v-model="newSupplier.name"
					label="Név"
					placeholder="Szállító neve"
					required
					:error="createErrors.name"
				/>
				<BaseInput
					v-model="newSupplier.taxNumber"
					label="Adószám"
					placeholder="Adószám"
					required
					:error="createErrors.taxNumber"
				/>
				<BaseInput
					v-model="newSupplier.country"
					label="Ország"
					placeholder="Ország"
					required
					:error="createErrors.country"
				/>
			</div>
			<template #footer>
				<BaseButton variant="secondary" @click="showCreateForm = false">Mégse</BaseButton>
				<BaseButton variant="primary" @click="createSupplier" :loading="creating">
					Létrehozás
				</BaseButton>
			</template>
		</BaseModal>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useToast } from '@/composables/useToast';
import BaseInput from '@/components/base/BaseInput.vue';
import BaseButton from '@/components/base/BaseButton.vue';
import BaseModal from '@/components/base/BaseModal.vue';
import api from '@/services/api';

interface Supplier {
	id: number;
	name: string;
	taxNumber: string;
}

interface Props {
	modelValue: number | null;
	disabled?: boolean;
	placeholder?: string;
}

const props = withDefaults(defineProps<Props>(), {
	disabled: false,
	placeholder: 'Keresés szállítóra...'
});

const emit = defineEmits<{
	'update:modelValue': [value: number | null];
	'supplier-created': [supplier: Supplier];
}>();

const { success, error: toastError } = useToast();

const searchQuery = ref('');
const searchResults = ref<Supplier[]>([]);
const showDropdown = ref(false);
const loading = ref(false);
const showCreateForm = ref(false);
const creating = ref(false);

const newSupplier = ref({
	name: '',
	taxNumber: '',
	country: 'HU',
});

const createErrors = ref<Record<string, string>>({});

const inputClass = computed(() => {
	const base = 'block w-full rounded-md border bg-white px-3 py-2 text-sm shadow-sm placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-offset-0 disabled:cursor-not-allowed disabled:bg-gray-100 disabled:text-gray-500';
	const normal = 'border-gray-300 focus:ring-blue-500';
	return `${base} ${normal}`;
});

let searchTimeout: ReturnType<typeof setTimeout> | null = null;

async function handleSearch() {
	if (searchQuery.value.length < 2) {
		searchResults.value = [];
		return;
	}
	
	if (searchTimeout) {
		clearTimeout(searchTimeout);
	}
	
	searchTimeout = setTimeout(async () => {
		loading.value = true;
		try {
			const response = await api.get<Supplier[]>('/suppliers/search', {
				params: { q: searchQuery.value }
			});
			searchResults.value = response.data;
		} catch (err) {
			console.error('Error searching suppliers:', err);
			searchResults.value = [];
		} finally {
			loading.value = false;
		}
	}, 300);
}

function selectSupplier(supplier: Supplier) {
	emit('update:modelValue', supplier.id);
	searchQuery.value = supplier.name;
	showDropdown.value = false;
}

function handleBlur() {
	// Delay to allow click events to fire
	setTimeout(() => {
		showDropdown.value = false;
	}, 200);
}

async function createSupplier() {
	createErrors.value = {};
	
	if (!newSupplier.value.name) {
		createErrors.value.name = 'Név kötelező';
		return;
	}
	if (!newSupplier.value.taxNumber) {
		createErrors.value.taxNumber = 'Adószám kötelező';
		return;
	}
	
	creating.value = true;
	try {
		const response = await api.post<Supplier>('/suppliers', {
			name: newSupplier.value.name,
			taxNumber: newSupplier.value.taxNumber,
			country: newSupplier.value.country,
		});
		
		const supplier = response.data;
		emit('update:modelValue', supplier.id);
		emit('supplier-created', supplier);
		searchQuery.value = supplier.name;
		showCreateForm.value = false;
		newSupplier.value = { name: '', taxNumber: '', country: 'HU' };
		success('Szállító sikeresen létrehozva');
	} catch (err: any) {
		console.error('Error creating supplier:', err);
		toastError(err.response?.data?.message || 'Hiba történt a szállító létrehozása során');
	} finally {
		creating.value = false;
	}
}

// Load selected supplier name when modelValue changes
watch(() => props.modelValue, async (newValue) => {
	if (newValue && !searchQuery.value) {
		try {
			const response = await api.get<Supplier>(`/suppliers/${newValue}`);
			searchQuery.value = response.data.name;
		} catch (err) {
			console.error('Error loading supplier:', err);
		}
	}
}, { immediate: true });
</script>


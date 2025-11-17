<template>
	<AppLayout>
		<div class="space-y-6">
			<!-- Header -->
			<div class="flex items-center justify-between">
				<h1 class="text-2xl font-semibold text-gray-900">Szállítók</h1>
				<BaseButton
					variant="primary"
					:left-icon="['fas', 'plus']"
					@click="showCreateModal = true"
				>
					Új szállító
				</BaseButton>
			</div>

			<!-- Suppliers Table -->
			<BaseCard>
				<BaseTable
					:columns="columns"
					:data="suppliers"
					:loading="isLoading"
				>
					<template #cell-isActive="{ row }">
						<span
							:class="[
								'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium',
								row.isActive
									? 'bg-green-100 text-green-800'
									: 'bg-gray-100 text-gray-800'
							]"
						>
							{{ row.isActive ? 'Aktív' : 'Inaktív' }}
						</span>
					</template>
					<template #cell-actions="{ row }">
						<div class="flex gap-2">
							<BaseButton
								variant="ghost"
								size="sm"
								:left-icon="['fas', 'edit']"
								@click="editSupplier(row)"
							>
								Szerkesztés
							</BaseButton>
							<BaseButton
								variant="ghost"
								size="sm"
								:left-icon="['fas', row.isActive ? 'times' : 'check']"
								@click="toggleSupplierStatus(row)"
							>
								{{ row.isActive ? 'Inaktiválás' : 'Aktiválás' }}
							</BaseButton>
						</div>
					</template>
				</BaseTable>
			</BaseCard>
		</div>

		<!-- Create/Edit Supplier Modal -->
		<BaseModal
			v-model="showCreateModal"
			:title="editingSupplier ? 'Szállító szerkesztése' : 'Új szállító'"
			size="md"
		>
			<div class="space-y-4">
				<BaseInput
					v-model="supplierForm.name"
					label="Név"
					placeholder="Szállító neve"
					required
					:error="errors.name"
				/>
				<BaseInput
					v-model="supplierForm.taxNumber"
					label="Adószám"
					placeholder="12345678-1-23"
					:error="errors.taxNumber"
				/>
				<BaseInput
					v-model="supplierForm.address"
					label="Cím"
					placeholder="Cím"
				/>
				<BaseInput
					v-model="supplierForm.contactPerson"
					label="Kapcsolattartó"
					placeholder="Kapcsolattartó neve"
				/>
				<BaseInput
					v-model="supplierForm.email"
					type="email"
					label="Email"
					placeholder="email@example.com"
				/>
				<BaseInput
					v-model="supplierForm.phone"
					label="Telefon"
					placeholder="+36 30 123 4567"
				/>
			</div>
			<template #footer>
				<BaseButton variant="secondary" @click="cancelEdit">
					Mégse
				</BaseButton>
				<BaseButton
					variant="primary"
					@click="saveSupplier"
					:loading="saving"
				>
					Mentés
				</BaseButton>
			</template>
		</BaseModal>
	</AppLayout>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import AppLayout from '../layout/AppLayout.vue';
import BaseCard from '../base/BaseCard.vue';
import BaseTable, { type TableColumn } from '../base/BaseTable.vue';
import BaseButton from '../base/BaseButton.vue';
import BaseInput from '../base/BaseInput.vue';
import BaseModal from '../base/BaseModal.vue';
import { useToast } from '@/composables/useToast';
import api from '@/services/api';

interface SupplierDto {
	id: number;
	name: string;
	taxNumber?: string | null;
	address?: string | null;
	contactPerson?: string | null;
	email?: string | null;
	phone?: string | null;
	isActive: boolean;
}

const { success, error: toastError } = useToast();

// State
const suppliers = ref<SupplierDto[]>([]);
const isLoading = ref(false);
const saving = ref(false);
const showCreateModal = ref(false);
const editingSupplier = ref<SupplierDto | null>(null);

const supplierForm = ref({
	name: '',
	taxNumber: '',
	address: '',
	contactPerson: '',
	email: '',
	phone: '',
});

const errors = ref<Record<string, string>>({});

// Table columns
const columns: TableColumn[] = [
	{ key: 'name', label: 'Név' },
	{ key: 'taxNumber', label: 'Adószám' },
	{ key: 'contactPerson', label: 'Kapcsolattartó' },
	{ key: 'email', label: 'Email' },
	{ key: 'isActive', label: 'Státusz' },
	{ key: 'actions', label: 'Műveletek' },
];

// Methods
async function loadSuppliers() {
	isLoading.value = true;
	try {
		const response = await api.get<SupplierDto[]>('/suppliers');
		suppliers.value = response.data;
	} catch (err) {
		toastError('Hiba történt a szállítók betöltése során');
		console.error('Error loading suppliers:', err);
	} finally {
		isLoading.value = false;
	}
}

function editSupplier(supplier: SupplierDto) {
	editingSupplier.value = supplier;
	supplierForm.value = {
		name: supplier.name,
		taxNumber: supplier.taxNumber || '',
		address: supplier.address || '',
		contactPerson: supplier.contactPerson || '',
		email: supplier.email || '',
		phone: supplier.phone || '',
	};
	errors.value = {};
	showCreateModal.value = true;
}

function cancelEdit() {
	showCreateModal.value = false;
	editingSupplier.value = null;
	supplierForm.value = {
		name: '',
		taxNumber: '',
		address: '',
		contactPerson: '',
		email: '',
		phone: '',
	};
	errors.value = {};
}

async function saveSupplier() {
	// Validate
	errors.value = {};
	let isValid = true;

	if (!supplierForm.value.name.trim()) {
		errors.value.name = 'Név kötelező';
		isValid = false;
	}

	if (!isValid) {
		return;
	}

	saving.value = true;
	try {
		if (editingSupplier.value) {
			// Update
			const response = await api.put<SupplierDto>(
				`/suppliers/${editingSupplier.value.id}`,
				supplierForm.value
			);
			const index = suppliers.value.findIndex(s => s.id === editingSupplier.value!.id);
			if (index !== -1) {
				suppliers.value[index] = response.data;
			}
			success('Szállító sikeresen frissítve');
		} else {
			// Create
			const response = await api.post<SupplierDto>('/suppliers', supplierForm.value);
			suppliers.value.push(response.data);
			success('Szállító sikeresen létrehozva');
		}
		cancelEdit();
	} catch (err: any) {
		toastError(err.response?.data?.message || 'Hiba történt a mentés során');
		console.error('Error saving supplier:', err);
	} finally {
		saving.value = false;
	}
}

async function toggleSupplierStatus(supplier: SupplierDto) {
	try {
		const response = await api.put<SupplierDto>(`/suppliers/${supplier.id}`, {
			isActive: !supplier.isActive
		});
		const index = suppliers.value.findIndex(s => s.id === supplier.id);
		if (index !== -1) {
			suppliers.value[index] = response.data;
		}
		success(`Szállító ${response.data.isActive ? 'aktiválva' : 'inaktiválva'}`);
	} catch (err: any) {
		toastError(err.response?.data?.message || 'Hiba történt a státusz változtatása során');
		console.error('Error toggling supplier status:', err);
	}
}

// Lifecycle
onMounted(() => {
	loadSuppliers();
});
</script>


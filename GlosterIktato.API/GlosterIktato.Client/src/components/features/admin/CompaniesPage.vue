<template>
	<AppLayout>
		<div class="space-y-6">
			<!-- Header -->
			<div class="flex items-center justify-between">
				<h1 class="text-2xl font-semibold text-gray-900">Cégek</h1>
				<BaseButton
					v-if="canEdit"
					variant="primary"
					:left-icon="['fas', 'plus']"
					@click="showCreateModal = true"
				>
					Új cég
				</BaseButton>
			</div>

			<!-- Search -->
			<BaseCard>
				<BaseInput
					v-model="searchQuery"
					placeholder="Keresés név vagy adószám alapján..."
				/>
			</BaseCard>

			<!-- Companies Table -->
			<BaseCard>
				<BaseTable
					:columns="columns"
					:data="filteredCompanies"
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
						<div v-if="canEdit" class="flex gap-2">
							<BaseButton
								variant="ghost"
								size="sm"
								:left-icon="['fas', 'edit']"
								@click="editCompany(row)"
							>
								Szerkesztés
							</BaseButton>
							<BaseButton
								variant="ghost"
								size="sm"
								:left-icon="['fas', 'trash']"
								@click="confirmDelete(row)"
							>
								Törlés
							</BaseButton>
						</div>
						<span v-else class="text-sm text-gray-400">-</span>
					</template>
				</BaseTable>
			</BaseCard>
		</div>

		<!-- Create/Edit Company Modal -->
		<BaseModal
			v-if="canEdit"
			v-model="showCreateModal"
			:title="editingCompany ? 'Cég szerkesztése' : 'Új cég'"
			size="md"
		>
			<div class="space-y-4">
				<BaseInput
					v-model="companyForm.name"
					label="Név"
					placeholder="Cég neve"
					required
					:error="errors.name"
				/>
				<BaseInput
					v-model="companyForm.taxNumber"
					label="Adószám"
					placeholder="12345678-1-23"
					required
					:error="errors.taxNumber"
				/>
				<BaseInput
					v-model="companyForm.address"
					label="Cím"
					placeholder="Cím"
					:error="errors.address"
				/>
			</div>
			<template #footer>
				<BaseButton variant="secondary" @click="cancelEdit">
					Mégse
				</BaseButton>
				<BaseButton
					variant="primary"
					@click="saveCompany"
					:loading="saving"
				>
					Mentés
				</BaseButton>
			</template>
		</BaseModal>

		<!-- Delete Confirmation Dialog -->
		<ConfirmDialog
			v-if="canEdit"
			v-model="showDeleteDialog"
			title="Cég törlése"
			message="Biztosan törölni szeretnéd ezt a céget?"
			confirm-text="Törlés"
			cancel-text="Mégse"
			variant="danger"
			:loading="deleting"
			@confirm="handleDelete"
			@cancel="cancelDelete"
		/>
	</AppLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import AppLayout from '../../layout/AppLayout.vue';
import BaseCard from '../../base/BaseCard.vue';
import BaseTable, { type TableColumn } from '../../base/BaseTable.vue';
import BaseButton from '../../base/BaseButton.vue';
import BaseInput from '../../base/BaseInput.vue';
import BaseModal from '../../base/BaseModal.vue';
import ConfirmDialog from '../../base/ConfirmDialog.vue';
import { useToast } from '@/composables/useToast';
import { useAuthStore } from '@/stores/authStore';
import api from '@/services/api';

interface CompanyDto {
	id: number;
	name: string;
	taxNumber: string;
	address?: string;
	isActive: boolean;
	createdAt: string;
	createdBy?: string;
	modifiedAt?: string;
	modifiedBy?: string;
}

interface CreateCompanyDto {
	name: string;
	taxNumber: string;
	address?: string;
}

interface UpdateCompanyDto {
	name?: string;
	taxNumber?: string;
	address?: string;
}

const { success, error: toastError } = useToast();
const auth = useAuthStore();

// State
const companies = ref<CompanyDto[]>([]);
const isLoading = ref(false);
const saving = ref(false);
const deleting = ref(false);
const showCreateModal = ref(false);
const showDeleteDialog = ref(false);
const editingCompany = ref<CompanyDto | null>(null);
const companyToDelete = ref<CompanyDto | null>(null);
const searchQuery = ref('');

const companyForm = ref<CreateCompanyDto>({
	name: '',
	taxNumber: '',
	address: ''
});

const errors = ref<Record<string, string>>({});

// Computed
const canEdit = computed(() => auth.isAdmin);

const filteredCompanies = computed(() => {
	if (!searchQuery.value.trim()) {
		return companies.value;
	}
	const query = searchQuery.value.toLowerCase().trim();
	return companies.value.filter(
		company =>
			company.name.toLowerCase().includes(query) ||
			company.taxNumber.toLowerCase().includes(query)
	);
});

// Table columns
const columns: TableColumn[] = [
	{ key: 'name', label: 'Név' },
	{ key: 'taxNumber', label: 'Adószám' },
	{ key: 'address', label: 'Cím' },
	{ key: 'isActive', label: 'Státusz' },
	{ key: 'actions', label: 'Műveletek' }
];

// Methods
async function loadCompanies() {
	isLoading.value = true;
	try {
		const response = await api.get<CompanyDto[]>('/companies');
		companies.value = response.data;
	} catch (err) {
		toastError('Hiba történt a cégek betöltése során');
		console.error('Error loading companies:', err);
	} finally {
		isLoading.value = false;
	}
}

function editCompany(company: CompanyDto) {
	editingCompany.value = company;
	companyForm.value = {
		name: company.name,
		taxNumber: company.taxNumber,
		address: company.address || ''
	};
	errors.value = {};
	showCreateModal.value = true;
}

function cancelEdit() {
	showCreateModal.value = false;
	editingCompany.value = null;
	companyForm.value = {
		name: '',
		taxNumber: '',
		address: ''
	};
	errors.value = {};
}

function validateForm(): boolean {
	errors.value = {};
	let isValid = true;

	if (!companyForm.value.name.trim()) {
		errors.value.name = 'Név kötelező';
		isValid = false;
	} else if (companyForm.value.name.length > 200) {
		errors.value.name = 'A név maximum 200 karakter lehet';
		isValid = false;
	}

	if (!companyForm.value.taxNumber.trim()) {
		errors.value.taxNumber = 'Adószám kötelező';
		isValid = false;
	} else if (companyForm.value.taxNumber.length > 50) {
		errors.value.taxNumber = 'Az adószám maximum 50 karakter lehet';
		isValid = false;
	}

	if (companyForm.value.address && companyForm.value.address.length > 500) {
		errors.value.address = 'A cím maximum 500 karakter lehet';
		isValid = false;
	}

	return isValid;
}

async function saveCompany() {
	if (!validateForm()) {
		return;
	}

	saving.value = true;
	try {
		if (editingCompany.value) {
			// Update
			const updateData: UpdateCompanyDto = {
				name: companyForm.value.name,
				taxNumber: companyForm.value.taxNumber,
				address: companyForm.value.address || undefined
			};
			const response = await api.put<CompanyDto>(
				`/companies/${editingCompany.value.id}`,
				updateData
			);
			const index = companies.value.findIndex(c => c.id === editingCompany.value!.id);
			if (index !== -1) {
				companies.value[index] = response.data;
			}
			success('Cég sikeresen frissítve');
		} else {
			// Create
			const response = await api.post<CompanyDto>('/companies', companyForm.value);
			companies.value.push(response.data);
			success('Cég sikeresen létrehozva');
		}
		cancelEdit();
	} catch (err: any) {
		toastError(err.response?.data?.message || 'Hiba történt a mentés során');
		console.error('Error saving company:', err);
	} finally {
		saving.value = false;
	}
}

function confirmDelete(company: CompanyDto) {
	companyToDelete.value = company;
	showDeleteDialog.value = true;
}

function cancelDelete() {
	showDeleteDialog.value = false;
	companyToDelete.value = null;
}

async function handleDelete() {
	if (!companyToDelete.value) return;

	deleting.value = true;
	try {
		// Backend NoContent-et ad vissza, szóval csak frissítjük a lokális állapotot
		await api.delete(`/companies/${companyToDelete.value.id}`);
		
		// Frissítjük a lokális állapotot - a cég inaktív lesz
		const index = companies.value.findIndex(c => c.id === companyToDelete.value!.id);
		if (index !== -1) {
			companies.value[index].isActive = false;
		}
		
		success('Cég sikeresen deaktiválva');
		cancelDelete();
	} catch (err: any) {
		toastError(err.response?.data?.message || 'Hiba történt a törlés során');
		console.error('Error deleting company:', err);
	} finally {
		deleting.value = false;
	}
}

// Lifecycle
onMounted(() => {
	loadCompanies();
});
</script>


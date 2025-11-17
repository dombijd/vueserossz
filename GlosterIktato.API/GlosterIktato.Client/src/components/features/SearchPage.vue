<template>
	<AppLayout>
		<div class="space-y-6">
			<!-- Header -->
			<div class="flex items-center justify-between">
				<h1 class="text-2xl font-semibold text-gray-900">Dokumentum keresés</h1>
			</div>

			<!-- Search Form Card -->
			<BaseCard title="Keresési feltételek">
				<div class="space-y-6">
					<!-- Alap szűrők -->
					<div>
						<h3 class="text-sm font-medium text-gray-900 mb-3">Alap szűrők</h3>
						<div class="grid grid-cols-1 md:grid-cols-3 gap-4">
							<BaseSelect
								v-model="searchForm.companyId"
								:options="companyOptions"
								label="Cég"
								placeholder="Összes cég"
							/>
							<BaseSelect
								v-model="searchForm.documentTypeId"
								:options="documentTypeOptions"
								label="Dokumentumtípus"
								placeholder="Összes típus"
							/>
							<BaseSelect
								v-model="searchForm.status"
								:options="statusOptions"
								label="Státusz"
								placeholder="Összes státusz"
								multiple
							/>
						</div>
					</div>

					<!-- Dátum szűrők -->
					<div>
						<h3 class="text-sm font-medium text-gray-900 mb-3">Dátum szűrők</h3>
						<div class="grid grid-cols-1 md:grid-cols-3 gap-4">
							<DateRangePicker
								v-model:start-date="searchForm.createdFrom"
								v-model:end-date="searchForm.createdTo"
								label="Létrehozva"
							/>
							<DateRangePicker
								v-model:start-date="searchForm.issueDateFrom"
								v-model:end-date="searchForm.issueDateTo"
								label="Kiállítás dátuma"
							/>
							<DateRangePicker
								v-model:start-date="searchForm.paymentDeadlineFrom"
								v-model:end-date="searchForm.paymentDeadlineTo"
								label="Fizetési határidő"
							/>
						</div>
					</div>

					<!-- Összeg szűrők -->
					<div>
						<h3 class="text-sm font-medium text-gray-900 mb-3">Összeg szűrők</h3>
						<div class="grid grid-cols-1 md:grid-cols-3 gap-4">
							<BaseInput
								v-model="searchForm.grossAmountMin"
								type="number"
								label="Bruttó összeg min"
								placeholder="0"
								step="0.01"
							/>
							<BaseInput
								v-model="searchForm.grossAmountMax"
								type="number"
								label="Bruttó összeg max"
								placeholder="0"
								step="0.01"
							/>
							<BaseSelect
								v-model="searchForm.currency"
								:options="CURRENCY_OPTIONS"
								label="Deviza"
								placeholder="Összes deviza"
							/>
						</div>
					</div>

					<!-- Szöveg keresés -->
					<div>
						<h3 class="text-sm font-medium text-gray-900 mb-3">Szöveg keresés</h3>
						<div class="grid grid-cols-1 md:grid-cols-2 gap-4">
							<BaseInput
								v-model="searchForm.archiveNumber"
								label="Iktatószám"
								placeholder="Keresés iktatószámban..."
							/>
							<BaseInput
								v-model="searchForm.invoiceNumber"
								label="Számlaszám"
								placeholder="Keresés számlaszámban..."
							/>
							<BaseInput
								v-model="searchForm.supplierName"
								label="Szállító"
								placeholder="Keresés szállító névben..."
							/>
							<BaseInput
								v-model="searchForm.comment"
								label="Megjegyzés"
								placeholder="Keresés megjegyzésben..."
							/>
						</div>
					</div>

					<!-- Hozzárendelt felhasználó -->
					<div>
						<BaseSelect
							v-model="searchForm.assignedToUserId"
							:options="userOptions"
							label="Hozzárendelt felhasználó"
							placeholder="Összes felhasználó"
							:loading="loadingUsers"
						/>
					</div>

					<!-- Action Buttons -->
					<div class="flex gap-3 justify-end pt-4 border-t border-gray-200">
						<BaseButton
							variant="secondary"
							@click="resetForm"
							:disabled="isSearching"
						>
							Törlés
						</BaseButton>
						<BaseButton
							variant="primary"
							@click="performSearch"
							:loading="isSearching"
							:left-icon="['fas', 'search']"
						>
							Keresés
						</BaseButton>
					</div>
				</div>
			</BaseCard>

			<!-- Search Results -->
			<BaseCard v-if="hasSearched" title="Találati lista">
				<!-- Bulk Actions -->
				<div v-if="selectedDocuments.length > 0" class="mb-4 flex gap-2">
					<BaseButton
						variant="secondary"
						size="sm"
						:left-icon="['fas', 'file-excel']"
						@click="exportToExcel"
						:loading="exporting"
					>
						Excel exportálás ({{ selectedDocuments.length }})
					</BaseButton>
					<BaseButton
						variant="secondary"
						size="sm"
						:left-icon="['fas', 'file-zipper']"
						@click="exportToPdfZip"
						:loading="exporting"
					>
						ZIP exportálás ({{ selectedDocuments.length }})
					</BaseButton>
				</div>

				<!-- Results Table -->
				<BaseTable
					:columns="resultsColumns"
					:data="searchResults"
					:loading="isSearching"
					:selectable="true"
					row-key="id"
					@select="handleSelect"
				>
					<!-- Custom cell templates -->
					<template #cell-archiveNumber="{ row }">
						<span class="font-mono text-sm">{{ row.archiveNumber }}</span>
					</template>
					<template #cell-status="{ row }">
						<StatusBadge :status="row.status" />
					</template>
					<template #cell-grossAmount="{ row }">
						<span v-if="row.grossAmount" class="text-sm">
							{{ formatCurrency(row.grossAmount, row.currency) }}
						</span>
						<span v-else class="text-sm text-gray-400">-</span>
					</template>
					<template #cell-createdAt="{ row }">
						<span class="text-sm text-gray-600">
							{{ formatDateTime(row.createdAt) }}
						</span>
					</template>
					<template #cell-actions="{ row }">
						<BaseButton
							variant="ghost"
							size="sm"
							:left-icon="['fas', 'eye']"
							@click="openDocument(row.id)"
						>
							Megnyit
						</BaseButton>
					</template>
				</BaseTable>

				<!-- Empty State -->
				<div v-if="!isSearching && searchResults.length === 0" class="py-12 text-center">
					<font-awesome-icon :icon="['fas', 'search']" class="text-4xl text-gray-400 mb-4" />
					<h3 class="text-lg font-medium text-gray-900 mb-2">Nincs találat</h3>
					<p class="text-sm text-gray-500">
						Próbáljon meg más keresési feltételeket.
					</p>
				</div>
			</BaseCard>
		</div>
	</AppLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import AppLayout from '../layout/AppLayout.vue';
import BaseCard from '../base/BaseCard.vue';
import BaseTable, { type TableColumn } from '../base/BaseTable.vue';
import BaseButton from '../base/BaseButton.vue';
import BaseInput from '../base/BaseInput.vue';
import BaseSelect from '../base/BaseSelect.vue';
import DateRangePicker from '../base/DateRangePicker.vue';
import StatusBadge from '../base/StatusBadge.vue';
import { useToast } from '@/composables/useToast';
import api from '@/services/api';
import type { DocumentResponseDto, PaginatedResult } from '@/types/document.types';
import { formatDateTime } from '@/utils/date.utils';
import { CURRENCY_OPTIONS } from '@/constants/app.constants';

const router = useRouter();
const { success, error: toastError } = useToast();

// State
const searchForm = ref({
	companyId: null as number | null,
	documentTypeId: null as number | null,
	status: [] as string[],
	createdFrom: null as string | null,
	createdTo: null as string | null,
	issueDateFrom: null as string | null,
	issueDateTo: null as string | null,
	paymentDeadlineFrom: null as string | null,
	paymentDeadlineTo: null as string | null,
	grossAmountMin: null as number | null,
	grossAmountMax: null as number | null,
	currency: null as string | null,
	archiveNumber: '',
	invoiceNumber: '',
	supplierName: '',
	comment: '',
	assignedToUserId: null as number | null,
});

const searchResults = ref<DocumentResponseDto[]>([]);
const searchPagination = ref<PaginatedResult<DocumentResponseDto> | null>(null);
const selectedDocuments = ref<DocumentResponseDto[]>([]);
const isSearching = ref(false);
const hasSearched = ref(false);
const exporting = ref(false);
const loadingUsers = ref(false);

// Options (load from API)
const companyOptions = ref<Array<{ label: string; value: number }>>([]);
const documentTypeOptions = ref<Array<{ label: string; value: number }>>([]);
const userOptions = ref<Array<{ label: string; value: number }>>([]);

const statusOptions = [
	{ label: 'Vázlat', value: 'Draft' },
	{ label: 'Jóváhagyásra vár', value: 'PendingApproval' },
	{ label: 'Emelt szintű jóváhagyás', value: 'ElevatedApproval' },
	{ label: 'Könyvelőnél', value: 'Accountant' },
	{ label: 'Kész', value: 'Done' },
	{ label: 'Elutasítva', value: 'Rejected' },
];

// Table columns
const resultsColumns: TableColumn[] = [
	{ key: 'archiveNumber', label: 'Iktatószám' },
	{ key: 'companyName', label: 'Cég' },
	{ key: 'supplierName', label: 'Szállító' },
	{ key: 'invoiceNumber', label: 'Számlaszám' },
	{ key: 'status', label: 'Státusz' },
	{ key: 'grossAmount', label: 'Összeg' },
	{ key: 'createdAt', label: 'Létrehozva' },
	{ key: 'actions', label: '' },
];

// Methods
async function performSearch() {
	isSearching.value = true;
	hasSearched.value = true;

	try {
		const params = buildSearchParams();
		const response = await api.get<PaginatedResult<DocumentResponseDto>>(
			'/documents/search',
			{ params }
		);

		searchResults.value = response.data.data || [];
		searchPagination.value = response.data;
	} catch (err) {
		toastError('Hiba történt a keresés során');
		console.error('Search error:', err);
		searchResults.value = [];
	} finally {
		isSearching.value = false;
	}
}

function buildSearchParams() {
	const params: any = { page: 1, pageSize: 20 };

	Object.entries(searchForm.value).forEach(([key, value]) => {
		if (value !== null && value !== '' && !(Array.isArray(value) && value.length === 0)) {
			params[key] = value;
		}
	});

	return params;
}

function resetForm() {
	searchForm.value = {
		companyId: null,
		documentTypeId: null,
		status: [],
		createdFrom: null,
		createdTo: null,
		issueDateFrom: null,
		issueDateTo: null,
		paymentDeadlineFrom: null,
		paymentDeadlineTo: null,
		grossAmountMin: null,
		grossAmountMax: null,
		currency: null,
		archiveNumber: '',
		invoiceNumber: '',
		supplierName: '',
		comment: '',
		assignedToUserId: null,
	};
	searchResults.value = [];
	selectedDocuments.value = [];
	hasSearched.value = false;
}

function handleSelect(selected: DocumentResponseDto[]) {
	selectedDocuments.value = selected;
}

function openDocument(documentId: number) {
	router.push(`/documents/${documentId}`);
}

function formatCurrency(amount: number, currency: string = 'HUF'): string {
	return new Intl.NumberFormat('hu-HU', {
		style: 'currency',
		currency: currency,
	}).format(amount);
}

async function exportToExcel() {
	if (selectedDocuments.value.length === 0) return;

	exporting.value = true;
	try {
		const documentIds = selectedDocuments.value.map(d => d.id);
		const response = await api.post(
			'/documents/export/excel',
			{ documentIds },
			{ responseType: 'blob' }
		);

		// Download file
		const blob = new Blob([response.data], {
			type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
		});
		const url = window.URL.createObjectURL(blob);
		const link = document.createElement('a');
		link.href = url;
		link.download = `documents_${new Date().toISOString().split('T')[0]}.xlsx`;
		link.click();
		window.URL.revokeObjectURL(url);

		success('Excel fájl sikeresen exportálva');
	} catch (err) {
		toastError('Hiba történt az exportálás során');
		console.error('Export error:', err);
	} finally {
		exporting.value = false;
	}
}

async function exportToPdfZip() {
	if (selectedDocuments.value.length === 0) return;

	exporting.value = true;
	try {
		const documentIds = selectedDocuments.value.map(d => d.id);
		const response = await api.post(
			'/documents/export/pdf-zip',
			{ documentIds },
			{ responseType: 'blob' }
		);

		// Download file
		const blob = new Blob([response.data], { type: 'application/zip' });
		const url = window.URL.createObjectURL(blob);
		const link = document.createElement('a');
		link.href = url;
		link.download = `documents_${new Date().toISOString().split('T')[0]}.zip`;
		link.click();
		window.URL.revokeObjectURL(url);

		success('ZIP fájl sikeresen exportálva');
	} catch (err) {
		toastError('Hiba történt az exportálás során');
		console.error('Export error:', err);
	} finally {
		exporting.value = false;
	}
}

// Load options on mount
onMounted(async () => {
	// TODO: Load companies, document types, users from API
	// For now, leaving empty arrays
});
</script>

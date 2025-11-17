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
							:placeholder="searchForm.companyId ? 'Összes felhasználó' : 'Először válasszon céget'"
							:loading="loadingUsers"
							:disabled="!searchForm.companyId"
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
				<!-- Export All Results -->
				<div v-if="searchResults.length > 0" class="mb-4 border-b border-gray-200 pb-4">
					<div class="flex gap-2 items-center justify-between mb-3">
						<div class="flex gap-2">
							<BaseButton
								variant="primary"
								size="sm"
								:left-icon="['fas', 'file-excel']"
								@click="exportAllToExcel"
								:loading="exporting"
							>
								Összes találat Excel exportálása ({{ searchResults.length }})
							</BaseButton>
							<BaseButton
								variant="primary"
								size="sm"
								:left-icon="['fas', 'file-zipper']"
								@click="exportAllToPdfZip"
								:loading="exporting"
							>
								Összes találat ZIP exportálása ({{ searchResults.length }})
							</BaseButton>
						</div>
						<div v-if="searchPagination" class="text-sm text-gray-600">
							Összesen: {{ searchPagination.totalCount }} találat
						</div>
					</div>
					<!-- Include Related Documents Checkbox for ZIP -->
					<div class="flex items-center gap-2">
						<input
							id="includeRelatedAll"
							type="checkbox"
							v-model="includeRelatedAll"
							class="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
						/>
						<label for="includeRelatedAll" class="text-sm text-gray-700 cursor-pointer">
							Kapcsolódó dokumentumok is exportálása (ZIP esetén)
						</label>
					</div>
				</div>

				<!-- Bulk Actions for Selected -->
				<div v-if="selectedDocuments.length > 0" class="mb-4">
					<div class="flex gap-2 mb-3">
						<BaseButton
							variant="secondary"
							size="sm"
							:left-icon="['fas', 'file-excel']"
							@click="exportSelectedToExcel"
							:loading="exporting"
						>
							Kiválasztottak Excel exportálása ({{ selectedDocuments.length }})
						</BaseButton>
						<BaseButton
							variant="secondary"
							size="sm"
							:left-icon="['fas', 'file-zipper']"
							@click="exportSelectedToPdfZip"
							:loading="exporting"
						>
							Kiválasztottak ZIP exportálása ({{ selectedDocuments.length }})
						</BaseButton>
					</div>
					<!-- Include Related Documents Checkbox for Selected ZIP -->
					<div class="flex items-center gap-2">
						<input
							id="includeRelatedSelected"
							type="checkbox"
							v-model="includeRelatedSelected"
							class="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
						/>
						<label for="includeRelatedSelected" class="text-sm text-gray-700 cursor-pointer">
							Kapcsolódó dokumentumok is exportálása (ZIP esetén)
						</label>
					</div>
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
import { ref, computed, onMounted, watch } from 'vue';
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
import { exportToExcel, exportToPdfZip } from '@/services/exportService';
import { getMyCompanies, getDocumentTypes, getUsersByCompany } from '@/services/dataService';
import { searchDocuments, type DocumentSearchParams } from '@/services/searchService';
import type { DocumentResponseDto, PaginatedResult } from '@/types/document.types';
import { formatDateTime } from '@/utils/date.utils';
import { formatCurrency } from '@/utils/currency.utils';
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
const includeRelatedAll = ref(false);
const includeRelatedSelected = ref(false);

// Options (load from API)
const companyOptions = ref<Array<{ label: string; value: number }>>([]);
const documentTypeOptions = ref<Array<{ label: string; value: number }>>([]);
const userOptions = ref<Array<{ label: string; value: number }>>([]);

// Státuszok (konstansok, nem adatbázisból)
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
		const searchParams: DocumentSearchParams = {
			...searchForm.value,
			page: 1,
			pageSize: 20
		};

		const result = await searchDocuments(searchParams);
		searchResults.value = result.data || [];
		searchPagination.value = result;
	} catch (err) {
		toastError('Hiba történt a keresés során');
		console.error('Search error:', err);
		searchResults.value = [];
	} finally {
		isSearching.value = false;
	}
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

async function exportAllToExcel() {
	if (searchResults.value.length === 0) return;

	exporting.value = true;
	try {
		const documentIds = searchResults.value.map(d => d.id);
		await exportToExcel({ documentIds });
		success(`Excel fájl sikeresen exportálva (${searchResults.value.length} dokumentum)`);
	} catch (err) {
		toastError('Hiba történt az exportálás során');
		console.error('Export error:', err);
	} finally {
		exporting.value = false;
	}
}

async function exportAllToPdfZip() {
	if (searchResults.value.length === 0) return;

	exporting.value = true;
	try {
		const documentIds = searchResults.value.map(d => d.id);
		await exportToPdfZip({
			documentIds,
			includeRelated: includeRelatedAll.value
		});
		success(`ZIP fájl sikeresen exportálva (${searchResults.value.length} dokumentum)`);
	} catch (err) {
		toastError('Hiba történt az exportálás során');
		console.error('Export error:', err);
	} finally {
		exporting.value = false;
	}
}

async function exportSelectedToExcel() {
	if (selectedDocuments.value.length === 0) return;

	exporting.value = true;
	try {
		const documentIds = selectedDocuments.value.map(d => d.id);
		await exportToExcel({ documentIds });
		success(`Excel fájl sikeresen exportálva (${selectedDocuments.value.length} dokumentum)`);
	} catch (err) {
		toastError('Hiba történt az exportálás során');
		console.error('Export error:', err);
	} finally {
		exporting.value = false;
	}
}

async function exportSelectedToPdfZip() {
	if (selectedDocuments.value.length === 0) return;

	exporting.value = true;
	try {
		const documentIds = selectedDocuments.value.map(d => d.id);
		await exportToPdfZip({
			documentIds,
			includeRelated: includeRelatedSelected.value
		});
		success(`ZIP fájl sikeresen exportálva (${selectedDocuments.value.length} dokumentum)`);
	} catch (err) {
		toastError('Hiba történt az exportálás során');
		console.error('Export error:', err);
	} finally {
		exporting.value = false;
	}
}

// Load data functions (component-specific logic only)
async function loadCompanies() {
	try {
		const companies = await getMyCompanies();
		companyOptions.value = companies.map(c => ({
			label: c.name,
			value: c.id
		}));
		
		// Ha csak egy cég van, automatikusan kiválasztjuk (UI logic)
		if (companyOptions.value.length === 1) {
			searchForm.value.companyId = companyOptions.value[0].value;
		}
	} catch (err) {
		console.error('Error loading companies:', err);
		toastError('Nem sikerült betölteni a cégeket');
	}
}

async function loadDocumentTypes() {
	try {
		const documentTypes = await getDocumentTypes();
		documentTypeOptions.value = documentTypes.map(dt => ({
			label: dt.name,
			value: dt.id
		}));
	} catch (err) {
		console.error('Error loading document types:', err);
		toastError('Nem sikerült betölteni a dokumentum típusokat');
	}
}

async function loadUsers(companyId: number) {
	if (!companyId) {
		userOptions.value = [];
		return;
	}

	loadingUsers.value = true;
	try {
		const users = await getUsersByCompany(companyId);
		userOptions.value = users.map(u => ({
			label: `${u.firstName} ${u.lastName} (${u.email})`,
			value: u.id
		}));
	} catch (err) {
		console.error('Error loading users:', err);
		toastError('Nem sikerült betölteni a felhasználókat');
	} finally {
		loadingUsers.value = false;
	}
}

// Watch company selection to load users
watch(() => searchForm.value.companyId, (newCompanyId) => {
	if (newCompanyId) {
		loadUsers(newCompanyId);
	} else {
		userOptions.value = [];
	}
});

// Load options on mount
onMounted(async () => {
	await Promise.all([
		loadCompanies(),
		loadDocumentTypes()
	]);
});
</script>

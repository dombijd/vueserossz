<template>
	<AppLayout>
		<div class="space-y-6">
			<!-- Header -->
			<div class="flex items-center justify-between">
				<h1 class="text-2xl font-semibold text-gray-900">Aktuális Ügyeim</h1>
				<BaseButton
					variant="primary"
					:left-icon="['fas', 'plus']"
					@click="router.push('/documents/upload')"
				>
					Új dokumentum
				</BaseButton>
			</div>

			<!-- Status Filter Tabs -->
			<BaseTabs
				v-model="statusFilter"
				:tabs="statusTabs"
			/>

			<!-- Table Card -->
			<BaseCard>
				<BaseTable
					:columns="columns"
					:data="documents || []"
					:loading="isLoading"
					:selectable="true"
					row-key="id"
					@select="handleSelect"
				>
					<!-- Checkbox column is handled by BaseTable -->
					
					<!-- Iktatószám -->
					<template #cell-archiveNumber="{ row }">
						<span class="font-mono text-sm">{{ row.archiveNumber || '-' }}</span>
					</template>

					<!-- Szállító -->
					<template #cell-supplierName="{ row }">
						<span class="text-sm">{{ row.supplierName || '-' }}</span>
					</template>

					<!-- Számlaszám -->
					<template #cell-invoiceNumber="{ row }">
						<span class="text-sm">{{ row.invoiceNumber || '-' }}</span>
					</template>

					<!-- Státusz -->
					<template #cell-status="{ row }">
						<StatusBadge :status="row.status" />
					</template>

					<!-- Létrehozva -->
					<template #cell-createdAt="{ row }">
						<span class="text-sm text-gray-600">
							{{ formatDateTime(row.createdAt) }}
						</span>
					</template>

					<!-- Műveletek -->
					<template #cell-actions="{ row }">
						<BaseButton
							variant="ghost"
							size="sm"
							:left-icon="['fas', 'eye']"
							@click.stop="handleOpen(row.id)"
						>
							Megnyit
						</BaseButton>
					</template>
				</BaseTable>

				<!-- Pagination -->
				<div v-if="pagination && pagination.totalPages > 1" class="mt-4 flex items-center justify-between border-t border-gray-200 px-4 py-3 sm:px-6">
					<div class="flex flex-1 justify-between sm:hidden">
						<BaseButton
							variant="ghost"
							size="sm"
							:disabled="!pagination || pagination.page <= 1"
							@click="goToPage((pagination?.page || 1) - 1)"
						>
							Előző
						</BaseButton>
						<BaseButton
							variant="ghost"
							size="sm"
							:disabled="!pagination || pagination.page >= pagination.totalPages"
							@click="goToPage((pagination?.page || 1) + 1)"
						>
							Következő
						</BaseButton>
					</div>
					<div class="hidden sm:flex sm:flex-1 sm:items-center sm:justify-between">
						<div>
							<p class="text-sm text-gray-700">
								Összesen
								<span class="font-medium">{{ pagination?.totalCount || 0 }}</span>
								dokumentum
								<span v-if="pagination && pagination.totalCount > 0">
									({{ ((pagination.page - 1) * pagination.pageSize + 1) }} - 
									{{ Math.min(pagination.page * pagination.pageSize, pagination.totalCount) }})
								</span>
							</p>
						</div>
						<div>
							<nav class="isolate inline-flex -space-x-px rounded-md shadow-sm" aria-label="Pagination">
								<BaseButton
									variant="ghost"
									size="sm"
									:disabled="!pagination || pagination.page <= 1"
									:left-icon="['fas', 'chevron-left']"
									@click="goToPage((pagination?.page || 1) - 1)"
									class="rounded-l-md"
								>
									Előző
								</BaseButton>
								<!-- Page numbers -->
								<template v-for="page in visiblePages" :key="page">
									<BaseButton
										v-if="page !== '...'"
										variant="ghost"
										size="sm"
										:class="[
											page === (pagination?.page || 1)
												? 'z-10 bg-blue-50 text-blue-600 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-600'
												: 'text-gray-900 hover:bg-gray-50'
										]"
										@click="goToPage(page as number)"
									>
										{{ page }}
									</BaseButton>
									<span
										v-else
										class="relative inline-flex items-center px-4 py-2 text-sm font-semibold text-gray-700"
									>
										...
									</span>
								</template>
								<BaseButton
									variant="ghost"
									size="sm"
									:disabled="!pagination || pagination.page >= pagination.totalPages"
									:right-icon="['fas', 'chevron-right']"
									@click="goToPage((pagination?.page || 1) + 1)"
									class="rounded-r-md"
								>
									Következő
								</BaseButton>
							</nav>
						</div>
					</div>
				</div>

				<!-- Empty state -->
				<div v-if="!isLoading && (!documents || documents.length === 0)" class="py-12 text-center">
					<p class="text-sm text-gray-500">Nincs hozzárendelt dokumentum.</p>
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
import BaseTabs, { type Tab } from '../base/BaseTabs.vue';
import StatusBadge from '../base/StatusBadge.vue';
import { useDocumentStore } from '../../stores/documentStore';
import type { DocumentResponseDto, PaginatedResult } from '../../types/document.types';
import { formatDateTime } from '@/utils/date.utils';
import { usePagination } from '@/composables/usePagination';
import api from '@/services/api';

const router = useRouter();
const documentStore = useDocumentStore(); // Still needed for isLoading

// State
const documents = ref<DocumentResponseDto[]>([]);
const pagination = ref<PaginatedResult<DocumentResponseDto> | null>(null);
const currentPage = ref(1);
const pageSize = ref(20);
const selectedDocuments = ref<DocumentResponseDto[]>([]);
const statusFilter = ref<string>('all');

// Tab definitions
const statusTabs: Tab[] = [
	{ value: 'all', label: 'Összes', icon: ['fas', 'list'] },
	{ value: 'Draft', label: 'Vázlat', icon: ['fas', 'file'] },
	{ value: 'PendingApproval', label: 'Jóváhagyásra vár', icon: ['fas', 'clock'] },
	{ value: 'Done', label: 'Kész', icon: ['fas', 'check-circle'] },
];

// Computed
const isLoading = computed(() => documentStore.isLoading);

// Table columns
const columns: TableColumn[] = [
	{ key: 'archiveNumber', label: 'Iktatószám' },
	{ key: 'supplierName', label: 'Szállító' },
	{ key: 'invoiceNumber', label: 'Számlaszám' },
	{ key: 'status', label: 'Státusz' },
	{ key: 'createdAt', label: 'Létrehozva' },
	{ key: 'actions', label: 'Műveletek' }
];

// Pagination composable
const { visiblePages } = usePagination(pagination);

// Methods
async function loadDocuments() {
	try {
		// Build params object
		const params: Record<string, any> = {
			page: currentPage.value,
			pageSize: pageSize.value
		};
		
		// Only add status parameter if not 'all'
		if (statusFilter.value !== 'all') {
			params.status = statusFilter.value;
		}
		
		// Always call API directly with consistent parameters
		const response = await api.get<PaginatedResult<DocumentResponseDto>>(
			'/documents/my-tasks',
			{ params }
		);
		
		const result = response.data;
		documents.value = result?.data || [];
		pagination.value = result;
	} catch (error) {
		console.error('Failed to load documents:', error);
		// Ensure documents is always an array even on error
		documents.value = [];
		pagination.value = null;
	}
}

function goToPage(page: number) {
	if (page < 1 || (pagination.value && page > pagination.value.totalPages)) {
		return;
	}
	currentPage.value = page;
	loadDocuments();
	// Scroll to top
	window.scrollTo({ top: 0, behavior: 'smooth' });
}

function handleSelect(selectedRows: DocumentResponseDto[]) {
	selectedDocuments.value = selectedRows;
}

function handleOpen(documentId: number) {
	router.push(`/documents/${documentId}`);
}

// Watch status filter changes
watch(statusFilter, () => {
	currentPage.value = 1; // Reset to first page
	loadDocuments();
});

// Lifecycle
onMounted(() => {
	loadDocuments();
});
</script>


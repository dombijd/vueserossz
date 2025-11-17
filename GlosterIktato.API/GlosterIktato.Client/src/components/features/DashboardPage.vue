<template>
	<AppLayout>
		<div class="space-y-6">
			<!-- Header -->
			<div class="flex items-center justify-between">
				<h1 class="text-2xl font-semibold text-gray-900">Dashboard</h1>
				<BaseButton
					variant="primary"
					:left-icon="['fas', 'plus']"
					@click="navigateToUpload"
				>
					Új dokumentum iktatása
				</BaseButton>
			</div>

			<!-- Statisztikák -->
			<div class="grid grid-cols-1 md:grid-cols-2 gap-4">
				<!-- Stat Card 1: Vázlat -->
				<BaseCard>
					<div class="p-4">
						<div class="flex items-center justify-between">
							<div>
								<p class="text-sm font-medium text-gray-600">Vázlat</p>
								<p class="text-2xl font-semibold text-gray-900">{{ stats.draft }}</p>
							</div>
							<div class="rounded-full bg-gray-100 p-3">
								<font-awesome-icon :icon="['fas', 'file']" class="h-6 w-6 text-gray-600" />
							</div>
						</div>
					</div>
				</BaseCard>

				<!-- Stat Card 2: Jóváhagyásra vár -->
				<BaseCard>
					<div class="p-4">
						<div class="flex items-center justify-between">
							<div>
								<p class="text-sm font-medium text-gray-600">Jóváhagyásra vár</p>
								<p class="text-2xl font-semibold text-gray-900">{{ stats.pending }}</p>
							</div>
							<div class="rounded-full bg-yellow-100 p-3">
								<font-awesome-icon :icon="['fas', 'clock']" class="h-6 w-6 text-yellow-600" />
							</div>
						</div>
					</div>
				</BaseCard>

				<!-- Stat Card 3: Kész -->
				<!-- <BaseCard>
					<div class="p-4">
						<div class="flex items-center justify-between">
							<div>
								<p class="text-sm font-medium text-gray-600">Kész</p>
								<p class="text-2xl font-semibold text-gray-900">{{ stats.done }}</p>
							</div>
							<div class="rounded-full bg-emerald-100 p-3">
								<font-awesome-icon :icon="['fas', 'check-circle']" class="h-6 w-6 text-emerald-600" />
							</div>
						</div>
					</div>
				</BaseCard> -->
			</div>

			<!-- Aktuális Ügyeim Widget -->
			<BaseCard title="Aktuális Ügyeim">
				<!-- Empty state -->
				<div v-if="!isLoading && documents.length === 0" class="py-12 text-center">
					<font-awesome-icon :icon="['fas', 'file']" class="text-4xl text-gray-400 mb-4" />
					<h3 class="text-lg font-medium text-gray-900 mb-2">Nincs dokumentum</h3>
					<p class="text-sm text-gray-500 mb-4">
						Kezdj el dolgozni az első dokumentum feltöltésével.
					</p>
					<BaseButton
						variant="primary"
						:left-icon="['fas', 'plus']"
						@click="navigateToUpload"
					>
						Új dokumentum iktatása
					</BaseButton>
				</div>

				<!-- Table -->
				<template v-else>
					<BaseTable
						:columns="columns"
						:data="documents"
						:loading="isLoading"
						row-key="id"
					>
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
								@click.stop="openDocument(row.id)"
							>
								Megnyit
							</BaseButton>
						</template>
					</BaseTable>

					<!-- Összes megtekintése link -->
					<div class="mt-4 flex justify-end border-t border-gray-200 pt-4">
						<BaseButton
							variant="ghost"
							:right-icon="['fas', 'arrow-right']"
							@click="viewAllDocuments"
						>
							Összes megtekintése
						</BaseButton>
					</div>
				</template>
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
import StatusBadge from '../base/StatusBadge.vue';
import { useDocumentStore } from '@/stores/documentStore';
import type { DocumentResponseDto } from '@/types/document.types';
import { formatDateTime } from '@/utils/date.utils';

const router = useRouter();
const documentStore = useDocumentStore();

// State
const documents = ref<DocumentResponseDto[]>([]);

// Computed
const isLoading = computed(() => documentStore.isLoading);

// Table columns
const columns: TableColumn[] = [
	{ key: 'archiveNumber', label: 'Iktatószám' },
	{ key: 'supplierName', label: 'Szállító' },
	{ key: 'invoiceNumber', label: 'Számlaszám' },
	{ key: 'status', label: 'Státusz' },
	{ key: 'createdAt', label: 'Létrehozva' },
	{ key: 'actions', label: '' }
];

// Statistics
const stats = computed(() => {
	if (!documents.value) return { draft: 0, pending: 0, done: 0 };

	return {
		draft: documents.value.filter(d => d.status === 'Draft' || d.status === 'Vázlat').length,
		pending: documents.value.filter(d =>
			d.status === 'PendingApproval' ||
			d.status === 'Jóváhagyásra vár' ||
			d.status === 'ElevatedApproval' ||
			d.status === 'Emelt szintű jóváhagyásra vár'
		).length,
		done: documents.value.filter(d => d.status === 'Done' || d.status === 'Kész' || d.status === 'Befejezett').length,
	};
});

// Methods
async function loadDocuments() {
	try {
		const result = await documentStore.fetchMyTasks(1, 5); // Első 5 dokumentum
		documents.value = result?.data || [];
	} catch (error) {
		console.error('Failed to load documents:', error);
		documents.value = [];
	}
}

function navigateToUpload() {
	router.push('/documents/upload');
}

function viewAllDocuments() {
	router.push('/documents');
}

function openDocument(documentId: number) {
	router.push(`/documents/${documentId}`);
}

// Lifecycle
onMounted(() => {
	loadDocuments();
});
</script>


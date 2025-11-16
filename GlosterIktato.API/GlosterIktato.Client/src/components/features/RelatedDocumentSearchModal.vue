<template>
	<BaseModal 
		:model-value="modelValue" 
		@update:model-value="$emit('update:modelValue', $event)"
		title="Dokumentum hozzárendelése" 
		size="lg"
	>
		<div class="space-y-4">
			<!-- Search Form -->
			<div class="grid grid-cols-1 md:grid-cols-2 gap-4">
				<BaseSelect
					v-model="searchFilters.documentTypeId"
					:options="documentTypeOptions"
					label="Dokumentum típus"
					placeholder="Összes típus"
				/>
				<BaseInput
					v-model="searchFilters.archiveNumber"
					label="Iktatószám"
					placeholder="Iktatószám"
				/>
				<BaseInput
					v-model="searchFilters.supplierName"
					label="Szállító"
					placeholder="Szállító neve"
				/>
				<div class="grid grid-cols-2 gap-2">
					<BaseInput
						v-model="searchFilters.dateFrom"
						type="date"
						label="Dátumtól"
					/>
					<BaseInput
						v-model="searchFilters.dateTo"
						type="date"
						label="Dátumig"
					/>
				</div>
			</div>
			
			<div class="flex justify-end">
				<BaseButton variant="primary" @click="searchDocuments" :loading="searching">
					Keresés
				</BaseButton>
			</div>

			<!-- Results -->
			<div v-if="searchResults.length > 0" class="border-t border-gray-200 pt-4">
				<h3 class="text-sm font-semibold text-gray-900 mb-3">Találatok</h3>
				<div class="space-y-2 max-h-96 overflow-y-auto">
					<div
						v-for="doc in searchResults"
						:key="doc.id"
						@click="selectDocument(doc.id)"
						class="p-3 border border-gray-200 rounded-md hover:bg-gray-50 cursor-pointer"
					>
						<div class="flex items-center justify-between">
							<div>
								<div class="font-medium text-gray-900">{{ doc.archiveNumber }}</div>
								<div class="text-sm text-gray-600">{{ doc.documentTypeName }}</div>
								<div class="text-sm text-gray-500">{{ doc.supplierName || 'Nincs szállító' }}</div>
								<div class="text-xs text-gray-400">{{ formatDate(doc.createdAt) }}</div>
							</div>
							<BaseButton variant="primary" size="sm">Kiválasztás</BaseButton>
						</div>
					</div>
				</div>
			</div>
			
			<div v-else-if="searched && !searching" class="text-center py-8 text-gray-500">
				<p>Nincs találat</p>
			</div>
		</div>
	</BaseModal>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useToast } from '@/composables/useToast';
import BaseModal from '@/components/base/BaseModal.vue';
import BaseButton from '@/components/base/BaseButton.vue';
import BaseInput from '@/components/base/BaseInput.vue';
import BaseSelect from '@/components/base/BaseSelect.vue';
import api from '@/services/api';

interface DocumentSearchResult {
	id: number;
	archiveNumber: string;
	documentTypeName: string;
	supplierName?: string | null;
	createdAt: string;
}

interface DocumentType {
	id: number;
	name: string;
	code: string;
}

interface Props {
	modelValue: boolean;
	currentDocumentId: number;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update:modelValue': [value: boolean];
	confirm: [documentId: number];
}>();

const { error: toastError } = useToast();

const searchFilters = ref({
	documentTypeId: null as number | null,
	archiveNumber: '',
	supplierName: '',
	dateFrom: null as string | null,
	dateTo: null as string | null,
});

const searchResults = ref<DocumentSearchResult[]>([]);
const searching = ref(false);
const searched = ref(false);
const documentTypes = ref<DocumentType[]>([]);

const documentTypeOptions = computed(() => {
	return [
		{ label: 'Összes típus', value: null },
		...documentTypes.value.map(dt => ({
			label: dt.name,
			value: dt.id
		}))
	];
});

async function loadDocumentTypes() {
	try {
		const response = await api.get<DocumentType[]>('/documents/types');
		documentTypes.value = response.data;
	} catch (err) {
		console.error('Error loading document types:', err);
	}
}

async function searchDocuments() {
	searching.value = true;
	searched.value = true;
	try {
		const params: any = {
			page: 1,
			pageSize: 20,
		};
		
		if (searchFilters.value.documentTypeId) {
			params.documentTypeId = searchFilters.value.documentTypeId;
		}
		if (searchFilters.value.archiveNumber) {
			params.archiveNumber = searchFilters.value.archiveNumber;
		}
		if (searchFilters.value.supplierName) {
			params.supplierName = searchFilters.value.supplierName;
		}
		if (searchFilters.value.dateFrom) {
			params.createdFrom = searchFilters.value.dateFrom;
		}
		if (searchFilters.value.dateTo) {
			params.createdTo = searchFilters.value.dateTo;
		}
		
		const response = await api.get('/documents/search', { params });
		// Filter out current document
		searchResults.value = response.data.data.filter((d: DocumentSearchResult) => d.id !== props.currentDocumentId);
	} catch (err: any) {
		console.error('Error searching documents:', err);
		toastError(err.response?.data?.message || 'Hiba történt a keresés során');
		searchResults.value = [];
	} finally {
		searching.value = false;
	}
}

function formatDate(dateString: string): string {
	const date = new Date(dateString);
	return date.toLocaleDateString('hu-HU', {
		year: 'numeric',
		month: 'short',
		day: 'numeric',
	});
}

function selectDocument(documentId: number) {
	emit('confirm', documentId);
	emit('update:modelValue', false);
}

// Load document types on mount
onMounted(() => {
	loadDocumentTypes();
});
</script>


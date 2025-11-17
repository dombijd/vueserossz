<template>
	<AppLayout>
		<div class="max-w-3xl mx-auto">
			<h1 class="text-2xl font-semibold text-gray-900 mb-6">Dokumentum feltöltése</h1>

			<BaseCard>
				<form @submit.prevent="handleSubmit" class="space-y-6">
					<!-- Company Select -->
					<div v-if="companies.length > 1">
						<BaseSelect
							v-model="selectedCompanyId"
							:options="companyOptions"
							label="Cég"
							placeholder="Válassz céget"
							required
							:error="errors.companyId"
						/>
					</div>
					<div v-else-if="companies.length === 1" class="text-sm text-gray-600">
						<p class="font-medium text-gray-700 mb-1">Cég</p>
						<p>{{ companies[0].name }}</p>
					</div>

					<!-- Document Type Select -->
					<BaseSelect
						v-model="selectedDocumentTypeId"
						:options="documentTypeOptions"
						label="Dokumentum típus"
						placeholder="Válassz dokumentum típust"
						required
						:error="errors.documentTypeId"
					/>

					<!-- File Upload -->
					<div>
						<label class="mb-1 block text-sm font-medium text-gray-700">
							Fájl
							<span class="text-rose-600">*</span>
						</label>
						<FileUpload
							ref="fileUploadRef"
							:multiple="false"
							accept=".pdf,application/pdf"
							:max-size="50"
							@files-selected-with-metadata="handleFilesSelected"
						/>
						<p v-if="errors.files" class="mt-1 text-sm text-rose-600" role="alert">
							{{ errors.files }}
						</p>
					</div>

					<!-- Error Display -->
					<div v-if="submitError" class="p-4 bg-rose-50 border border-rose-200 rounded-md">
						<p class="text-sm text-rose-800">{{ submitError }}</p>
					</div>

					<!-- Actions -->
					<div class="flex gap-3 justify-end pt-4 border-t border-gray-200">
						<BaseButton
							type="button"
							variant="secondary"
							@click="handleCancel"
							:disabled="isUploading"
						>
							Mégse
						</BaseButton>
						<BaseButton
							type="submit"
							variant="primary"
							:loading="isUploading"
							:disabled="isUploading || !canSubmit"
						>
							Feltöltés
						</BaseButton>
					</div>
				</form>
			</BaseCard>
		</div>
	</AppLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/authStore';
import { useToast } from '@/composables/useToast';
import AppLayout from '@/components/layout/AppLayout.vue';
import BaseCard from '@/components/base/BaseCard.vue';
import BaseSelect from '@/components/base/BaseSelect.vue';
import BaseButton from '@/components/base/BaseButton.vue';
import FileUpload from '@/components/base/FileUpload.vue';
import api from '@/services/api';
import type { CompanyDto } from '@/types/auth.types';

interface DocumentType {
	id: number;
	name: string;
}

interface UploadError {
	companyId?: string;
	documentTypeId?: string;
	files?: string;
}

// Document types - will be loaded from API
const documentTypes = ref<DocumentType[]>([]);

const router = useRouter();
const authStore = useAuthStore();
const { success, error: toastError } = useToast();

// State
const companies = ref<CompanyDto[]>([]);
const selectedCompanyId = ref<number | null>(null);
const selectedDocumentTypeId = ref<number | null>(null);
const selectedFile = ref<File | null>(null);
const isUploading = ref(false);
const errors = ref<UploadError>({});
const submitError = ref<string | null>(null);
const fileUploadRef = ref<InstanceType<typeof FileUpload> | null>(null);

// Computed
const companyOptions = computed(() =>
	companies.value
		.filter(c => c.isActive !== false) // Szűrjük az aktív cégeket
		.map(c => ({
			label: c.name,
			value: c.id
		}))
);

const documentTypeOptions = computed(() =>
	documentTypes.value.map(dt => ({
		label: dt.name,
		value: dt.id
	}))
);

const canSubmit = computed(() => {
	return (
		selectedCompanyId.value !== null &&
		selectedDocumentTypeId.value !== null &&
		selectedFile.value !== null &&
		!isUploading.value
	);
});

// Methods
async function loadCompanies() {
	try {
		// First try to use user's companies
		const userCompanies = authStore.userCompanies;
		if (userCompanies.length > 0) {
			// Szűrjük az aktív cégeket (csak azok, ahol isActive !== false)
			// A backend már csak aktívakat küld, de biztonsági okokból szűrünk
			companies.value = userCompanies.filter(c => c.isActive !== false);
			// Auto-select if only one company
			if (companies.value.length === 1) {
				selectedCompanyId.value = companies.value[0].id;
			}
		} else {
			// Fallback to API
			const response = await api.get<CompanyDto[]>('/companies');
			// Szűrjük az aktív cégeket (csak azok, ahol isActive !== false)
			// A backend már csak aktívakat küld, de biztonsági okokból szűrünk
			companies.value = response.data.filter(c => c.isActive !== false);
			if (companies.value.length === 1) {
				selectedCompanyId.value = companies.value[0].id;
			}
		}
	} catch (err) {
		console.error('Failed to load companies:', err);
		toastError('Nem sikerült betölteni a cégeket');
	}
}

async function loadDocumentTypes() {
	try {
		const response = await api.get<DocumentType[]>('/documents/types');
		documentTypes.value = response.data;
	} catch (err) {
		console.error('Failed to load document types:', err);
		// Fallback to hardcoded values if API fails
		documentTypes.value = [
			{ id: 5, name: 'Számla' },
			{ id: 6, name: 'Teljesítésigazolás' },
			{ id: 7, name: 'Szerződés' },
			{ id: 8, name: 'Egyéb' }
		];
		toastError('Nem sikerült betölteni a dokumentum típusokat');
	}
}

function handleFilesSelected(filesWithMetadata: Array<{ file: File; id: string }>) {
	// Take only the first file
	if (filesWithMetadata.length === 0) {
		selectedFile.value = null;
		return;
	}

	const file = filesWithMetadata[0].file;
	
	// Validate PDF file
	if (file.type !== 'application/pdf' && !file.name.toLowerCase().endsWith('.pdf')) {
		errors.value.files = 'Csak PDF fájlok tölthetők fel';
		selectedFile.value = null;
		return;
	}
	
	selectedFile.value = file;
	errors.value.files = undefined;
	submitError.value = null;
}

function validateForm(): boolean {
	errors.value = {};
	let isValid = true;

	if (!selectedCompanyId.value) {
		errors.value.companyId = 'Válassz céget';
		isValid = false;
	}

	if (!selectedDocumentTypeId.value) {
		errors.value.documentTypeId = 'Válassz dokumentum típust';
		isValid = false;
	}

	if (!selectedFile.value) {
		errors.value.files = 'Válassz egy fájlt';
		isValid = false;
	} else {
		// Validate file is PDF
		const file = selectedFile.value;
		if (file.type !== 'application/pdf' && !file.name.toLowerCase().endsWith('.pdf')) {
			errors.value.files = 'Csak PDF fájlok tölthetők fel';
			isValid = false;
		}
	}

	return isValid;
}

async function uploadFile(): Promise<void> {
	if (!selectedCompanyId.value || !selectedDocumentTypeId.value || !selectedFile.value) {
		throw new Error('Company, document type, or file not selected');
	}

	const formData = new FormData();
	formData.append('File', selectedFile.value);
	formData.append('CompanyId', selectedCompanyId.value.toString());
	formData.append('DocumentTypeId', selectedDocumentTypeId.value.toString());

	// Upload with progress tracking
	return new Promise((resolve, reject) => {
		api.post('/documents/upload', formData, {
			onUploadProgress: (progressEvent: any) => {
				if (progressEvent.total && fileUploadRef.value) {
					const progress = Math.round((progressEvent.loaded * 100) / progressEvent.total);
					// Update progress if FileUpload component supports it
					if (fileUploadRef.value.updateProgress) {
						fileUploadRef.value.updateProgress('single-file', progress);
					}
				}
			}
		})
			.then(() => {
				if (fileUploadRef.value?.updateProgress) {
					fileUploadRef.value.updateProgress('single-file', 100);
				}
				resolve();
			})
			.catch((err) => {
				const errorMessage = err.response?.data?.message || 'Feltöltési hiba';
				if (fileUploadRef.value?.setError) {
					fileUploadRef.value.setError('single-file', errorMessage);
				}
				reject(err);
			});
	});
}

async function handleSubmit() {
	if (!validateForm()) {
		return;
	}

	isUploading.value = true;
	submitError.value = null;

	try {
		// Upload single file
		await uploadFile();

		// Success
		success('Dokumentum sikeresen feltöltve');
		
		// Redirect after a short delay
		setTimeout(() => {
			router.push('/documents');
		}, 1000);
	} catch (err: any) {
		console.error('Upload error:', err);
		const errorMessage = err.response?.data?.message || 'Hiba történt a feltöltés során';
		submitError.value = errorMessage;
		toastError(errorMessage);
	} finally {
		isUploading.value = false;
	}
}

function handleCancel() {
	router.push('/documents');
}

// Lifecycle
onMounted(() => {
	loadCompanies();
	loadDocumentTypes();
});
</script>


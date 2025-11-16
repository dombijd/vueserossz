<template>
	<AppLayout>
		<div v-if="loading" class="flex items-center justify-center min-h-[400px]">
			<div class="text-center">
				<div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
				<p class="mt-4 text-gray-600">Dokumentum betöltése...</p>
			</div>
		</div>

		<div v-else-if="error" class="max-w-3xl mx-auto">
			<BaseCard>
				<div class="text-center py-8">
					<font-awesome-icon icon="exclamation-triangle" class="text-4xl text-red-500 mb-4" />
					<h2 class="text-xl font-semibold text-gray-900 mb-2">Hiba történt</h2>
					<p class="text-gray-600 mb-4">{{ error }}</p>
					<BaseButton variant="primary" @click="loadDocument">Újrapróbálás</BaseButton>
				</div>
			</BaseCard>
		</div>

		<div v-else-if="document" class="space-y-6">
			<!-- Header -->
			<div class="flex items-center justify-between">
				<div>
					<h1 class="text-2xl font-semibold text-gray-900">Dokumentum karton</h1>
					<p class="text-sm text-gray-500 mt-1">Iktatószám: {{ document.archiveNumber }}</p>
				</div>
				<div class="flex gap-2">
					<BaseButton
						v-if="canEdit && !isEditMode"
						variant="primary"
						@click="enterEditMode"
						:left-icon="['fas', 'edit']"
					>
						Szerkesztés
					</BaseButton>
					<BaseButton
						v-if="canEdit && isEditMode"
						variant="success"
						@click="saveDocument"
						:loading="saving"
						:left-icon="['fas', 'save']"
					>
						Mentés
					</BaseButton>
					<BaseButton
						v-if="isEditMode"
						variant="secondary"
						@click="cancelEdit"
						:disabled="saving"
					>
						Mégse
					</BaseButton>
				</div>
			</div>

			<!-- Main Content: 2-column grid -->
			<div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
				<!-- Left Column: Dynamic Data Form -->
				<div class="space-y-6">
					<BaseCard title="Dokumentum adatai">
						<div class="space-y-4">
							<!-- Common Fields -->
							<div>
								<label class="block text-sm font-medium text-gray-700 mb-1">Cég</label>
								<p class="text-gray-900 font-semibold">{{ document.companyName }}</p>
							</div>

							<div>
								<label class="block text-sm font-medium text-gray-700 mb-1">Dokumentumtípus</label>
								<span
									:class="getDocumentTypeBadgeClass(document.documentTypeCode)"
									class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
								>
									{{ document.documentTypeName }}
								</span>
							</div>

							<div>
								<label class="block text-sm font-medium text-gray-700 mb-1">Iktatószám</label>
								<div class="flex items-center gap-2">
									<p class="text-gray-900 font-semibold font-mono">{{ document.archiveNumber }}</p>
									<button
										@click="copyToClipboard(document.archiveNumber)"
										class="text-gray-400 hover:text-gray-600 transition-colors"
										title="Másolás vágólapra"
									>
										<font-awesome-icon icon="copy" class="text-sm" />
									</button>
								</div>
							</div>

							<div>
								<label class="block text-sm font-medium text-gray-700 mb-1">Létrehozva</label>
								<p class="text-gray-600">{{ formatDate(document.createdAt) }}</p>
							</div>

							<div>
								<label class="block text-sm font-medium text-gray-700 mb-1">Létrehozó felhasználó</label>
								<p class="text-gray-600">{{ document.createdByName }}</p>
							</div>

							<div>
								<label class="block text-sm font-medium text-gray-700 mb-1">Státusz</label>
								<StatusBadge :status="document.status" />
							</div>

							<div>
								<label class="block text-sm font-medium text-gray-700 mb-1">Hozzárendelt felhasználó</label>
								<BaseSelect
									v-if="isEditMode && canEdit"
									v-model="formData.assignedToUserId"
									:options="userOptions"
									placeholder="Válassz felhasználót"
									:loading="loadingUsers"
								/>
								<p v-else class="text-gray-600">{{ document.assignedToName || 'Nincs hozzárendelve' }}</p>
							</div>

							<!-- Type-specific fields -->
							<template v-if="document.documentTypeCode === 'SZLA'">
								<!-- Invoice specific fields -->
								<div class="border-t border-gray-200 pt-4 mt-4">
									<h3 class="text-sm font-semibold text-gray-900 mb-4">Számla adatok</h3>

									<div>
										<label class="block text-sm font-medium text-gray-700 mb-1">
											Szállító
											<span class="text-rose-600">*</span>
										</label>
										<SupplierAutocomplete
											v-model="formData.supplierId"
											:disabled="!isEditMode"
											@supplier-created="handleSupplierCreated"
										/>
									</div>

									<div>
										<BaseInput
											v-model="formData.invoiceNumber"
											label="Számlaszám"
											placeholder="Számlaszám"
											:required="true"
											:disabled="!isEditMode"
											:error="errors.invoiceNumber"
										/>
									</div>

									<div class="grid grid-cols-1 md:grid-cols-2 gap-4">
										<div>
											<BaseInput
												v-model="formData.issueDate"
												type="date"
												label="Kiállítás dátuma"
												:required="true"
												:disabled="!isEditMode"
												:error="errors.issueDate"
											/>
										</div>
										<div>
											<BaseInput
												v-model="formData.performanceDate"
												type="date"
												label="Teljesítés dátuma"
												:required="true"
												:disabled="!isEditMode"
												:error="errors.performanceDate"
											/>
										</div>
									</div>

									<div>
										<BaseInput
											v-model="formData.paymentDeadline"
											type="date"
											label="Fizetési határidő"
											:required="true"
											:disabled="!isEditMode"
											:error="errors.paymentDeadline"
										/>
									</div>

									<div class="grid grid-cols-1 md:grid-cols-2 gap-4">
										<div>
											<BaseInput
												v-model="formData.grossAmount"
												type="number"
												label="Bruttó összeg"
												placeholder="0.00"
												step="0.01"
												:required="true"
												:disabled="!isEditMode"
												:error="errors.grossAmount"
											/>
										</div>
										<div>
											<BaseSelect
												v-model="formData.currency"
												:options="currencyOptions"
												label="Deviza"
												:required="true"
												:disabled="!isEditMode"
												:error="errors.currency"
											/>
										</div>
									</div>

									<!-- BC Data Section -->
									<div class="border-t border-gray-200 pt-4 mt-4">
										<h3 class="text-sm font-semibold text-gray-900 mb-4">Business Central adatok</h3>
										<div class="grid grid-cols-1 md:grid-cols-2 gap-4">
											<div>
												<BaseSelect
													v-model="formData.costCenter"
													:options="bcCostCenterOptions"
													label="Kategória"
													placeholder="Válassz kategóriát"
													:disabled="!isEditMode"
													:loading="loadingBcData"
												/>
											</div>
											<div>
												<BaseSelect
													v-model="formData.gptCode"
													:options="bcGptCodeOptions"
													label="GPT"
													placeholder="Válassz GPT kódot"
													:disabled="!isEditMode"
													:loading="loadingBcData"
												/>
											</div>
											<div>
												<BaseSelect
													v-model="formData.businessUnit"
													:options="bcBusinessUnitOptions"
													label="Üzletág"
													placeholder="Válassz üzletágat"
													:disabled="!isEditMode"
													:loading="loadingBcData"
												/>
											</div>
											<div>
												<BaseSelect
													v-model="formData.project"
													:options="bcProjectOptions"
													label="Projekt"
													placeholder="Válassz projektet"
													:disabled="!isEditMode"
													:loading="loadingBcData"
												/>
											</div>
											<div>
												<BaseSelect
													v-model="formData.employee"
													:options="bcEmployeeOptions"
													label="Dolgozó"
													placeholder="Válassz dolgozót"
													:disabled="!isEditMode"
													:loading="loadingBcData"
												/>
											</div>
										</div>
									</div>

									<!-- NAV Data Section (Collapsible) -->
									<div class="border-t border-gray-200 pt-4 mt-4">
										<button
											@click="showNavData = !showNavData"
											class="flex items-center justify-between w-full text-left"
										>
											<h3 class="text-sm font-semibold text-gray-900">NAV adatok</h3>
											<font-awesome-icon
												:icon="showNavData ? 'chevron-up' : 'chevron-down'"
												class="text-gray-400"
											/>
										</button>
										<div v-if="showNavData" class="mt-4 space-y-4">
											<BaseButton
												variant="secondary"
												@click="fetchNavData"
												:loading="loadingNavData"
												:left-icon="['fas', 'download']"
											>
												Adatok lekérése NAV-tól
											</BaseButton>
											<div v-if="navDataStatus" class="p-3 rounded-md" :class="navDataStatusClass">
												<p class="text-sm font-medium">{{ navDataStatusMessage }}</p>
											</div>
											<div v-if="navData" class="space-y-2 text-sm">
												<!-- NAV data fields would be displayed here -->
												<p class="text-gray-600">NAV adatok betöltve</p>
											</div>
										</div>
									</div>
								</div>
							</template>

							<template v-else-if="document.documentTypeCode === 'TIG' || document.documentTypeCode === 'SZ'">
								<!-- TIG/Contract specific fields -->
								<div class="border-t border-gray-200 pt-4 mt-4">
									<h3 class="text-sm font-semibold text-gray-900 mb-4">
										{{ document.documentTypeCode === 'SZ' ? 'Szerződés' : 'TIG' }} adatok
									</h3>

									<div>
										<label class="block text-sm font-medium text-gray-700 mb-1">
											Partner/Szállító
										</label>
										<SupplierAutocomplete
											v-model="formData.supplierId"
											:disabled="!isEditMode"
											@supplier-created="handleSupplierCreated"
										/>
									</div>

									<div>
										<BaseInput
											v-model="formData.issueDate"
											type="date"
											label="Kiállítás dátuma"
											:disabled="!isEditMode"
										/>
									</div>

									<template v-if="document.documentTypeCode === 'SZ'">
										<div class="grid grid-cols-1 md:grid-cols-2 gap-4">
											<div>
												<BaseInput
													v-model="formData.validityStart"
													type="date"
													label="Érvényesség kezdete"
													:disabled="!isEditMode"
												/>
											</div>
											<div>
												<BaseInput
													v-model="formData.validityEnd"
													type="date"
													label="Érvényesség vége"
													:disabled="!isEditMode"
												/>
											</div>
										</div>
									</template>

									<!-- BC Data Section (smaller set) -->
									<div class="border-t border-gray-200 pt-4 mt-4">
										<h3 class="text-sm font-semibold text-gray-900 mb-4">Business Central adatok</h3>
										<div class="grid grid-cols-1 md:grid-cols-2 gap-4">
											<div>
												<BaseSelect
													v-model="formData.project"
													:options="bcProjectOptions"
													label="Projekt"
													placeholder="Válassz projektet"
													:disabled="!isEditMode"
													:loading="loadingBcData"
												/>
											</div>
											<div>
												<BaseSelect
													v-model="formData.employee"
													:options="bcEmployeeOptions"
													label="Dolgozó"
													placeholder="Válassz dolgozót"
													:disabled="!isEditMode"
													:loading="loadingBcData"
												/>
											</div>
										</div>
									</div>
								</div>
							</template>

							<template v-else>
								<!-- Other document type fields -->
								<div class="border-t border-gray-200 pt-4 mt-4">
									<h3 class="text-sm font-semibold text-gray-900 mb-4">Dokumentum adatok</h3>

									<div>
										<label class="block text-sm font-medium text-gray-700 mb-1">
											Dokumentum leírása
											<span class="text-rose-600">*</span>
										</label>
										<textarea
											v-model="formData.description"
											rows="4"
											class="block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 disabled:bg-gray-100 disabled:text-gray-500"
											:disabled="!isEditMode"
											:error="errors.description"
										></textarea>
										<p v-if="errors.description" class="mt-1 text-sm text-rose-600">{{ errors.description }}</p>
									</div>

									<div>
										<BaseInput
											v-model="formData.issueDate"
											type="date"
											label="Kiállítás dátuma"
											:disabled="!isEditMode"
										/>
									</div>

									<div>
										<label class="block text-sm font-medium text-gray-700 mb-1">Partner</label>
										<SupplierAutocomplete
											v-model="formData.supplierId"
											:disabled="!isEditMode"
											@supplier-created="handleSupplierCreated"
										/>
									</div>
								</div>
							</template>
						</div>
					</BaseCard>

					<!-- Action Buttons -->
					<BaseCard title="Műveletek" v-if="!isEditMode">
						<div class="space-y-2">
							<BaseButton
								v-if="canWorkflowAction"
								variant="primary"
								@click="handleWorkflowAction('forward')"
								:left-icon="['fas', 'arrow-right']"
								class="w-full"
							>
								Továbbküldés következő lépésre
							</BaseButton>
							<BaseButton
								v-if="canWorkflowAction"
								variant="secondary"
								@click="showRejectModal = true"
								:left-icon="['fas', 'arrow-left']"
								class="w-full"
							>
								Visszaküldés előző lépésre
							</BaseButton>
							<BaseButton
								v-if="canDelegate"
								variant="secondary"
								@click="showDelegateModal = true"
								:left-icon="['fas', 'user-plus']"
								class="w-full"
							>
								Dokumentum átadása másnak
							</BaseButton>
							<BaseButton
								v-if="canReject"
								variant="danger"
								@click="showRejectModal = true"
								:left-icon="['fas', 'times-circle']"
								class="w-full"
							>
								Elutasítás
							</BaseButton>
							<BaseButton
								v-if="canFinalize"
								variant="success"
								@click="handleFinalize"
								:left-icon="['fas', 'check-circle']"
								class="w-full"
							>
								Iktatás lezárása
							</BaseButton>
						</div>
					</BaseCard>
				</div>

				<!-- Right Column: PDF Viewer -->
				<div class="space-y-6">
					<BaseCard title="Dokumentum megjelenítő">
						<div class="space-y-4">
							<div class="flex items-center justify-between">
								<p class="text-sm text-gray-600">{{ document.originalFileName }}</p>
								<div class="flex gap-2">
									<BaseButton
										variant="secondary"
										size="sm"
										@click="zoomOut"
										:disabled="zoomLevel <= 0.5"
									>
										<font-awesome-icon icon="minus" />
									</BaseButton>
									<span class="text-sm text-gray-600 px-2">{{ Math.round(zoomLevel * 100) }}%</span>
									<BaseButton
										variant="secondary"
										size="sm"
										@click="zoomIn"
										:disabled="zoomLevel >= 2"
									>
										<font-awesome-icon icon="plus" />
									</BaseButton>
									<BaseButton
										variant="secondary"
										size="sm"
										@click="resetZoom"
									>
										<font-awesome-icon icon="expand" />
									</BaseButton>
								</div>
							</div>
							<div class="border border-gray-200 rounded-md overflow-hidden bg-gray-50" style="height: 600px;">
								<iframe
									:src="pdfUrl"
									class="w-full h-full"
									frameborder="0"
								></iframe>
							</div>
							<div class="flex gap-2 justify-end">
								<BaseButton
									variant="secondary"
									@click="downloadDocument"
									:left-icon="['fas', 'download']"
								>
									Letöltés
								</BaseButton>
								<BaseButton
									variant="secondary"
									@click="openFullscreen"
									:left-icon="['fas', 'expand']"
								>
									Teljes képernyő
								</BaseButton>
							</div>
						</div>
					</BaseCard>
				</div>
			</div>

			<!-- Tabs for Related Documents, Comments, History -->
			<div class="space-y-6">
				<div class="border-b border-gray-200">
					<nav class="-mb-px flex space-x-8">
						<button
							v-for="tab in tabs"
							:key="tab.id"
							@click="activeTab = tab.id"
							:class="[
								'whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm transition-colors',
								activeTab === tab.id
									? 'border-blue-500 text-blue-600'
									: 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
							]"
						>
							{{ tab.label }}
							<span
								v-if="tab.count !== undefined"
								:class="[
									'ml-2 py-0.5 px-2 rounded-full text-xs',
									activeTab === tab.id ? 'bg-blue-100 text-blue-600' : 'bg-gray-100 text-gray-600'
								]"
							>
								{{ tab.count }}
							</span>
						</button>
					</nav>
				</div>

				<!-- Related Documents Tab -->
				<div v-if="activeTab === 'related'" class="space-y-4">
					<div class="flex justify-between items-center">
						<h3 class="text-lg font-semibold text-gray-900">Kapcsolódó dokumentumok</h3>
						<BaseButton
							variant="primary"
							@click="showRelatedDocumentModal = true"
							:left-icon="['fas', 'plus']"
						>
							Dokumentum hozzárendelése
						</BaseButton>
					</div>
					<RelatedDocumentsList
						:document-id="documentId"
						:relations="relatedDocuments"
						@refresh="loadRelatedDocuments"
						@remove="handleRemoveRelation"
					/>
				</div>

				<!-- Comments Tab -->
				<div v-if="activeTab === 'comments'" class="space-y-4">
					<h3 class="text-lg font-semibold text-gray-900">Megjegyzések</h3>
					<CommentsSection
						:document-id="documentId"
						:comments="comments"
						@refresh="loadComments"
					/>
				</div>

				<!-- History Tab -->
				<div v-if="activeTab === 'history'" class="space-y-4">
					<div class="flex justify-between items-center">
						<h3 class="text-lg font-semibold text-gray-900">Életút / Historikus adatok</h3>
						<div class="flex gap-2">
							<BaseSelect
								v-model="historyFilter"
								:options="historyFilterOptions"
								placeholder="Szűrés művelettípus szerint"
								class="w-64"
							/>
							<BaseButton
								v-if="canExportHistory"
								variant="secondary"
								@click="exportHistory"
								:left-icon="['fas', 'download']"
							>
								Export
							</BaseButton>
						</div>
					</div>
					<HistoryTimeline
						:history="filteredHistory"
						:loading="loadingHistory"
					/>
				</div>
			</div>
		</div>

		<!-- Modals -->
		<RejectModal
			v-model="showRejectModal"
			@confirm="handleReject"
		/>
		<DelegateModal
			v-model="showDelegateModal"
			:users="users"
			@confirm="handleDelegate"
		/>
		<RelatedDocumentSearchModal
			v-model="showRelatedDocumentModal"
			:current-document-id="documentId"
			@confirm="handleAddRelatedDocument"
		/>
		<FullscreenPdfModal
			v-model="showFullscreenPdf"
			:pdf-url="pdfUrl"
		/>
	</AppLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/authStore';
import { useToast } from '@/composables/useToast';
import AppLayout from '@/components/layout/AppLayout.vue';
import BaseCard from '@/components/base/BaseCard.vue';
import BaseButton from '@/components/base/BaseButton.vue';
import BaseInput from '@/components/base/BaseInput.vue';
import BaseSelect from '@/components/base/BaseSelect.vue';
import BaseModal from '@/components/base/BaseModal.vue';
import StatusBadge from '@/components/base/StatusBadge.vue';
import api from '@/services/api';
import { getStatusColor, getStatusDisplayName } from '@/types/document.types';
import SupplierAutocomplete from './SupplierAutocomplete.vue';
import RelatedDocumentsList from './RelatedDocumentsList.vue';
import CommentsSection from './CommentsSection.vue';
import HistoryTimeline from './HistoryTimeline.vue';
import RejectModal from './RejectModal.vue';
import DelegateModal from './DelegateModal.vue';
import RelatedDocumentSearchModal from './RelatedDocumentSearchModal.vue';
import FullscreenPdfModal from './FullscreenPdfModal.vue';

// Types
interface DocumentDetailDto {
	id: number;
	archiveNumber: string;
	originalFileName: string;
	status: string;
	invoiceNumber?: string | null;
	issueDate?: string | null;
	performanceDate?: string | null;
	paymentDeadline?: string | null;
	grossAmount?: number | null;
	currency?: string | null;
	companyId: number;
	companyName: string;
	documentTypeId: number;
	documentTypeName: string;
	documentTypeCode: string;
	supplierId?: number | null;
	supplierName?: string | null;
	createdByUserId: number;
	createdByName: string;
	assignedToUserId?: number | null;
	assignedToName?: string | null;
	createdAt: string;
	modifiedAt?: string | null;
	storagePath: string;
	history: DocumentHistoryDto[];
}

interface DocumentHistoryDto {
	id: number;
	userId: number;
	userName: string;
	action: string;
	fieldName?: string | null;
	oldValue?: string | null;
	newValue?: string | null;
	comment?: string | null;
	createdAt: string;
}

interface UserDto {
	id: number;
	email: string;
	firstName: string;
	lastName: string;
}

interface BcDataOption {
	label: string;
	value: string;
}

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const { success, error: toastError } = useToast();

const documentId = computed(() => parseInt(route.params.id as string));

// State
const loading = ref(true);
const error = ref<string | null>(null);
const document = ref<DocumentDetailDto | null>(null);
const isEditMode = ref(false);
const saving = ref(false);
const showNavData = ref(false);
const loadingNavData = ref(false);
const navDataStatus = ref<'pending' | 'success' | 'error' | null>(null);
const navData = ref<any>(null);
const zoomLevel = ref(1);
const activeTab = ref<'related' | 'comments' | 'history'>('related');
const showRejectModal = ref(false);
const showDelegateModal = ref(false);
const showRelatedDocumentModal = ref(false);
const showFullscreenPdf = ref(false);

// Form data
const formData = ref({
	supplierId: null as number | null,
	invoiceNumber: null as string | null,
	issueDate: null as string | null,
	performanceDate: null as string | null,
	paymentDeadline: null as string | null,
	grossAmount: null as number | null,
	currency: null as string | null,
	costCenter: null as string | null,
	gptCode: null as string | null,
	businessUnit: null as string | null,
	project: null as string | null,
	employee: null as string | null,
	validityStart: null as string | null,
	validityEnd: null as string | null,
	description: null as string | null,
	assignedToUserId: null as number | null,
});

const errors = ref<Record<string, string>>({});

// Users
const users = ref<UserDto[]>([]);
const loadingUsers = ref(false);

// BC Data
const bcCostCenters = ref<BcDataOption[]>([]);
const bcGptCodes = ref<BcDataOption[]>([]);
const bcBusinessUnits = ref<BcDataOption[]>([]);
const bcProjects = ref<BcDataOption[]>([]);
const bcEmployees = ref<BcDataOption[]>([]);
const loadingBcData = ref(false);

// Related Documents
const relatedDocuments = ref<any[]>([]);
const loadingRelatedDocuments = ref(false);

// Comments
const comments = ref<any[]>([]);
const loadingComments = ref(false);

// History
const history = ref<DocumentHistoryDto[]>([]);
const loadingHistory = ref(false);
const historyFilter = ref<string | null>(null);

// State for PDF blob URL
const pdfBlobUrl = ref<string | null>(null);

// Computed
const pdfUrl = computed(() => {
	return pdfBlobUrl.value || '';
});

const canEdit = computed(() => {
	if (!document.value) return false;
	// Check permissions based on status and user roles
	const status = document.value.status;
	const userRoles = authStore.userRoles;
	
	// Draft documents can be edited by creator
	if (status === 'Draft' && document.value.createdByUserId === authStore.user?.id) {
		return true;
	}
	
	// Admin can always edit
	if (userRoles.includes('Admin')) {
		return true;
	}
	
	return false;
});

const canWorkflowAction = computed(() => {
	if (!document.value) return false;
	const status = document.value.status;
	return status === 'PendingApproval' || status === 'ElevatedApproval';
});

const canDelegate = computed(() => {
	if (!document.value) return false;
	return document.value.assignedToUserId === authStore.user?.id;
});

const canReject = computed(() => {
	if (!document.value) return false;
	const status = document.value.status;
	return status === 'PendingApproval' || status === 'ElevatedApproval';
});

const canFinalize = computed(() => {
	if (!document.value) return false;
	const status = document.value.status;
	return status === 'Accountant';
});

const canExportHistory = computed(() => {
	return authStore.userRoles.includes('Admin') || authStore.userRoles.includes('ElevatedApprover');
});

const userOptions = computed(() => {
	return users.value.map(u => ({
		label: `${u.firstName} ${u.lastName} (${u.email})`,
		value: u.id
	}));
});

const currencyOptions = [
	{ label: 'HUF', value: 'HUF' },
	{ label: 'EUR', value: 'EUR' },
	{ label: 'USD', value: 'USD' },
];

const bcCostCenterOptions = computed(() => 
	bcCostCenters.value.map(c => ({ label: `${c.value} - ${c.label}`, value: c.value }))
);
const bcGptCodeOptions = computed(() => 
	bcGptCodes.value.map(c => ({ label: `${c.value} - ${c.label}`, value: c.value }))
);
const bcBusinessUnitOptions = computed(() => 
	bcBusinessUnits.value.map(c => ({ label: `${c.value} - ${c.label}`, value: c.value }))
);
const bcProjectOptions = computed(() => 
	bcProjects.value.map(c => ({ label: `${c.value} - ${c.description || c.label}`, value: c.value }))
);
const bcEmployeeOptions = computed(() => 
	bcEmployees.value.map(c => ({ label: `${c.value} - ${c.label}`, value: c.value }))
);

const tabs = computed(() => [
	{ id: 'related', label: 'Kapcsolódó dokumentumok', count: relatedDocuments.value.length },
	{ id: 'comments', label: 'Megjegyzések', count: comments.value.length },
	{ id: 'history', label: 'Életút', count: history.value.length },
]);

const filteredHistory = computed(() => {
	if (!historyFilter.value) return history.value;
	return history.value.filter(h => h.action === historyFilter.value);
});

const historyFilterOptions = computed(() => {
	const actions = [...new Set(history.value.map(h => h.action))];
	return [
		{ label: 'Összes', value: null },
		...actions.map(a => ({ label: a, value: a }))
	];
});

const navDataStatusClass = computed(() => {
	switch (navDataStatus.value) {
		case 'pending': return 'bg-yellow-50 text-yellow-800';
		case 'success': return 'bg-green-50 text-green-800';
		case 'error': return 'bg-red-50 text-red-800';
		default: return '';
	}
});

const navDataStatusMessage = computed(() => {
	switch (navDataStatus.value) {
		case 'pending': return 'NAV lekérdezés folyamatban...';
		case 'success': return 'NAV adatok sikeresen lekérve';
		case 'error': return 'Hiba történt a NAV lekérdezés során';
		default: return '';
	}
});

// Methods
async function loadDocument() {
	loading.value = true;
	error.value = null;
	
	try {
		const response = await api.get<DocumentDetailDto>(`/documents/${documentId.value}`);
		document.value = response.data;
		
		// Initialize form data
		formData.value = {
			supplierId: document.value.supplierId ?? null,
			invoiceNumber: document.value.invoiceNumber ?? null,
			issueDate: document.value.issueDate ? formatDateForInput(document.value.issueDate) : null,
			performanceDate: document.value.performanceDate ? formatDateForInput(document.value.performanceDate) : null,
			paymentDeadline: document.value.paymentDeadline ? formatDateForInput(document.value.paymentDeadline) : null,
			grossAmount: document.value.grossAmount ?? null,
			currency: document.value.currency ?? null,
			costCenter: null, // These would come from document if stored
			gptCode: null,
			businessUnit: null,
			project: null,
			employee: null,
			validityStart: null,
			validityEnd: null,
			description: null,
			assignedToUserId: document.value.assignedToUserId ?? null,
		};
		
		// Load related data
		await Promise.all([
			loadUsers(),
			loadBcData(),
			loadRelatedDocuments(),
			loadComments(),
			loadHistory(),
			loadPdf(),
		]);
	} catch (err: any) {
		console.error('Error loading document:', err);
		error.value = err.response?.data?.message || 'Hiba történt a dokumentum betöltése során';
		toastError(error.value);
	} finally {
		loading.value = false;
	}
}

async function loadUsers() {
	if (loadingUsers.value) return;
	loadingUsers.value = true;
	try {
		const response = await api.get<UserDto[]>(`/users/company/${document.value?.companyId}`);
		users.value = response.data;
	} catch (err) {
		console.error('Error loading users:', err);
	} finally {
		loadingUsers.value = false;
	}
}

async function loadBcData() {
	if (!document.value || loadingBcData.value) return;
	loadingBcData.value = true;
	try {
		const [costCenters, gptCodes, businessUnits, projects, employees] = await Promise.all([
			api.get('/bc-data/cost-centers', { params: { companyId: document.value.companyId } }),
			api.get('/bc-data/gpt-codes', { params: { companyId: document.value.companyId } }),
			api.get('/bc-data/business-units', { params: { companyId: document.value.companyId } }),
			api.get('/bc-data/projects', { params: { companyId: document.value.companyId } }),
			api.get('/bc-data/employees', { params: { companyId: document.value.companyId } }),
		]);
		
		bcCostCenters.value = costCenters.data.map((c: any) => ({ label: c.name, value: c.code }));
		bcGptCodes.value = gptCodes.data.map((c: any) => ({ label: c.description, value: c.code }));
		bcBusinessUnits.value = businessUnits.data.map((c: any) => ({ label: c.name, value: c.code }));
		bcProjects.value = projects.data.map((c: any) => ({ label: c.description, value: c.code }));
		bcEmployees.value = employees.data.map((c: any) => ({ label: c.fullName, value: c.code }));
	} catch (err) {
		console.error('Error loading BC data:', err);
	} finally {
		loadingBcData.value = false;
	}
}

async function loadRelatedDocuments() {
	if (loadingRelatedDocuments.value) return;
	loadingRelatedDocuments.value = true;
	try {
		const response = await api.get(`/documents/${documentId.value}/relations`);
		relatedDocuments.value = response.data;
	} catch (err) {
		console.error('Error loading related documents:', err);
	} finally {
		loadingRelatedDocuments.value = false;
	}
}

async function loadComments() {
	if (loadingComments.value) return;
	loadingComments.value = true;
	try {
		const response = await api.get(`/documents/${documentId.value}/comments`);
		comments.value = response.data;
	} catch (err) {
		console.error('Error loading comments:', err);
	} finally {
		loadingComments.value = false;
	}
}

async function loadHistory() {
	if (loadingHistory.value) return;
	loadingHistory.value = true;
	try {
		const response = await api.get(`/documents/${documentId.value}/history`);
		history.value = response.data;
	} catch (err) {
		console.error('Error loading history:', err);
	} finally {
		loadingHistory.value = false;
	}
}

function enterEditMode() {
	isEditMode.value = true;
}

function cancelEdit() {
	isEditMode.value = false;
	// Reset form data
	if (document.value) {
		formData.value = {
			supplierId: document.value.supplierId ?? null,
			invoiceNumber: document.value.invoiceNumber ?? null,
			issueDate: document.value.issueDate ? formatDateForInput(document.value.issueDate) : null,
			performanceDate: document.value.performanceDate ? formatDateForInput(document.value.performanceDate) : null,
			paymentDeadline: document.value.paymentDeadline ? formatDateForInput(document.value.paymentDeadline) : null,
			grossAmount: document.value.grossAmount ?? null,
			currency: document.value.currency ?? null,
			costCenter: null,
			gptCode: null,
			businessUnit: null,
			project: null,
			employee: null,
			validityStart: null,
			validityEnd: null,
			description: null,
			assignedToUserId: document.value.assignedToUserId ?? null,
		};
	}
	errors.value = {};
}

async function saveDocument() {
	// Validate
	errors.value = {};
	let isValid = true;
	
	if (document.value?.documentTypeCode === 'SZLA') {
		if (!formData.value.invoiceNumber) {
			errors.value.invoiceNumber = 'Számlaszám kötelező';
			isValid = false;
		}
		if (!formData.value.issueDate) {
			errors.value.issueDate = 'Kiállítás dátuma kötelező';
			isValid = false;
		}
		if (!formData.value.performanceDate) {
			errors.value.performanceDate = 'Teljesítés dátuma kötelező';
			isValid = false;
		}
		if (!formData.value.paymentDeadline) {
			errors.value.paymentDeadline = 'Fizetési határidő kötelező';
			isValid = false;
		}
		if (!formData.value.grossAmount) {
			errors.value.grossAmount = 'Bruttó összeg kötelező';
			isValid = false;
		}
		if (!formData.value.currency) {
			errors.value.currency = 'Deviza kötelező';
			isValid = false;
		}
	}
	
	if (!isValid) {
		return;
	}
	
	saving.value = true;
	try {
		const updateDto: any = {
			supplierId: formData.value.supplierId,
			invoiceNumber: formData.value.invoiceNumber,
			issueDate: formData.value.issueDate,
			performanceDate: formData.value.performanceDate,
			paymentDeadline: formData.value.paymentDeadline,
			grossAmount: formData.value.grossAmount,
			currency: formData.value.currency,
			costCenter: formData.value.costCenter,
			gptCode: formData.value.gptCode,
			businessUnit: formData.value.businessUnit,
			project: formData.value.project,
			employee: formData.value.employee,
		};
		
		const response = await api.put(`/documents/${documentId.value}`, updateDto);
		document.value = response.data;
		isEditMode.value = false;
		success('Dokumentum sikeresen frissítve');
		await loadHistory(); // Refresh history
	} catch (err: any) {
		console.error('Error saving document:', err);
		toastError(err.response?.data?.message || 'Hiba történt a mentés során');
	} finally {
		saving.value = false;
	}
}

function getDocumentTypeBadgeClass(code: string): string {
	switch (code) {
		case 'SZLA': return 'bg-blue-100 text-blue-800';
		case 'TIG': return 'bg-green-100 text-green-800';
		case 'SZ': return 'bg-purple-100 text-purple-800';
		default: return 'bg-gray-100 text-gray-800';
	}
}

function formatDate(dateString: string): string {
	const date = new Date(dateString);
	return date.toLocaleDateString('hu-HU', {
		year: 'numeric',
		month: 'long',
		day: 'numeric',
		hour: '2-digit',
		minute: '2-digit',
	});
}

function formatDateForInput(dateString: string): string {
	const date = new Date(dateString);
	return date.toISOString().split('T')[0];
}

function copyToClipboard(text: string) {
	navigator.clipboard.writeText(text).then(() => {
		success('Másolva a vágólapra');
	}).catch(() => {
		toastError('Nem sikerült másolni');
	});
}

function zoomIn() {
	if (zoomLevel.value < 2) {
		zoomLevel.value += 0.1;
	}
}

function zoomOut() {
	if (zoomLevel.value > 0.5) {
		zoomLevel.value -= 0.1;
	}
}

function resetZoom() {
	zoomLevel.value = 1;
}

async function downloadDocument() {
	try {
		const baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5156/api';
		const response = await api.get(`${baseURL}/documents/${documentId.value}/download`, {
			responseType: 'blob',
		});
		
		// Create blob and download
		const blob = new Blob([response.data], { type: 'application/pdf' });
		const url = URL.createObjectURL(blob);
		const link = document.createElement('a');
		link.href = url;
		link.download = document.value?.originalFileName || 'document.pdf';
		document.body.appendChild(link);
		link.click();
		document.body.removeChild(link);
		URL.revokeObjectURL(url);
		success('Dokumentum letöltve');
	} catch (err) {
		console.error('Error downloading document:', err);
		toastError('Hiba történt a letöltés során');
	}
}

function openFullscreen() {
	showFullscreenPdf.value = true;
}

function handleSupplierCreated(supplier: any) {
	formData.value.supplierId = supplier.id;
	success('Szállító sikeresen létrehozva');
}

async function fetchNavData() {
	loadingNavData.value = true;
	navDataStatus.value = 'pending';
	try {
		// TODO: Implement NAV API call
		await new Promise(resolve => setTimeout(resolve, 2000)); // Simulate API call
		navDataStatus.value = 'success';
		navData.value = {}; // Placeholder
		success('NAV adatok sikeresen lekérve');
	} catch (err) {
		navDataStatus.value = 'error';
		toastError('Hiba történt a NAV lekérdezés során');
	} finally {
		loadingNavData.value = false;
	}
}

async function handleWorkflowAction(action: 'forward' | 'backward') {
	if (action === 'forward') {
		try {
			const response = await api.post(`/documents/${documentId.value}/workflow/advance`, {});
			if (response.data.success) {
				success('Dokumentum továbbküldve');
				await loadDocument(); // Reload to get updated status
				await loadHistory();
			} else {
				toastError(response.data.message || 'Hiba történt a továbbküldés során');
			}
		} catch (err: any) {
			console.error('Error advancing document:', err);
			toastError(err.response?.data?.message || 'Hiba történt a továbbküldés során');
		}
	} else {
		// Backward action - show modal for reason
		showRejectModal.value = true;
	}
}

async function handleReject(reason: string) {
	try {
		const response = await api.post(`/documents/${documentId.value}/workflow/reject`, {
			reason: reason
		});
		if (response.data.success) {
			success('Dokumentum elutasítva');
			showRejectModal.value = false;
			await loadDocument(); // Reload to get updated status
			await loadHistory();
		} else {
			toastError(response.data.message || 'Hiba történt az elutasítás során');
		}
	} catch (err: any) {
		console.error('Error rejecting document:', err);
		toastError(err.response?.data?.message || 'Hiba történt az elutasítás során');
	}
}

async function handleDelegate(userId: number) {
	try {
		const response = await api.post(`/documents/${documentId.value}/workflow/delegate`, {
			targetUserId: userId
		});
		if (response.data.success) {
			success('Dokumentum átadva');
			showDelegateModal.value = false;
			await loadDocument(); // Reload to get updated assignment
			await loadHistory();
		} else {
			toastError(response.data.message || 'Hiba történt az átadás során');
		}
	} catch (err: any) {
		console.error('Error delegating document:', err);
		toastError(err.response?.data?.message || 'Hiba történt az átadás során');
	}
}

async function handleFinalize() {
	// Finalization is done through advance when status is Accountant
	try {
		const response = await api.post(`/documents/${documentId.value}/workflow/advance`, {});
		if (response.data.success) {
			success('Iktatás lezárva');
			await loadDocument(); // Reload to get updated status
			await loadHistory();
		} else {
			toastError(response.data.message || 'Hiba történt a lezárás során');
		}
	} catch (err: any) {
		console.error('Error finalizing document:', err);
		toastError(err.response?.data?.message || 'Hiba történt a lezárás során');
	}
}

async function handleAddRelatedDocument(relatedDocumentId: number) {
	try {
		await api.post(`/documents/${documentId.value}/relations`, {
			documentId: documentId.value,
			relatedDocumentId,
		});
		success('Kapcsolat létrehozva');
		showRelatedDocumentModal.value = false;
		await loadRelatedDocuments();
	} catch (err: any) {
		toastError(err.response?.data?.message || 'Hiba történt a kapcsolat létrehozása során');
	}
}

async function handleRemoveRelation(relationId: number) {
	try {
		await api.delete(`/documents/${documentId.value}/relations/${relationId}`);
		success('Kapcsolat eltávolítva');
		await loadRelatedDocuments();
	} catch (err: any) {
		console.error('Error removing relation:', err);
		toastError(err.response?.data?.message || 'Hiba történt az eltávolítás során');
	}
}

function exportHistory() {
	// TODO: Implement history export
	success('Életút exportálva');
}

async function loadPdf() {
	if (!document.value) return;
	try {
		const baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5156/api';
		const response = await api.get(`${baseURL}/documents/${documentId.value}/download`, {
			responseType: 'blob',
		});
		
		// Create blob URL
		const blob = new Blob([response.data], { type: 'application/pdf' });
		if (pdfBlobUrl.value) {
			URL.revokeObjectURL(pdfBlobUrl.value);
		}
		pdfBlobUrl.value = URL.createObjectURL(blob);
	} catch (err) {
		console.error('Error loading PDF:', err);
		toastError('Hiba történt a PDF betöltése során');
	}
}

// Lifecycle
onMounted(() => {
	loadDocument();
});

// Cleanup blob URL on unmount
onUnmounted(() => {
	if (pdfBlobUrl.value) {
		URL.revokeObjectURL(pdfBlobUrl.value);
	}
});
</script>


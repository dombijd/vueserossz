<template>
	<BaseCard>
		<div v-if="loading" class="text-center py-8">
			<div class="inline-block animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
			<p class="mt-2 text-sm text-gray-600">Betöltés...</p>
		</div>
		<div v-else-if="relations.length === 0" class="text-center py-8 text-gray-500">
			<p>Nincsenek kapcsolódó dokumentumok</p>
		</div>
		<div v-else class="space-y-2">
			<div
				v-for="relation in relations"
				:key="relation.id"
				class="flex items-center justify-between p-4 border border-gray-200 rounded-md hover:bg-gray-50"
			>
				<div class="flex-1">
					<div class="flex items-center gap-2">
						<router-link
							:to="`/documents/${getRelatedDocumentId(relation)}`"
							class="font-medium text-blue-600 hover:text-blue-800"
						>
							{{ getRelatedArchiveNumber(relation) }}
						</router-link>
						<span
							v-if="relation.relatedDocumentTypeCode"
							:class="getDocumentTypeBadgeClass(relation.relatedDocumentTypeCode)"
							class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium"
						>
							{{ relation.relatedDocumentTypeName || 'Ismeretlen' }}
						</span>
					</div>
					<p v-if="relation.relatedDocumentSupplierName" class="text-sm text-gray-600 mt-1">
						{{ relation.relatedDocumentSupplierName }}
					</p>
					<p class="text-xs text-gray-500 mt-1">
						Létrehozva: {{ formatDate(relation.createdAt) }}
					</p>
				</div>
				<div class="flex gap-2">
					<BaseButton
						variant="secondary"
						size="sm"
						@click="previewDocument(getRelatedDocumentId(relation))"
						title="Előnézet"
					>
						<font-awesome-icon icon="eye" />
					</BaseButton>
					<BaseButton
						v-if="canRemove"
						variant="danger"
						size="sm"
						@click="removeRelation(relation.id)"
						title="Eltávolítás"
					>
						<font-awesome-icon icon="trash" />
					</BaseButton>
				</div>
			</div>
		</div>
	</BaseCard>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/authStore';
import { useToast } from '@/composables/useToast';
import BaseCard from '@/components/base/BaseCard.vue';
import BaseButton from '@/components/base/BaseButton.vue';
import api from '@/services/api';

interface DocumentRelation {
	id: number;
	documentId: number;
	documentArchiveNumber: string;
	relatedDocumentId: number;
	relatedDocumentArchiveNumber: string;
	relatedDocumentTypeName?: string;
	relatedDocumentTypeCode?: string;
	relatedDocumentSupplierName?: string;
	relationType?: string | null;
	comment?: string | null;
	createdByUserId: number;
	createdByName: string;
	createdAt: string;
}

interface Props {
	documentId: number;
	relations: DocumentRelation[];
}

const props = defineProps<Props>();

const emit = defineEmits<{
	refresh: [];
	remove: [relationId: number];
}>();

const router = useRouter();
const authStore = useAuthStore();
const { success, error: toastError } = useToast();

const loading = ref(false);

const canRemove = computed(() => {
	return authStore.userRoles.includes('Admin') || authStore.userRoles.includes('ElevatedApprover');
});

function getRelatedDocumentId(relation: DocumentRelation): number {
	return relation.documentId === props.documentId
		? relation.relatedDocumentId
		: relation.documentId;
}

function getRelatedArchiveNumber(relation: DocumentRelation): string {
	return relation.documentId === props.documentId
		? relation.relatedDocumentArchiveNumber
		: relation.documentArchiveNumber;
}

function formatDate(dateString: string): string {
	const date = new Date(dateString);
	return date.toLocaleDateString('hu-HU', {
		year: 'numeric',
		month: 'short',
		day: 'numeric',
	});
}

function getDocumentTypeBadgeClass(code: string): string {
	switch (code) {
		case 'SZLA': return 'bg-blue-100 text-blue-800';
		case 'TIG': return 'bg-green-100 text-green-800';
		case 'SZ': return 'bg-purple-100 text-purple-800';
		default: return 'bg-gray-100 text-gray-800';
	}
}

function previewDocument(documentId: number) {
	// Open in new tab or modal
	window.open(`/documents/${documentId}`, '_blank');
}

async function removeRelation(relationId: number) {
	if (!confirm('Biztosan eltávolítja ezt a kapcsolatot?')) {
		return;
	}
	
	loading.value = true;
	try {
		await api.delete(`/documents/${props.documentId}/relations/${relationId}`);
		success('Kapcsolat eltávolítva');
		emit('remove', relationId);
		emit('refresh');
	} catch (err: any) {
		console.error('Error removing relation:', err);
		toastError(err.response?.data?.message || 'Hiba történt az eltávolítás során');
	} finally {
		loading.value = false;
	}
}
</script>


<template>
	<BaseCard>
		<!-- New Comment Form -->
		<div class="mb-6 pb-6 border-b border-gray-200">
			<label class="block text-sm font-medium text-gray-700 mb-2">
				Új megjegyzés
			</label>
			<textarea
				v-model="newCommentText"
				rows="3"
				class="block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
				placeholder="Írjon megjegyzést..."
			></textarea>
			<div class="mt-2 flex justify-end">
				<BaseButton
					variant="primary"
					@click="submitComment"
					:loading="submitting"
					:disabled="!newCommentText.trim()"
				>
					Beküldés
				</BaseButton>
			</div>
		</div>

		<!-- Comments List -->
		<div v-if="loading" class="text-center py-8">
			<div class="inline-block animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
			<p class="mt-2 text-sm text-gray-600">Betöltés...</p>
		</div>
		<div v-else-if="comments.length === 0" class="text-center py-8 text-gray-500">
			<p>Még nincsenek megjegyzések</p>
		</div>
		<div v-else class="space-y-4">
			<div
				v-for="comment in comments"
				:key="comment.id"
				class="flex gap-3"
			>
				<div class="flex-shrink-0">
					<div class="w-10 h-10 rounded-full bg-gradient-to-br from-indigo-500 to-purple-600 text-white flex items-center justify-center font-semibold text-sm">
						{{ getUserInitials(comment.userName) }}
					</div>
				</div>
				<div class="flex-1 min-w-0">
					<div class="flex items-center gap-2 mb-1">
						<span class="font-medium text-gray-900">{{ comment.userName }}</span>
						<span class="text-xs text-gray-500" :title="formatDateTime(comment.createdAt)">
							{{ formatRelativeTime(comment.createdAt) }}
						</span>
					</div>
					<p class="text-gray-700 whitespace-pre-wrap">{{ comment.text }}</p>
				</div>
			</div>
		</div>
	</BaseCard>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useToast } from '@/composables/useToast';
import BaseCard from '@/components/base/BaseCard.vue';
import BaseButton from '@/components/base/BaseButton.vue';
import api from '@/services/api';
import { formatRelativeTime, formatDateTime } from '@/utils/date.utils';

import type { DocumentCommentDto } from '@/types/document.types';

interface Props {
	documentId: number;
	comments: DocumentCommentDto[];
}

const props = defineProps<Props>();

const emit = defineEmits<{
	refresh: [];
}>();

const { success, error: toastError } = useToast();

const newCommentText = ref('');
const submitting = ref(false);
const loading = ref(false);

function getUserInitials(name: string): string {
	const parts = name.split(' ');
	if (parts.length >= 2) {
		return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
	}
	return name.substring(0, 2).toUpperCase();
}

async function submitComment() {
	if (!newCommentText.value.trim()) {
		return;
	}
	
	submitting.value = true;
	try {
		await api.post(`/documents/${props.documentId}/comments`, {
			text: newCommentText.value.trim(),
		});
		newCommentText.value = '';
		success('Megjegyzés hozzáadva');
		emit('refresh');
	} catch (err: any) {
		console.error('Error submitting comment:', err);
		toastError(err.response?.data?.message || 'Hiba történt a megjegyzés hozzáadása során');
	} finally {
		submitting.value = false;
	}
}
</script>


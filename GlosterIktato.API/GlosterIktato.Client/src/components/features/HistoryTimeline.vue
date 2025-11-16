<template>
	<BaseCard>
		<div v-if="loading" class="text-center py-8">
			<div class="inline-block animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
			<p class="mt-2 text-sm text-gray-600">Betöltés...</p>
		</div>
		<div v-else-if="history.length === 0" class="text-center py-8 text-gray-500">
			<p>Nincs történeti adat</p>
		</div>
		<div v-else class="relative">
			<!-- Timeline -->
			<div class="space-y-4">
				<div
					v-for="(item, index) in history"
					:key="item.id"
					class="flex gap-4"
				>
					<!-- Timeline line -->
					<div class="flex flex-col items-center">
						<div
							:class="getActionBadgeClass(item.action)"
							class="w-10 h-10 rounded-full flex items-center justify-center text-white font-semibold text-sm"
						>
							<font-awesome-icon :icon="getActionIcon(item.action)" />
						</div>
						<div
							v-if="index < history.length - 1"
							class="w-0.5 h-full bg-gray-200 mt-2"
							style="min-height: 60px;"
						></div>
					</div>
					
					<!-- Content -->
					<div class="flex-1 pb-4">
						<div class="flex items-center gap-2 mb-1">
							<span class="font-medium text-gray-900">{{ item.userName }}</span>
							<span class="text-xs text-gray-500">{{ formatDate(item.createdAt) }}</span>
							<span
								:class="getActionBadgeClass(item.action)"
								class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium text-white"
							>
								{{ getActionLabel(item.action) }}
							</span>
						</div>
						
						<div v-if="item.fieldName" class="text-sm text-gray-600 mb-1">
							<strong>{{ item.fieldName }}:</strong>
							<span v-if="item.oldValue" class="text-red-600 line-through">{{ item.oldValue }}</span>
							<span v-if="item.oldValue && item.newValue" class="mx-2">→</span>
							<span v-if="item.newValue" class="text-green-600">{{ item.newValue }}</span>
						</div>
						
						<div v-if="item.comment" class="text-sm text-gray-700 mt-2 p-2 bg-gray-50 rounded">
							{{ item.comment }}
						</div>
					</div>
				</div>
			</div>
		</div>
	</BaseCard>
</template>

<script setup lang="ts">
import BaseCard from '@/components/base/BaseCard.vue';

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

interface Props {
	history: DocumentHistoryDto[];
	loading?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	loading: false,
});

function formatDate(dateString: string): string {
	const date = new Date(dateString);
	return date.toLocaleString('hu-HU', {
		year: 'numeric',
		month: 'short',
		day: 'numeric',
		hour: '2-digit',
		minute: '2-digit',
	});
}

function getActionLabel(action: string): string {
	const labels: Record<string, string> = {
		'Created': 'Létrehozva',
		'Updated': 'Módosítva',
		'StatusChanged': 'Státusz változás',
		'CommentAdded': 'Megjegyzés hozzáadva',
		'Forwarded': 'Továbbküldve',
		'Returned': 'Visszaküldve',
		'Rejected': 'Elutasítva',
		'Finalized': 'Lezárva',
		'Assigned': 'Hozzárendelve',
		'Delegated': 'Átadva',
	};
	return labels[action] || action;
}

function getActionIcon(action: string): [string, string] {
	const icons: Record<string, [string, string]> = {
		'Created': ['fas', 'plus'],
		'Updated': ['fas', 'edit'],
		'StatusChanged': ['fas', 'arrows-rotate'],
		'CommentAdded': ['fas', 'comment'],
		'Forwarded': ['fas', 'arrow-right'],
		'Returned': ['fas', 'arrow-left'],
		'Rejected': ['fas', 'times-circle'],
		'Finalized': ['fas', 'check-circle'],
		'Assigned': ['fas', 'user'],
		'Delegated': ['fas', 'user-plus'],
	};
	return icons[action] || ['fas', 'circle'];
}

function getActionBadgeClass(action: string): string {
	const classes: Record<string, string> = {
		'Created': 'bg-blue-500',
		'Updated': 'bg-yellow-500',
		'StatusChanged': 'bg-purple-500',
		'CommentAdded': 'bg-gray-500',
		'Forwarded': 'bg-green-500',
		'Returned': 'bg-orange-500',
		'Rejected': 'bg-red-500',
		'Finalized': 'bg-emerald-500',
		'Assigned': 'bg-indigo-500',
		'Delegated': 'bg-pink-500',
	};
	return classes[action] || 'bg-gray-500';
}
</script>


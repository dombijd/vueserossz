<template>
	<div class="w-full overflow-x-auto">
		<table class="min-w-full divide-y divide-gray-200" role="table" aria-label="Data table">
			<thead class="bg-gray-50">
				<tr>
					<th
						v-if="selectable"
						scope="col"
						class="w-12 px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
					>
						<input
							type="checkbox"
							:checked="allSelected"
							:indeterminate="someSelected && !allSelected"
							@change="toggleSelectAll"
							class="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
							:aria-label="allSelected ? 'Deselect all rows' : 'Select all rows'"
						/>
					</th>
					<th
						v-for="column in columns"
						:key="column.key"
						scope="col"
						:class="[
							'px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider',
							{ 'cursor-pointer hover:bg-gray-100 select-none': column.sortable }
						]"
						@click="column.sortable ? handleSort(column.key) : null"
						:aria-sort="getSortDirection(column.key)"
					>
						<div class="flex items-center gap-2">
							<span>{{ column.label }}</span>
							<span v-if="column.sortable" class="flex flex-col">
								<font-awesome-icon
									:icon="['fas', 'chevron-up']"
									:class="[
										'h-3 w-3 transition-opacity',
										currentSort.key === column.key && currentSort.direction === 'asc'
											? 'text-blue-600 opacity-100'
											: 'text-gray-400 opacity-50'
									]"
								/>
								<font-awesome-icon
									:icon="['fas', 'chevron-down']"
									:class="[
										'h-3 w-3 transition-opacity -mt-1',
										currentSort.key === column.key && currentSort.direction === 'desc'
											? 'text-blue-600 opacity-100'
											: 'text-gray-400 opacity-50'
									]"
								/>
							</span>
						</div>
					</th>
				</tr>
			</thead>
			<tbody class="bg-white divide-y divide-gray-200">
				<!-- Loading skeleton -->
				<template v-if="loading">
					<tr v-for="i in 5" :key="`skeleton-${i}`">
						<td v-if="selectable" class="px-6 py-4 whitespace-nowrap">
							<div class="h-4 w-4 bg-gray-200 rounded animate-pulse"></div>
						</td>
						<td v-for="column in columns" :key="column.key" class="px-6 py-4 whitespace-nowrap">
							<div class="h-4 bg-gray-200 rounded animate-pulse"></div>
						</td>
					</tr>
				</template>
				<!-- Data rows -->
				<template v-else-if="data.length > 0">
					<tr
						v-for="(row, index) in data"
						:key="getRowKey(row, index)"
						:class="[
							'hover:bg-gray-50 transition-colors',
							{ 'cursor-pointer': selectable || $attrs.onRowClick }
						]"
						@click="handleRowClick(row, index)"
					>
						<td v-if="selectable" class="px-6 py-4 whitespace-nowrap">
							<input
								type="checkbox"
								:checked="isSelected(row, index)"
								@change="toggleSelect(row, index)"
								@click.stop
								class="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
								:aria-label="`Select row ${index + 1}`"
							/>
						</td>
						<td
							v-for="column in columns"
							:key="column.key"
							class="px-6 py-4 whitespace-nowrap text-sm text-gray-900"
						>
							<slot :name="`cell-${column.key}`" :row="row" :value="getValue(row, column.key)">
								{{ getValue(row, column.key) }}
							</slot>
						</td>
					</tr>
				</template>
				<!-- Empty state -->
				<tr v-else>
					<td :colspan="selectable ? columns.length + 1 : columns.length" class="px-6 py-12 text-center">
						<div class="text-sm text-gray-500">No data available</div>
					</td>
				</tr>
			</tbody>
		</table>
	</div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';

/**
 * Table column definition
 */
export interface TableColumn {
	/** Unique key for the column (used to access data) */
	key: string;
	/** Display label for the column header */
	label: string;
	/** Whether the column is sortable */
	sortable?: boolean;
}

/**
 * Sort direction
 */
type SortDirection = 'asc' | 'desc' | null;

/**
 * Current sort state
 */
interface SortState {
	key: string;
	direction: SortDirection;
}

/**
 * BaseTable component props
 */
interface BaseTableProps<T = Record<string, any>> {
	/** Array of column definitions */
	columns: TableColumn[];
	/** Array of data objects to display */
	data: T[];
	/** Whether the table is in loading state */
	loading?: boolean;
	/** Whether rows can be selected (shows checkbox column) */
	selectable?: boolean;
	/** Key function to get unique identifier for each row */
	rowKey?: string | ((row: T, index: number) => string | number);
}

const props = withDefaults(defineProps<BaseTableProps>(), {
	loading: false,
	selectable: false
});

const emit = defineEmits<{
	/** Emitted when sort changes */
	(e: 'sort', key: string, direction: SortDirection): void;
	/** Emitted when row selection changes */
	(e: 'select', selectedRows: any[], selectedIndices: number[]): void;
	/** Emitted when a row is clicked */
	(e: 'row-click', row: any, index: number): void;
}>();

const selectedIndices = ref<Set<number>>(new Set());
const currentSort = ref<SortState>({ key: '', direction: null });

const allSelected = computed(() => {
	if (!props.selectable || props.data.length === 0) return false;
	return selectedIndices.value.size === props.data.length;
});

const someSelected = computed(() => {
	return selectedIndices.value.size > 0 && !allSelected.value;
});

function getRowKey(row: any, index: number): string | number {
	if (props.rowKey) {
		if (typeof props.rowKey === 'function') {
			return props.rowKey(row, index);
		}
		return row[props.rowKey];
	}
	return index;
}

function getValue(row: any, key: string): any {
	// Support nested keys like "user.name"
	return key.split('.').reduce((obj, k) => obj?.[k], row);
}

function isSelected(row: any, index: number): boolean {
	return selectedIndices.value.has(index);
}

function toggleSelect(row: any, index: number) {
	if (selectedIndices.value.has(index)) {
		selectedIndices.value.delete(index);
	} else {
		selectedIndices.value.add(index);
	}
	emitSelectionChange();
}

function toggleSelectAll() {
	if (allSelected.value) {
		selectedIndices.value.clear();
	} else {
		selectedIndices.value = new Set(props.data.map((_, i) => i));
	}
	emitSelectionChange();
}

function emitSelectionChange() {
	const selected = Array.from(selectedIndices.value).map(i => props.data[i]);
	emit('select', selected, Array.from(selectedIndices.value));
}

function handleSort(key: string) {
	if (currentSort.value.key === key) {
		// Cycle: asc -> desc -> null
		if (currentSort.value.direction === 'asc') {
			currentSort.value = { key, direction: 'desc' };
		} else if (currentSort.value.direction === 'desc') {
			currentSort.value = { key: '', direction: null };
		} else {
			currentSort.value = { key, direction: 'asc' };
		}
	} else {
		currentSort.value = { key, direction: 'asc' };
	}
	emit('sort', currentSort.value.key, currentSort.value.direction);
}

function getSortDirection(key: string): 'ascending' | 'descending' | 'none' {
	if (currentSort.value.key !== key) return 'none';
	return currentSort.value.direction === 'asc' ? 'ascending' : 'descending';
}

function handleRowClick(row: any, index: number) {
	emit('row-click', row, index);
}
</script>


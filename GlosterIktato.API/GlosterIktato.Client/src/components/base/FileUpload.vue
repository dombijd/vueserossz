<template>
	<div class="w-full">
		<!-- Drop zone -->
		<div
			ref="dropZoneRef"
			:class="dropZoneClass"
			@dragover.prevent="handleDragOver"
			@dragleave.prevent="handleDragLeave"
			@drop.prevent="handleDrop"
			@click="triggerFileInput"
			role="button"
			tabindex="0"
			:aria-label="`Upload files${multiple ? ' (multiple allowed)' : ''}`"
			@keydown.enter="triggerFileInput"
			@keydown.space.prevent="triggerFileInput"
		>
			<input
				ref="fileInputRef"
				type="file"
				:multiple="multiple"
				:accept="accept"
				class="hidden"
				@change="handleFileSelect"
				:aria-label="`File input${multiple ? ' (multiple allowed)' : ''}`"
			/>
			<div class="text-center">
				<font-awesome-icon :icon="['fas', 'upload']" class="h-8 w-8 text-gray-400 mb-2" />
				<p class="text-sm text-gray-600">
					<span class="font-medium text-blue-600">Click to upload</span>
					or drag and drop
				</p>
				<p v-if="accept" class="text-xs text-gray-500 mt-1">{{ accept }}</p>
				<p v-if="maxSize" class="text-xs text-gray-500 mt-1">
					Max size: {{ maxSize }}MB
				</p>
			</div>
		</div>

		<!-- Error message -->
		<p v-if="error" class="mt-2 text-sm text-rose-600" role="alert">
			{{ error }}
		</p>

		<!-- File list -->
		<div v-if="files.length > 0" class="mt-4 space-y-2">
			<div
				v-for="(file, index) in files"
				:key="file.id"
				class="flex items-center gap-3 p-3 bg-gray-50 rounded-md border border-gray-200"
			>
				<div class="flex-1 min-w-0">
					<p class="text-sm font-medium text-gray-900 truncate">{{ file.name }}</p>
					<p class="text-xs text-gray-500">{{ formatFileSize(file.size) }}</p>
					<!-- Progress bar -->
					<div v-if="file.progress !== undefined" class="mt-2">
						<div class="w-full bg-gray-200 rounded-full h-1.5">
							<div
								class="bg-blue-600 h-1.5 rounded-full transition-all duration-300"
								:style="{ width: `${file.progress}%` }"
							></div>
						</div>
						<p class="text-xs text-gray-500 mt-1">{{ file.progress }}%</p>
					</div>
					<!-- Error message for file -->
					<p v-if="file.error" class="text-xs text-rose-600 mt-1">{{ file.error }}</p>
				</div>
				<button
					@click="removeFile(index)"
					type="button"
					class="shrink-0 p-1 rounded-md text-gray-400 hover:text-rose-600 hover:bg-rose-50 transition-colors"
					:aria-label="`Remove ${file.name}`"
				>
					<font-awesome-icon :icon="['fas', 'times']" class="h-4 w-4" />
				</button>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';

/**
 * File with upload metadata
 */
interface FileWithMetadata {
	id: string;
	file: File;
	name: string;
	size: number;
	progress?: number;
	error?: string;
}

/**
 * FileUpload component props
 */
interface FileUploadProps {
	/** Whether multiple files can be selected */
	multiple?: boolean;
	/** Accepted file types (e.g., "image/*", ".pdf,.doc") */
	accept?: string;
	/** Maximum file size in MB */
	maxSize?: number;
}

const props = withDefaults(defineProps<FileUploadProps>(), {
	multiple: false,
	maxSize: undefined
});

const emit = defineEmits<{
	/** Emitted when files are selected */
	(e: 'files-selected', files: File[]): void;
	/** Emitted when files are selected with metadata */
	(e: 'files-selected-with-metadata', files: Array<{ file: File; id: string }>): void;
}>();

const fileInputRef = ref<HTMLInputElement | null>(null);
const dropZoneRef = ref<HTMLElement | null>(null);
const files = ref<FileWithMetadata[]>([]);
const isDragging = ref(false);
const error = ref<string | null>(null);

const dropZoneClass = computed(() => {
	const base = 'border-2 border-dashed rounded-lg p-8 text-center cursor-pointer transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2';
	const normal = 'border-gray-300 bg-white hover:border-blue-400 hover:bg-blue-50';
	const dragging = 'border-blue-500 bg-blue-50';
	return `${base} ${isDragging.value ? dragging : normal}`;
});

function triggerFileInput() {
	fileInputRef.value?.click();
}

function validateFile(file: File): string | null {
	if (props.maxSize) {
		const maxSizeBytes = props.maxSize * 1024 * 1024;
		if (file.size > maxSizeBytes) {
			return `File size exceeds ${props.maxSize}MB limit`;
		}
	}
	return null;
}

function processFiles(fileList: FileList | null) {
	if (!fileList || fileList.length === 0) return;

	error.value = null;
	const newFiles: File[] = [];

	Array.from(fileList).forEach((file) => {
		const validationError = validateFile(file);
		if (validationError) {
			error.value = validationError;
			return;
		}

		const fileMetadata: FileWithMetadata = {
			id: `${Date.now()}-${Math.random().toString(36).slice(2)}`,
			file,
			name: file.name,
			size: file.size
		};

		files.value.push(fileMetadata);
		newFiles.push(file);
	});

	if (newFiles.length > 0) {
		emit('files-selected', newFiles);
		// Also emit with metadata for progress tracking
		const filesWithMetadata = files.value
			.filter(f => newFiles.includes(f.file))
			.map(f => ({ file: f.file, id: f.id }));
		emit('files-selected-with-metadata', filesWithMetadata);
	}
}

function handleFileSelect(e: Event) {
	const target = e.target as HTMLInputElement;
	processFiles(target.files);
	// Reset input so same file can be selected again
	target.value = '';
}

function handleDragOver(e: DragEvent) {
	e.preventDefault();
	isDragging.value = true;
}

function handleDragLeave(e: DragEvent) {
	e.preventDefault();
	// Only set dragging to false if we're leaving the drop zone itself
	if (e.target === dropZoneRef.value || !dropZoneRef.value?.contains(e.target as Node)) {
		isDragging.value = false;
	}
}

function handleDrop(e: DragEvent) {
	e.preventDefault();
	isDragging.value = false;
	processFiles(e.dataTransfer?.files || null);
}

function removeFile(index: number) {
	files.value.splice(index, 1);
}

function formatFileSize(bytes: number): string {
	if (bytes === 0) return '0 Bytes';
	const k = 1024;
	const sizes = ['Bytes', 'KB', 'MB', 'GB'];
	const i = Math.floor(Math.log(bytes) / Math.log(k));
	return `${Math.round((bytes / Math.pow(k, i)) * 100) / 100} ${sizes[i]}`;
}

// Expose method to update progress
defineExpose({
	updateProgress: (fileId: string, progress: number) => {
		const file = files.value.find(f => f.id === fileId);
		if (file) {
			file.progress = progress;
		}
	},
	setError: (fileId: string, errorMessage: string) => {
		const file = files.value.find(f => f.id === fileId);
		if (file) {
			file.error = errorMessage;
		}
	},
	clearFiles: () => {
		files.value = [];
	},
	getFileId: (file: File): string | undefined => {
		const fileMetadata = files.value.find(f => 
			f.file === file || 
			(f.file.name === file.name && f.file.size === file.size && f.file.lastModified === file.lastModified)
		);
		return fileMetadata?.id;
	}
});
</script>


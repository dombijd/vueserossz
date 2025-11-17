/**
 * Document status enumeration
 */
export enum DocumentStatus {
	/** Draft status - document is in draft state */
	Draft = 'Vázlat',
	/** In progress status - document is being processed */
	InProgress = 'Folyamatban',
	/** Approved status - document has been approved */
	Approved = 'Jóváhagyott',
	/** Rejected status - document has been rejected */
	Rejected = 'Elutasított',
	/** Completed status - document processing is completed */
	Completed = 'Befejezett',
	/** Archived status - document has been archived */
	Archived = 'Archivált'
}

/**
 * Document interface matching backend model
 */
export interface Document {
	Id: number;
	RegistrationNumber: string;
	FileName: string;
	OriginalFileName: string;
	FilePath: string;
	Status: DocumentStatus | string;
	CreatedAt: string;
	ModifiedAt?: string | null;
	CompanyId: number;
	Company: {
		id: number;
		name: string;
		taxNumber: string;
	};
	DocumentTypeId: number;
	DocumentType: {
		id: number;
		name: string;
	};
	SupplierId?: number | null;
	Supplier?: {
		id: number;
		name: string;
	} | null;
	CreatedByUserId: number;
	CreatedBy: {
		id: number;
		email: string;
		firstName: string;
		lastName: string;
	};
	AssignedToUserId?: number | null;
	AssignedTo?: {
		id: number;
		email: string;
		firstName: string;
		lastName: string;
	} | null;
}

/**
 * Document response DTO from backend API (camelCase to match backend JSON serialization)
 */
export interface DocumentResponseDto {
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
}

/**
 * Paginated result wrapper (camelCase to match backend JSON serialization)
 */
export interface PaginatedResult<T> {
	data: T[];
	page: number;
	pageSize: number;
	totalCount: number;
	totalPages: number;
}

/**
 * Get status color for badge display
 * @param status - Document status
 * @returns TailwindCSS color class string
 */
export function getStatusColor(status: DocumentStatus | string): string {
	// Backend status values
	if (status === 'Draft' || status === DocumentStatus.Draft || status === 'Vázlat') {
		return 'bg-gray-100 text-gray-800';
	}
	if (status === 'PendingApproval' || status === 'Jóváhagyásra vár') {
		return 'bg-yellow-100 text-yellow-800';
	}
	if (status === 'ElevatedApproval' || status === 'Emelt szintű jóváhagyásra vár') {
		return 'bg-orange-100 text-orange-800';
	}
	if (status === 'Accountant' || status === 'Könyvelőnél') {
		return 'bg-blue-100 text-blue-800';
	}
	if (status === 'Done' || status === DocumentStatus.Completed || status === 'Kész' || status === 'Befejezett') {
		return 'bg-emerald-100 text-emerald-800';
	}
	if (status === 'Rejected' || status === DocumentStatus.Rejected || status === 'Elutasítva' || status === 'Elutasított') {
		return 'bg-red-100 text-red-800';
	}
	// Frontend enum values (for backward compatibility)
	if (status === DocumentStatus.InProgress || status === 'Folyamatban') {
		return 'bg-blue-100 text-blue-800';
	}
	if (status === DocumentStatus.Approved || status === 'Jóváhagyott') {
		return 'bg-green-100 text-green-800';
	}
	if (status === DocumentStatus.Archived || status === 'Archivált') {
		return 'bg-slate-100 text-slate-800';
	}
	// Default
	return 'bg-gray-100 text-gray-800';
}

/**
 * Get Hungarian display name for status
 * @param status - Document status
 * @returns Hungarian translation of the status
 */
export function getStatusDisplayName(status: DocumentStatus | string): string {
	// Backend status values
	if (status === 'Draft' || status === DocumentStatus.Draft || status === 'Vázlat') {
		return 'Vázlat';
	}
	if (status === 'PendingApproval' || status === 'Jóváhagyásra vár') {
		return 'Jóváhagyásra vár';
	}
	if (status === 'ElevatedApproval' || status === 'Emelt szintű jóváhagyásra vár') {
		return 'Emelt szintű jóváhagyásra vár';
	}
	if (status === 'Accountant' || status === 'Könyvelőnél') {
		return 'Könyvelőnél';
	}
	if (status === 'Done' || status === DocumentStatus.Completed || status === 'Kész' || status === 'Befejezett') {
		return 'Kész';
	}
	if (status === 'Rejected' || status === DocumentStatus.Rejected || status === 'Elutasítva' || status === 'Elutasított') {
		return 'Elutasítva';
	}
	// Frontend enum values (for backward compatibility)
	if (status === DocumentStatus.InProgress || status === 'Folyamatban') {
		return 'Folyamatban';
	}
	if (status === DocumentStatus.Approved || status === 'Jóváhagyott') {
		return 'Jóváhagyott';
	}
	if (status === DocumentStatus.Archived || status === 'Archivált') {
		return 'Archivált';
	}
	// Default - return as is
	return status as string;
}

/**
 * Get status icon for badge display
 * @param status - Document status
 * @returns FontAwesome icon definition array [prefix, iconName]
 */
export function getStatusIcon(status: DocumentStatus | string): [string, string] {
	// Backend status values
	if (status === 'Draft' || status === DocumentStatus.Draft || status === 'Vázlat') {
		return ['fas', 'file'];
	}
	if (status === 'PendingApproval' || status === 'Jóváhagyásra vár') {
		return ['fas', 'clock'];
	}
	if (status === 'ElevatedApproval' || status === 'Emelt szintű jóváhagyásra vár') {
		return ['fas', 'exclamation-circle'];
	}
	if (status === 'Accountant' || status === 'Könyvelőnél') {
		return ['fas', 'calculator'];
	}
	if (status === 'Done' || status === DocumentStatus.Completed || status === 'Kész' || status === 'Befejezett') {
		return ['fas', 'check-circle'];
	}
	if (status === 'Rejected' || status === DocumentStatus.Rejected || status === 'Elutasítva' || status === 'Elutasított') {
		return ['fas', 'times-circle'];
	}
	// Frontend enum values (for backward compatibility)
	if (status === DocumentStatus.InProgress || status === 'Folyamatban') {
		return ['fas', 'clock'];
	}
	if (status === DocumentStatus.Approved || status === 'Jóváhagyott') {
		return ['fas', 'check-circle'];
	}
	if (status === DocumentStatus.Archived || status === 'Archivált') {
		return ['fas', 'archive'];
	}
	// Default
	return ['fas', 'file'];
}

/**
 * Document history entry DTO
 */
export interface DocumentHistoryDto {
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

/**
 * Document detail DTO with full information
 */
export interface DocumentDetailDto {
	id: number;
	archiveNumber: string;
	originalFileName: string;
	status: string;
	invoiceNumber?: string | null;
	issueDate?: string | null;
	performanceDate?: string | null;
	paymentDeadline?: string | null;
	grossAmount?: number | null;
	netAmount?: number | null;
	taxAmount?: number | null;
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
	storagePath?: string;
	customFields?: Record<string, any>;
	history?: DocumentHistoryDto[];
	// Dataxo integráció mezők
	dataxoTransactionId?: string | null;
	dataxoStatus?: string | null; // null, Processing, Success, Failed
	dataxoSubmittedAt?: string | null;
	dataxoCompletedAt?: string | null;
}

/**
 * Document relation DTO
 */
export interface DocumentRelationDto {
	id: number;
	documentId: number;
	relatedDocumentId: number;
	relatedDocumentArchiveNumber: string;
	relatedDocumentTypeName: string;
	relatedDocumentTypeCode?: string;
	relatedDocumentSupplierName?: string;
	relationType: string;
	createdAt: string;
	createdByName: string;
}

/**
 * Document comment DTO
 */
export interface DocumentCommentDto {
	id: number;
	documentId: number;
	userId: number;
	userName: string;
	text: string;
	createdAt: string;
	modifiedAt?: string | null;
}

/**
 * Get badge color classes for document type
 * @param code - Document type code
 * @returns TailwindCSS color class string
 */
export function getDocumentTypeBadgeClass(code: string): string {
	switch (code) {
		case 'SZLA': return 'bg-blue-100 text-blue-800';
		case 'TIG': return 'bg-green-100 text-green-800';
		case 'SZ': return 'bg-purple-100 text-purple-800';
		default: return 'bg-gray-100 text-gray-800';
	}
}

/**
 * Get Hungarian label for document history action
 * @param action - Action code
 * @returns Hungarian translation of the action
 */
export function getActionLabel(action: string): string {
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

/**
 * Get icon for document history action
 * @param action - Action code
 * @returns FontAwesome icon definition array [prefix, iconName]
 */
export function getActionIcon(action: string): [string, string] {
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

/**
 * Get badge color classes for document history action
 * @param action - Action code
 * @returns TailwindCSS color class string
 */
export function getActionBadgeClass(action: string): string {
	switch (action) {
		case 'Created': return 'bg-green-500';
		case 'Updated': return 'bg-blue-500';
		case 'StatusChanged': return 'bg-indigo-500';
		case 'CommentAdded': return 'bg-purple-500';
		case 'Forwarded': return 'bg-cyan-500';
		case 'Returned': return 'bg-orange-500';
		case 'Rejected': return 'bg-red-500';
		case 'Finalized': return 'bg-emerald-500';
		case 'Assigned': return 'bg-blue-600';
		case 'Delegated': return 'bg-teal-500';
		default: return 'bg-gray-500';
	}
}

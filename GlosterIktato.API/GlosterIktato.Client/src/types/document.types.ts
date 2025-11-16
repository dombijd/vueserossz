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

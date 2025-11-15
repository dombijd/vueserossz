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
 * Get status color for badge display
 * @param status - Document status
 * @returns TailwindCSS color class string
 */
export function getStatusColor(status: DocumentStatus | string): string {
	switch (status) {
		case DocumentStatus.Draft:
			return 'bg-gray-100 text-gray-800';
		case DocumentStatus.InProgress:
			return 'bg-blue-100 text-blue-800';
		case DocumentStatus.Approved:
			return 'bg-green-100 text-green-800';
		case DocumentStatus.Rejected:
			return 'bg-red-100 text-red-800';
		case DocumentStatus.Completed:
			return 'bg-emerald-100 text-emerald-800';
		case DocumentStatus.Archived:
			return 'bg-slate-100 text-slate-800';
		default:
			return 'bg-gray-100 text-gray-800';
	}
}

/**
 * Get status icon for badge display
 * @param status - Document status
 * @returns FontAwesome icon definition array [prefix, iconName]
 */
export function getStatusIcon(status: DocumentStatus | string): [string, string] {
	switch (status) {
		case DocumentStatus.Draft:
			return ['fas', 'file'];
		case DocumentStatus.InProgress:
			return ['fas', 'clock'];
		case DocumentStatus.Approved:
			return ['fas', 'check-circle'];
		case DocumentStatus.Rejected:
			return ['fas', 'times-circle'];
		case DocumentStatus.Completed:
			return ['fas', 'clipboard-check'];
		case DocumentStatus.Archived:
			return ['fas', 'circle'];
		default:
			return ['fas', 'file'];
	}
}

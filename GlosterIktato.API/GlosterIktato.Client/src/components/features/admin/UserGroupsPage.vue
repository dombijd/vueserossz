<template>
	<AppLayout>
		<div class="space-y-6">
			<!-- Header -->
			<div class="flex items-center justify-between">
				<h1 class="text-2xl font-semibold text-gray-900">Felhasználói Csoportok</h1>
				<BaseButton
					variant="primary"
					:left-icon="['fas', 'plus']"
					@click="showCreateModal = true"
				>
					Új csoport
				</BaseButton>
			</div>

			<!-- Search and Filters -->
			<BaseCard>
				<div class="grid grid-cols-1 md:grid-cols-3 gap-4">
					<BaseInput
						v-model="searchQuery"
						placeholder="Keresés név vagy leírás alapján..."
					/>
					<BaseSelect
						v-model="filterCompanyId"
						:options="companyOptions"
						placeholder="Összes cég"
						label="Cég"
					/>
					<BaseSelect
						v-model="filterGroupType"
						:options="groupTypeOptions"
						placeholder="Összes típus"
						label="Típus"
					/>
				</div>
			</BaseCard>

			<!-- User Groups Table -->
			<BaseCard>
				<BaseTable
					:columns="columns"
					:data="filteredGroups"
					:loading="isLoading"
				>
					<template #cell-description="{ row }">
						<span class="text-sm text-gray-600">{{ row.description || '-' }}</span>
					</template>
					<template #cell-groupType="{ row }">
						<span
							v-if="row.groupType"
							class="px-2 py-1 text-xs rounded-full bg-purple-100 text-purple-800"
						>
							{{ row.groupType }}
						</span>
						<span v-else class="text-sm text-gray-400">-</span>
					</template>
					<template #cell-memberCount="{ row }">
						<span class="text-sm font-medium">{{ row.memberCount || 0 }}</span>
					</template>
					<template #cell-isActive="{ row }">
						<span
							:class="[
								'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium',
								row.isActive
									? 'bg-green-100 text-green-800'
									: 'bg-gray-100 text-gray-800'
							]"
						>
							{{ row.isActive ? 'Aktív' : 'Inaktív' }}
						</span>
					</template>
					<template #cell-actions="{ row }">
						<div class="flex gap-2">
							<BaseButton
								variant="ghost"
								size="sm"
								:left-icon="['fas', 'users']"
								@click="openMembersModal(row)"
							>
								Tagok
							</BaseButton>
							<BaseButton
								variant="ghost"
								size="sm"
								:left-icon="['fas', 'edit']"
								@click="editGroup(row)"
							>
								Szerkesztés
							</BaseButton>
							<BaseButton
								variant="ghost"
								size="sm"
								:left-icon="['fas', 'trash']"
								@click="confirmDelete(row)"
							>
								Törlés
							</BaseButton>
						</div>
					</template>
				</BaseTable>
			</BaseCard>
		</div>

		<!-- Create/Edit Group Modal -->
		<BaseModal
			v-model="showCreateModal"
			:title="editingGroup ? 'Csoport szerkesztése' : 'Új csoport'"
			size="md"
		>
			<div class="space-y-4">
				<BaseInput
					v-model="groupForm.name"
					label="Név"
					placeholder="Csoport neve"
					required
					:error="errors.name"
				/>
				<BaseInput
					v-model="groupForm.description"
					label="Leírás"
					placeholder="Csoport leírása"
					:error="errors.description"
				/>
				<BaseSelect
					v-model="groupForm.groupType"
					:options="groupTypeOptions"
					label="Típus"
					placeholder="Válasszon típust..."
				/>
				<BaseSelect
					v-model="groupForm.companyId"
					:options="companyOptions"
					label="Cég"
					placeholder="Válasszon céget..."
					required
					:error="errors.companyId"
				/>
				<BaseInput
					v-model.number="groupForm.priority"
					type="number"
					label="Prioritás"
					placeholder="0"
					:error="errors.priority"
				/>
			</div>
			<template #footer>
				<BaseButton variant="secondary" @click="cancelEdit">
					Mégse
				</BaseButton>
				<BaseButton
					variant="primary"
					@click="saveGroup"
					:loading="saving"
				>
					Mentés
				</BaseButton>
			</template>
		</BaseModal>

		<!-- Members Modal -->
		<BaseModal
			v-model="showMembersModal"
			title="Csoport tagjai"
			size="lg"
		>
			<div v-if="selectedGroupForMembers" class="space-y-4">
				<div class="flex items-center justify-between">
					<div>
						<h3 class="text-lg font-semibold">{{ selectedGroupForMembers.name }}</h3>
						<p v-if="selectedGroupForMembers.description" class="text-sm text-gray-600">
							{{ selectedGroupForMembers.description }}
						</p>
					</div>
					<BaseButton
						variant="primary"
						size="sm"
						:left-icon="['fas', 'plus']"
						@click="showAddMemberModal = true"
					>
						Tag hozzáadása
					</BaseButton>
				</div>

				<div v-if="groupMembers.length === 0" class="py-8 text-center text-gray-500">
					<p>Nincs tag a csoportban</p>
				</div>
				<div v-else>
					<BaseTable
						:columns="memberColumns"
						:data="groupMembers"
						:loading="loadingMembers"
					>
						<template #cell-roleInGroup="{ row }">
							<span v-if="row.roleInGroup" class="text-sm">{{ row.roleInGroup }}</span>
							<span v-else class="text-sm text-gray-400">-</span>
						</template>
						<template #cell-priority="{ row }">
							<span class="text-sm">{{ row.priority || 0 }}</span>
						</template>
						<template #cell-joinedAt="{ row }">
							<span class="text-sm text-gray-600">
								{{ formatDateTime(row.joinedAt) }}
							</span>
						</template>
						<template #cell-actions="{ row }">
							<BaseButton
								variant="ghost"
								size="sm"
								:left-icon="['fas', 'trash']"
								@click="confirmRemoveMember(row)"
							>
								Eltávolítás
							</BaseButton>
						</template>
					</BaseTable>
				</div>
			</div>
		</BaseModal>

		<!-- Add Member Modal -->
		<BaseModal
			v-model="showAddMemberModal"
			title="Tag hozzáadása"
			size="md"
		>
			<div class="space-y-4">
				<BaseSelect
					v-model="memberForm.userId"
					:options="availableUserOptions"
					label="Felhasználó"
					placeholder="Válasszon felhasználót..."
					required
					:error="errors.userId"
				/>
				<BaseInput
					v-model="memberForm.roleInGroup"
					label="Szerepkör csoportban"
					placeholder="pl. Lead, Member"
					:error="errors.roleInGroup"
				/>
				<BaseInput
					v-model.number="memberForm.priority"
					type="number"
					label="Prioritás"
					placeholder="0"
					:error="errors.priority"
				/>
			</div>
			<template #footer>
				<BaseButton variant="secondary" @click="showAddMemberModal = false">
					Mégse
				</BaseButton>
				<BaseButton
					variant="primary"
					@click="handleAddMember"
					:loading="isAddingMember"
				>
					Hozzáadás
				</BaseButton>
			</template>
		</BaseModal>

		<!-- Delete Confirmation Dialog -->
		<ConfirmDialog
			v-model="showDeleteDialog"
			title="Csoport törlése"
			message="Biztosan törölni szeretnéd ezt a csoportot? A tagok is törlődnek!"
			confirm-text="Törlés"
			cancel-text="Mégse"
			variant="danger"
			:loading="deleting"
			@confirm="handleDelete"
			@cancel="cancelDelete"
		/>

		<!-- Remove Member Confirmation Dialog -->
		<ConfirmDialog
			v-model="showRemoveMemberDialog"
			title="Tag eltávolítása"
			:message="`Eltávolítod ${memberToRemove?.userName} felhasználót a csoportból?`"
			confirm-text="Eltávolítás"
			cancel-text="Mégse"
			variant="danger"
			:loading="isRemovingMember"
			@confirm="handleRemoveMember"
			@cancel="cancelRemoveMember"
		/>
	</AppLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import AppLayout from '../../layout/AppLayout.vue';
import BaseCard from '../../base/BaseCard.vue';
import BaseTable, { type TableColumn } from '../../base/BaseTable.vue';
import BaseButton from '../../base/BaseButton.vue';
import BaseInput from '../../base/BaseInput.vue';
import BaseSelect from '../../base/BaseSelect.vue';
import BaseModal from '../../base/BaseModal.vue';
import ConfirmDialog from '../../base/ConfirmDialog.vue';
import { useToast } from '@/composables/useToast';
import api from '@/services/api';
import { formatDateTime } from '@/utils/date.utils';

interface UserGroupDto {
	id: number;
	name: string;
	description?: string;
	groupType?: string;
	companyId: number;
	companyName: string;
	isActive: boolean;
	priority: number;
	roundRobinIndex: number;
	memberCount: number;
	createdAt: string;
	members?: UserGroupMemberDto[];
}

interface UserGroupMemberDto {
	id: number;
	userId: number;
	userName: string;
	userEmail: string;
	roleInGroup?: string;
	priority: number;
	isActive: boolean;
	joinedAt: string;
	addedByName?: string;
}

interface CreateUserGroupDto {
	name: string;
	description?: string;
	groupType?: string;
	companyId: number;
	priority?: number;
}

interface UpdateUserGroupDto {
	name: string;
	description?: string;
	groupType?: string;
	priority?: number;
	isActive: boolean;
}

interface AddUserGroupMemberDto {
	userId: number;
	roleInGroup?: string;
	priority?: number;
}

interface Company {
	id: number;
	name: string;
}

interface User {
	id: number;
	firstName: string;
	lastName: string;
	email: string;
}

const { success, error: toastError } = useToast();

// State
const groups = ref<UserGroupDto[]>([]);
const companies = ref<Company[]>([]);
const availableUsers = ref<User[]>([]);
const isLoading = ref(false);
const saving = ref(false);
const deleting = ref(false);
const showCreateModal = ref(false);
const showDeleteDialog = ref(false);
const showMembersModal = ref(false);
const showAddMemberModal = ref(false);
const showRemoveMemberDialog = ref(false);
const editingGroup = ref<UserGroupDto | null>(null);
const groupToDelete = ref<UserGroupDto | null>(null);
const selectedGroupForMembers = ref<UserGroupDto | null>(null);
const groupMembers = ref<UserGroupMemberDto[]>([]);
const loadingMembers = ref(false);
const isAddingMember = ref(false);
const isRemovingMember = ref(false);
const memberToRemove = ref<UserGroupMemberDto | null>(null);
const searchQuery = ref('');
const filterCompanyId = ref<number | null>(null);
const filterGroupType = ref<string | null>(null);

const groupForm = ref<CreateUserGroupDto>({
	name: '',
	description: '',
	groupType: '',
	companyId: 0,
	priority: 0
});

const memberForm = ref<AddUserGroupMemberDto>({
	userId: 0,
	roleInGroup: '',
	priority: 0
});

const errors = ref<Record<string, string>>({});

// Options
const groupTypeOptions = [
	{ label: 'Összes típus', value: null },
	{ label: 'Approver', value: 'Approver' },
	{ label: 'ElevatedApprover', value: 'ElevatedApprover' },
	{ label: 'Accountant', value: 'Accountant' },
	{ label: 'Manager', value: 'Manager' }
];

const companyOptions = computed(() => {
	const options = companies.value.map(c => ({
		label: c.name,
		value: c.id
	}));
	return [{ label: 'Összes cég', value: null }, ...options];
});

const availableUserOptions = computed(() => {
	return availableUsers.value.map(u => ({
		label: `${u.firstName} ${u.lastName} (${u.email})`,
		value: u.id
	}));
});

// Table columns
const columns: TableColumn[] = [
	{ key: 'name', label: 'Név' },
	{ key: 'description', label: 'Leírás' },
	{ key: 'groupType', label: 'Típus' },
	{ key: 'companyName', label: 'Cég' },
	{ key: 'memberCount', label: 'Tagok száma' },
	{ key: 'isActive', label: 'Státusz' },
	{ key: 'actions', label: 'Műveletek' }
];

const memberColumns: TableColumn[] = [
	{ key: 'userName', label: 'Név' },
	{ key: 'userEmail', label: 'Email' },
	{ key: 'roleInGroup', label: 'Szerepkör' },
	{ key: 'priority', label: 'Prioritás' },
	{ key: 'joinedAt', label: 'Csatlakozás' },
	{ key: 'actions', label: 'Műveletek' }
];

// Computed
const filteredGroups = computed(() => {
	let result = groups.value;

	// Search filter
	if (searchQuery.value.trim()) {
		const query = searchQuery.value.toLowerCase().trim();
		result = result.filter(
			group =>
				group.name.toLowerCase().includes(query) ||
				(group.description && group.description.toLowerCase().includes(query))
		);
	}

	// Company filter
	if (filterCompanyId.value) {
		result = result.filter(group => group.companyId === filterCompanyId.value);
	}

	// Group type filter
	if (filterGroupType.value) {
		result = result.filter(group => group.groupType === filterGroupType.value);
	}

	return result;
});

// Methods
async function loadGroups() {
	isLoading.value = true;
	try {
		const params: Record<string, any> = {};
		if (filterCompanyId.value) {
			params.companyId = filterCompanyId.value;
		}
		if (filterGroupType.value) {
			params.groupType = filterGroupType.value;
		}

		const response = await api.get<UserGroupDto[]>('/user-groups', { params });
		groups.value = response.data;
	} catch (err) {
		toastError('Hiba történt a csoportok betöltése során');
		console.error('Error loading groups:', err);
	} finally {
		isLoading.value = false;
	}
}

async function loadCompanies() {
	try {
		const response = await api.get<Company[]>('/companies');
		companies.value = response.data;
	} catch (err) {
		console.error('Error loading companies:', err);
	}
}

async function loadUsers() {
	try {
		const response = await api.get<User[]>('/users');
		availableUsers.value = response.data;
	} catch (err) {
		console.error('Error loading users:', err);
	}
}

function editGroup(group: UserGroupDto) {
	editingGroup.value = group;
	groupForm.value = {
		name: group.name,
		description: group.description || '',
		groupType: group.groupType || '',
		companyId: group.companyId,
		priority: group.priority || 0
	};
	errors.value = {};
	showCreateModal.value = true;
}

function cancelEdit() {
	showCreateModal.value = false;
	editingGroup.value = null;
	groupForm.value = {
		name: '',
		description: '',
		groupType: '',
		companyId: 0,
		priority: 0
	};
	errors.value = {};
}

function validateForm(): boolean {
	errors.value = {};
	let isValid = true;

	if (!groupForm.value.name.trim()) {
		errors.value.name = 'Név kötelező';
		isValid = false;
	} else if (groupForm.value.name.length > 100) {
		errors.value.name = 'A név maximum 100 karakter lehet';
		isValid = false;
	}

	if (groupForm.value.description && groupForm.value.description.length > 500) {
		errors.value.description = 'A leírás maximum 500 karakter lehet';
		isValid = false;
	}

	if (groupForm.value.groupType && groupForm.value.groupType.length > 50) {
		errors.value.groupType = 'A típus maximum 50 karakter lehet';
		isValid = false;
	}

	if (!groupForm.value.companyId || groupForm.value.companyId <= 0) {
		errors.value.companyId = 'Cég kötelező';
		isValid = false;
	}

	if (groupForm.value.priority !== undefined && groupForm.value.priority < 0) {
		errors.value.priority = 'A prioritás nem lehet negatív';
		isValid = false;
	}

	return isValid;
}

async function saveGroup() {
	if (!validateForm()) {
		return;
	}

	saving.value = true;
	try {
		if (editingGroup.value) {
			// Update
			const updateData: UpdateUserGroupDto = {
				name: groupForm.value.name,
				description: groupForm.value.description || undefined,
				groupType: groupForm.value.groupType || undefined,
				priority: groupForm.value.priority || 0,
				isActive: editingGroup.value.isActive
			};
			const response = await api.put<UserGroupDto>(
				`/user-groups/${editingGroup.value.id}`,
				updateData
			);
			const index = groups.value.findIndex(g => g.id === editingGroup.value!.id);
			if (index !== -1) {
				groups.value[index] = response.data;
			}
			success('Csoport sikeresen frissítve');
		} else {
			// Create
			const response = await api.post<UserGroupDto>('/user-groups', groupForm.value);
			groups.value.push(response.data);
			success('Csoport sikeresen létrehozva');
		}
		cancelEdit();
	} catch (err: any) {
		toastError(err.response?.data?.message || 'Hiba történt a mentés során');
		console.error('Error saving group:', err);
	} finally {
		saving.value = false;
	}
}

function confirmDelete(group: UserGroupDto) {
	groupToDelete.value = group;
	showDeleteDialog.value = true;
}

function cancelDelete() {
	showDeleteDialog.value = false;
	groupToDelete.value = null;
}

async function handleDelete() {
	if (!groupToDelete.value) return;

	deleting.value = true;
	try {
		await api.delete(`/user-groups/${groupToDelete.value.id}`);
		groups.value = groups.value.filter(g => g.id !== groupToDelete.value!.id);
		success('Csoport sikeresen törölve');
		cancelDelete();
	} catch (err: any) {
		toastError(err.response?.data?.message || 'Hiba történt a törlés során');
		console.error('Error deleting group:', err);
	} finally {
		deleting.value = false;
	}
}

async function openMembersModal(group: UserGroupDto) {
	selectedGroupForMembers.value = group;
	loadingMembers.value = true;
	showMembersModal.value = true;

	try {
		const response = await api.get<UserGroupDto>(`/user-groups/${group.id}`);
		groupMembers.value = response.data.members || [];
	} catch (err) {
		toastError('Hiba történt a tagok betöltése során');
		console.error('Error loading members:', err);
		groupMembers.value = [];
	} finally {
		loadingMembers.value = false;
	}
}

async function handleAddMember() {
	if (!selectedGroupForMembers.value) return;

	errors.value = {};
	if (!memberForm.value.userId || memberForm.value.userId <= 0) {
		errors.value.userId = 'Felhasználó kötelező';
		return;
	}

	isAddingMember.value = true;
	try {
		await api.post(
			`/user-groups/${selectedGroupForMembers.value.id}/members`,
			memberForm.value
		);
		success('Tag hozzáadva');
		showAddMemberModal.value = false;
		memberForm.value = { userId: 0, roleInGroup: '', priority: 0 };
		// Reload members
		await openMembersModal(selectedGroupForMembers.value);
		// Reload groups to update member count
		await loadGroups();
	} catch (err: any) {
		toastError(err.response?.data?.message || 'Hiba történt a tag hozzáadása során');
		console.error('Error adding member:', err);
	} finally {
		isAddingMember.value = false;
	}
}

function confirmRemoveMember(member: UserGroupMemberDto) {
	memberToRemove.value = member;
	showRemoveMemberDialog.value = true;
}

function cancelRemoveMember() {
	showRemoveMemberDialog.value = false;
	memberToRemove.value = null;
}

async function handleRemoveMember() {
	if (!memberToRemove.value || !selectedGroupForMembers.value) return;

	isRemovingMember.value = true;
	try {
		await api.delete(
			`/user-groups/${selectedGroupForMembers.value.id}/members/${memberToRemove.value.userId}`
		);
		success('Tag eltávolítva');
		cancelRemoveMember();
		// Reload members
		await openMembersModal(selectedGroupForMembers.value);
		// Reload groups to update member count
		await loadGroups();
	} catch (err: any) {
		toastError(err.response?.data?.message || 'Hiba történt a tag eltávolítása során');
		console.error('Error removing member:', err);
	} finally {
		isRemovingMember.value = false;
	}
}

// Watch filters to reload groups
watch([filterCompanyId, filterGroupType], () => {
	loadGroups();
});

// Lifecycle
onMounted(() => {
	loadGroups();
	loadCompanies();
	loadUsers();
});
</script>


<template>
	<AppLayout>
		<div class="space-y-6">
			<!-- Header -->
			<div class="flex items-center justify-between">
				<h1 class="text-2xl font-semibold text-gray-900">Felhasználók</h1>
				<BaseButton
					variant="primary"
					:left-icon="['fas', 'plus']"
					@click="showCreateModal = true"
				>
					Új felhasználó
				</BaseButton>
			</div>

			<!-- Search -->
			<BaseCard>
				<BaseInput
					v-model="searchQuery"
					placeholder="Keresés név vagy email alapján..."
				/>
			</BaseCard>

			<!-- Users Table -->
			<BaseCard>
				<BaseTable
					:columns="columns"
					:data="filteredUsers"
					:loading="isLoading"
				>
					<template #cell-name="{ row }">
						{{ `${row.firstName} ${row.lastName}` }}
					</template>
					<template #cell-roles="{ row }">
						<div class="flex gap-1 flex-wrap">
							<span
								v-for="role in row.roles"
								:key="role.id"
								class="px-2 py-1 text-xs rounded-full bg-blue-100 text-blue-800"
							>
								{{ role.name }}
							</span>
						</div>
					</template>
					<template #cell-companies="{ row }">
						<div class="flex gap-1 flex-wrap">
							<span
								v-for="company in row.companies"
								:key="company.id"
								class="px-2 py-1 text-xs rounded-full bg-gray-100 text-gray-800"
							>
								{{ company.name }}
							</span>
						</div>
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
								:left-icon="['fas', 'edit']"
								@click="editUser(row)"
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

		<!-- Create/Edit User Modal -->
		<BaseModal
			v-model="showCreateModal"
			:title="editingUser ? 'Felhasználó szerkesztése' : 'Új felhasználó'"
			size="md"
		>
			<div class="space-y-4">
				<BaseInput
					v-model="userForm.email"
					label="Email"
					type="email"
					placeholder="email@example.com"
					required
					:error="errors.email"
				/>
				<BaseInput
					v-model="userForm.password"
					label="Jelszó"
					type="password"
					placeholder="Minimum 6 karakter"
					:required="!editingUser"
					:error="errors.password"
				/>
				<BaseInput
					v-model="userForm.firstName"
					label="Keresztnév"
					placeholder="Keresztnév"
					required
					:error="errors.firstName"
				/>
				<BaseInput
					v-model="userForm.lastName"
					label="Vezetéknév"
					placeholder="Vezetéknév"
					required
					:error="errors.lastName"
				/>

				<!-- Roles Multi-Select -->
				<div>
					<label class="mb-2 block text-sm font-medium text-gray-700">
						Szerepkörök
					</label>
					<div class="space-y-2 rounded-md border border-gray-300 p-3">
						<label
							v-for="role in availableRoles"
							:key="role"
							class="flex items-center"
						>
							<input
								type="checkbox"
								v-model="userForm.roleNames"
								:value="role"
								class="mr-2 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
							/>
							<span class="text-sm text-gray-700">{{ role }}</span>
						</label>
					</div>
					<p v-if="errors.roleNames" class="mt-1 text-sm text-rose-600">
						{{ errors.roleNames }}
					</p>
				</div>

				<!-- Companies Multi-Select -->
				<div>
					<label class="mb-2 block text-sm font-medium text-gray-700">
						Cégek
					</label>
					<div class="space-y-2 rounded-md border border-gray-300 p-3 max-h-48 overflow-y-auto">
						<label
							v-for="company in companies"
							:key="company.id"
							class="flex items-center"
						>
							<input
								type="checkbox"
								v-model="userForm.companyIds"
								:value="company.id"
								class="mr-2 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
							/>
							<span class="text-sm text-gray-700">{{ company.name }}</span>
						</label>
					</div>
					<p v-if="errors.companyIds" class="mt-1 text-sm text-rose-600">
						{{ errors.companyIds }}
					</p>
				</div>
			</div>
			<template #footer>
				<BaseButton variant="secondary" @click="cancelEdit">
					Mégse
				</BaseButton>
				<BaseButton
					variant="primary"
					@click="saveUser"
					:loading="saving"
				>
					Mentés
				</BaseButton>
			</template>
		</BaseModal>

		<!-- Delete Confirmation Dialog -->
		<ConfirmDialog
			v-model="showDeleteDialog"
			title="Felhasználó deaktiválása"
			message="Biztosan deaktiválod ezt a felhasználót?"
			confirm-text="Deaktiválás"
			cancel-text="Mégse"
			variant="danger"
			:loading="deleting"
			@confirm="handleDelete"
			@cancel="cancelDelete"
		/>
	</AppLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import AppLayout from '../../layout/AppLayout.vue';
import BaseCard from '../../base/BaseCard.vue';
import BaseTable, { type TableColumn } from '../../base/BaseTable.vue';
import BaseButton from '../../base/BaseButton.vue';
import BaseInput from '../../base/BaseInput.vue';
import BaseModal from '../../base/BaseModal.vue';
import ConfirmDialog from '../../base/ConfirmDialog.vue';
import { useToast } from '@/composables/useToast';
import api from '@/services/api';

interface Role {
	id: number;
	name: string;
}

interface Company {
	id: number;
	name: string;
}

interface UserDto {
	id: number;
	email: string;
	firstName: string;
	lastName: string;
	isActive: boolean;
	roles: Role[];
	companies: Company[];
	createdAt: string;
	createdBy?: string;
	modifiedAt?: string;
	modifiedBy?: string;
}

interface CreateUserDto {
	email: string;
	password: string;
	firstName: string;
	lastName: string;
	roleNames: string[];
	companyIds: number[];
}

interface UpdateUserDto {
	email?: string;
	password?: string;
	firstName?: string;
	lastName?: string;
	roleNames?: string[];
	companyIds?: number[];
}

const { success, error: toastError } = useToast();

// State
const users = ref<UserDto[]>([]);
const companies = ref<Company[]>([]);
const isLoading = ref(false);
const saving = ref(false);
const deleting = ref(false);
const showCreateModal = ref(false);
const showDeleteDialog = ref(false);
const editingUser = ref<UserDto | null>(null);
const userToDelete = ref<UserDto | null>(null);
const searchQuery = ref('');

const availableRoles = ['Admin', 'User', 'Accountant', 'Manager'];

const userForm = ref<CreateUserDto>({
	email: '',
	password: '',
	firstName: '',
	lastName: '',
	roleNames: [],
	companyIds: []
});

const errors = ref<Record<string, string>>({});

// Table columns
const columns: TableColumn[] = [
	{ key: 'name', label: 'Név' },
	{ key: 'email', label: 'Email' },
	{ key: 'roles', label: 'Szerepkörök' },
	{ key: 'companies', label: 'Cégek' },
	{ key: 'isActive', label: 'Státusz' },
	{ key: 'actions', label: 'Műveletek' }
];

// Computed
const filteredUsers = computed(() => {
	if (!searchQuery.value.trim()) {
		return users.value;
	}
	const query = searchQuery.value.toLowerCase().trim();
	return users.value.filter(
		user =>
			`${user.firstName} ${user.lastName}`.toLowerCase().includes(query) ||
			user.email.toLowerCase().includes(query)
	);
});

// Methods
async function loadUsers() {
	isLoading.value = true;
	try {
		const response = await api.get<UserDto[]>('/users');
		users.value = response.data;
	} catch (err) {
		toastError('Hiba történt a felhasználók betöltése során');
		console.error('Error loading users:', err);
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

function editUser(user: UserDto) {
	editingUser.value = user;
	userForm.value = {
		email: user.email,
		password: '', // Password is optional for updates
		firstName: user.firstName,
		lastName: user.lastName,
		roleNames: user.roles.map(r => r.name),
		companyIds: user.companies.map(c => c.id)
	};
	errors.value = {};
	showCreateModal.value = true;
}

function cancelEdit() {
	showCreateModal.value = false;
	editingUser.value = null;
	userForm.value = {
		email: '',
		password: '',
		firstName: '',
		lastName: '',
		roleNames: [],
		companyIds: []
	};
	errors.value = {};
}

function validateForm(): boolean {
	errors.value = {};
	let isValid = true;

	if (!userForm.value.email.trim()) {
		errors.value.email = 'Email kötelező';
		isValid = false;
	} else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(userForm.value.email)) {
		errors.value.email = 'Érvénytelen email cím';
		isValid = false;
	}

	if (!editingUser.value && !userForm.value.password.trim()) {
		errors.value.password = 'Jelszó kötelező';
		isValid = false;
	} else if (userForm.value.password && userForm.value.password.length < 6) {
		errors.value.password = 'A jelszó minimum 6 karakter';
		isValid = false;
	}

	if (!userForm.value.firstName.trim()) {
		errors.value.firstName = 'Keresztnév kötelező';
		isValid = false;
	}

	if (!userForm.value.lastName.trim()) {
		errors.value.lastName = 'Vezetéknév kötelező';
		isValid = false;
	}

	return isValid;
}

async function saveUser() {
	if (!validateForm()) {
		return;
	}

	saving.value = true;
	try {
		if (editingUser.value) {
			// Update
			const updateData: UpdateUserDto = {
				email: userForm.value.email,
				firstName: userForm.value.firstName,
				lastName: userForm.value.lastName,
				roleNames: userForm.value.roleNames,
				companyIds: userForm.value.companyIds
			};
			// Only include password if it was provided
			if (userForm.value.password.trim()) {
				updateData.password = userForm.value.password;
			}

			const response = await api.put<UserDto>(
				`/admin/users/${editingUser.value.id}`,
				updateData
			);
			const index = users.value.findIndex(u => u.id === editingUser.value!.id);
			if (index !== -1) {
				users.value[index] = response.data;
			}
			success('Felhasználó sikeresen frissítve');
		} else {
			// Create
			const response = await api.post<UserDto>('/admin/users', userForm.value);
			users.value.push(response.data);
			success('Felhasználó sikeresen létrehozva');
		}
		cancelEdit();
	} catch (err: any) {
		toastError(err.response?.data?.message || 'Hiba történt a mentés során');
		console.error('Error saving user:', err);
	} finally {
		saving.value = false;
	}
}

function confirmDelete(user: UserDto) {
	userToDelete.value = user;
	showDeleteDialog.value = true;
}

function cancelDelete() {
	showDeleteDialog.value = false;
	userToDelete.value = null;
}

async function handleDelete() {
	if (!userToDelete.value) return;

	deleting.value = true;
	try {
		const response = await api.delete<UserDto>(`/admin/users/${userToDelete.value.id}`);
		
		// Debug: log the response to see what we're getting
		console.log('Delete response:', response.data);
		console.log('Original isActive:', userToDelete.value.isActive);
		
		const index = users.value.findIndex(u => u.id === userToDelete.value!.id);
		if (index !== -1) {
			// Always use the response data - backend returns the updated user
			if (response.data) {
				users.value[index] = response.data;
			} else {
				// Fallback: if no response data, toggle manually
				users.value[index].isActive = !users.value[index].isActive;
			}
		}
		
		// Use the response data's isActive value (backend toggles it)
		// If response.data exists, use its isActive, otherwise use the toggled value
		const newStatus = response.data?.isActive !== undefined 
			? response.data.isActive 
			: !userToDelete.value.isActive;
		
		console.log('New status:', newStatus);
		success(`Felhasználó sikeresen ${newStatus ? 'aktiválva' : 'deaktiválva'}`);
		cancelDelete();
	} catch (err: any) {
		toastError(err.response?.data?.message || 'Hiba történt a törlés során');
		console.error('Error deleting user:', err);
	} finally {
		deleting.value = false;
	}
}

// Lifecycle
onMounted(() => {
	loadUsers();
	loadCompanies();
});
</script>


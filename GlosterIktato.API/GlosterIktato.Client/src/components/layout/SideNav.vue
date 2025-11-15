<template>
	<aside
		:class="[
			'h-screen border-r border-gray-200 bg-white transition-all duration-200 ease-in-out flex flex-col',
			collapsed ? 'w-16' : 'w-64'
		]"
		aria-label="Sidebar navigation"
	>
		<!-- Navigation items -->
		<nav class="flex-1 overflow-y-auto px-2 py-3">
			<ul class="space-y-1">
				<li v-for="(item, idx) in navItems" :key="idx">
					<!-- Leaf item -->
					<router-link
						v-if="!item.children || item.children.length === 0"
						:to="item.to || '#'"
						:class="linkClass(isActive(item))"
						:aria-label="collapsed ? item.label : undefined"
					>
						<FontAwesomeIcon v-if="item.icon" :icon="item.icon" class="h-5 w-5 shrink-0" />
						<span v-if="!collapsed" class="truncate">{{ item.label }}</span>
					</router-link>

					<!-- Group with children -->
					<div v-else>
						<button
							type="button"
							@click="toggleGroup(idx)"
							:class="linkClass(isGroupActive(item))"
							:aria-label="collapsed ? item.label : undefined"
							:aria-expanded="openedGroups.has(idx) ? 'true' : 'false'"
						>
							<FontAwesomeIcon v-if="item.icon" :icon="item.icon" class="h-5 w-5 shrink-0" />
							<span v-if="!collapsed" class="truncate flex-1 text-left">{{ item.label }}</span>
							<FontAwesomeIcon
								v-if="!collapsed"
								:icon="['fas', 'chevron-right']"
								:class="[
									'h-4 w-4 transition-transform duration-200',
									openedGroups.has(idx) ? 'rotate-90' : ''
								]"
							/>
						</button>
						<Transition
							enter-active-class="transition ease-out duration-100"
							enter-from-class="transform opacity-0 -translate-y-1"
							enter-to-class="transform opacity-100 translate-y-0"
							leave-active-class="transition ease-in duration-75"
							leave-from-class="transform opacity-100 translate-y-0"
							leave-to-class="transform opacity-0 -translate-y-1"
						>
							<ul v-if="openedGroups.has(idx) && !collapsed" class="mt-1 ml-6 space-y-1">
								<li v-for="(child, cidx) in item.children" :key="cidx">
									<router-link
										:to="child.to || '#'"
										:class="childLinkClass(isActive(child))"
									>
										<FontAwesomeIcon v-if="child.icon" :icon="child.icon" class="h-4 w-4 shrink-0" />
										<span class="truncate">{{ child.label }}</span>
									</router-link>
								</li>
							</ul>
						</Transition>
					</div>
				</li>
			</ul>
		</nav>

		<!-- Collapse button at bottom -->
		<div class="border-t border-gray-200 p-2">
			<button
				type="button"
				@click="$emit('toggle-collapse')"
				class="w-full flex items-center gap-3 rounded-md px-3 py-2 text-sm text-gray-700 hover:bg-gray-100 transition-colors"
				:aria-label="collapsed ? 'Expand sidebar' : 'Collapse sidebar'"
			>
				<FontAwesomeIcon :icon="['fas', 'bars']" class="h-5 w-5 shrink-0" />
				<span v-if="!collapsed" class="truncate">Collapse</span>
			</button>
		</div>
	</aside>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import type { IconDefinition, IconProp } from '@fortawesome/fontawesome-svg-core';

/**
 * Navigation item interface
 */
interface SideNavItem {
	label: string;
	to?: string;
	icon?: IconProp | IconDefinition | [string, string] | string;
	children?: SideNavItem[];
	exact?: boolean;
}

/**
 * SideNav component props
 */
interface SideNavProps {
	/** Whether sidebar is collapsed */
	collapsed?: boolean;
}

const props = withDefaults(defineProps<SideNavProps>(), {
	collapsed: false
});

const emit = defineEmits<{
	/** Emitted when collapse toggle is clicked */
	(e: 'toggle-collapse'): void;
}>();

const router = useRouter();
const route = useRoute();
const openedGroups = ref<Set<number>>(new Set());

// Default navigation items
const navItems = computed<SideNavItem[]>(() => [
	{
		label: 'Dashboard',
		to: '/dashboard',
		icon: ['fas', 'home'],
		exact: true
	},
	{
		label: 'Aktuális ügyeim',
		to: '/current-cases',
		icon: ['fas', 'list']
	},
	{
		label: 'Dokumentumok listája',
		to: '/documents',
		icon: ['fas', 'file-invoice']
	},
	{
		label: 'Új dokumentum',
		to: '/documents/new',
		icon: ['fas', 'plus']
	},
	{
		label: 'Adminisztráció',
		icon: ['fas', 'cog'],
		children: [
			{
				label: 'Felhasználók',
				to: '/admin/users',
				icon: ['fas', 'users']
			},
			{
				label: 'Cégek',
				to: '/admin/companies',
				icon: ['fas', 'building']
			},
			{
				label: 'Szállítók',
				to: '/admin/suppliers',
				icon: ['fas', 'truck']
			},
			{
				label: 'Workflow',
				to: '/admin/workflow',
				icon: ['fas', 'cog']
			}
		]
	}
]);

// Auto-open parent group if child is active
watch(
	() => route.path,
	(path) => {
		navItems.value.forEach((item, idx) => {
			if (item.children && item.children.some((child) => isActive(child))) {
				openedGroups.value.add(idx);
			}
		});
	},
	{ immediate: true }
);

// Close all groups when collapsed
watch(
	() => props.collapsed,
	(val) => {
		if (val) {
			openedGroups.value = new Set();
		}
	}
);

const collapsed = computed(() => props.collapsed);

function linkClass(active: boolean): string {
	const base = 'w-full flex items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors';
	const rest = 'text-gray-700 hover:bg-gray-100';
	const act = 'bg-blue-50 text-blue-700 hover:bg-blue-50 font-medium';
	return `${base} ${active ? act : rest}`;
}

function childLinkClass(active: boolean): string {
	const base = 'w-full flex items-center gap-2 rounded-md px-3 py-1.5 text-sm transition-colors';
	const rest = 'text-gray-700 hover:bg-gray-100';
	const act = 'bg-blue-50 text-blue-700 hover:bg-blue-50 font-medium';
	return `${base} ${active ? act : rest}`;
}

function isActive(item: SideNavItem): boolean {
	if (!item.to) return false;
	if (item.exact) {
		return route.path === item.to;
	}
	return route.path.startsWith(item.to);
}

function isGroupActive(item: SideNavItem): boolean {
	if (!item.children) return false;
	return item.children.some(isActive);
}

function toggleGroup(idx: number) {
	if (openedGroups.value.has(idx)) {
		openedGroups.value.delete(idx);
	} else {
		openedGroups.value.add(idx);
	}
	openedGroups.value = new Set(openedGroups.value);
}
</script>
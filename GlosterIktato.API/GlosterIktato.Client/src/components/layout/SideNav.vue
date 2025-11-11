<template>
	<aside
		:class="[
			'h-screen border-r border-gray-200 bg-white transition-all duration-200 ease-in-out',
			collapsed ? 'w-16' : 'w-64'
		]"
	>
		<nav class="h-full overflow-y-auto px-2 py-3">
			<ul class="space-y-1">
				<li v-for="(item, idx) in items" :key="idx">
					<!-- Leaf item -->
					<button
						v-if="!item.children || item.children.length === 0"
						type="button"
						@click="go(item)"
						:class="linkClass(isActive(item))"
					>
						<FontAwesomeIcon v-if="item.icon" :icon="item.icon" class="h-4 w-4 shrink-0" />
						<span v-if="!collapsed" class="truncate">{{ item.label }}</span>
					</button>

					<!-- Group with children -->
					<div v-else>
						<button
							type="button"
							@click="toggleGroup(idx)"
							:class="linkClass(isGroupActive(item))"
						>
							<FontAwesomeIcon v-if="item.icon" :icon="item.icon" class="h-4 w-4 shrink-0" />
							<span v-if="!collapsed" class="truncate flex-1 text-left">{{ item.label }}</span>
							<svg v-if="!collapsed" :class="openedGroups.has(idx) ? 'rotate-90' : ''" class="h-4 w-4 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
							</svg>
						</button>
						<ul v-if="openedGroups.has(idx) && !collapsed" class="mt-1 ml-6 space-y-1">
							<li v-for="(child, cidx) in item.children" :key="cidx">
								<button type="button" @click="go(child)" :class="childLinkClass(isActive(child))">
									<FontAwesomeIcon v-if="child.icon" :icon="child.icon" class="h-4 w-4 shrink-0" />
									<span class="truncate">{{ child.label }}</span>
								</button>
							</li>
						</ul>
					</div>
				</li>
			</ul>
		</nav>
	</aside>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome';
import type { IconDefinition, IconProp } from '@fortawesome/fontawesome-svg-core';

interface SideNavItem {
	label: string;
	to?: string;
	icon?: IconProp | IconDefinition | [string, string] | string;
	children?: SideNavItem[];
	exact?: boolean;
}

interface SideNavProps {
	items: SideNavItem[];
	collapsed?: boolean;
}

const props = withDefaults(defineProps<SideNavProps>(), {
	items: () => [],
	collapsed: false
});

const emit = defineEmits<{
	(e: 'update:collapsed', value: boolean): void;
}>();

const router = useRouter();
const route = useRoute();
const openedGroups = ref<Set<number>>(new Set());

watch(() => props.collapsed, (val) => {
	if (val) {
		openedGroups.value = new Set(); // close all when collapsed
	}
});

function linkClass(active: boolean): string {
	const base = 'w-full flex items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors';
	const rest = 'text-gray-700 hover:bg-gray-100';
	const act = 'bg-blue-50 text-blue-700 hover:bg-blue-50';
	return `${base} ${active ? act : rest}`;
}
function childLinkClass(active: boolean): string {
	const base = 'w-full flex items-center gap-2 rounded-md px-3 py-1.5 text-sm transition-colors';
	const rest = 'text-gray-700 hover:bg-gray-100';
	const act = 'bg-blue-50 text-blue-700 hover:bg-blue-50';
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
function go(item: SideNavItem) {
	if (item.to) {
		router.push(item.to);
	}
}

const collapsed = computed({
	get: () => props.collapsed,
	set: (v: boolean) => emit('update:collapsed', v)
});
</script>



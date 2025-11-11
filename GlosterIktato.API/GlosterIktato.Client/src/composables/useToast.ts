import { reactive } from 'vue';

export type ToastType = 'success' | 'error' | 'info';

export interface Toast {
	id: number;
	type: ToastType;
	message: string;
	timeoutMs?: number;
}

const state = reactive<{ toasts: Toast[] }>({
	toasts: []
});

let idCounter = 1;

function push(type: ToastType, message: string, timeoutMs = 3000): number {
	const id = idCounter++;
	state.toasts.push({ id, type, message, timeoutMs });
	if (timeoutMs && timeoutMs > 0) {
		window.setTimeout(() => remove(id), timeoutMs);
	}
	return id;
}

function remove(id: number): void {
	const idx = state.toasts.findIndex(t => t.id === id);
	if (idx >= 0) {
		state.toasts.splice(idx, 1);
	}
}

export function useToast() {
	return {
		toasts: state.toasts,
		push,
		remove,
		success(message: string, timeoutMs?: number) { return push('success', message, timeoutMs); },
		error(message: string, timeoutMs?: number) { return push('error', message, timeoutMs); },
		info(message: string, timeoutMs?: number) { return push('info', message, timeoutMs); }
	};
}



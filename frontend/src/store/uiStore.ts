import { create } from 'zustand';

export type ToastType = 'success' | 'error' | 'info' | 'warning';

export interface Toast {
    id: string;
    type: ToastType;
    message: string;
    duration?: number;
}

export interface Alert {
    id: string;
    type: ToastType;
    title: string;
    message: string;
}

interface UIState {
    toasts: Toast[];
    alerts: Alert[];

    // Toast Actions
    addToast: (type: ToastType, message: string, duration?: number) => void;
    removeToast: (id: string) => void;

    // Alert Actions
    addAlert: (type: ToastType, title: string, message: string) => void;
    removeAlert: (id: string) => void;
}

export const useUIStore = create<UIState>((set) => ({
    toasts: [],
    alerts: [],

    addToast: (type, message, duration = 4000) => {
        const id = crypto.randomUUID();
        set((state) => ({
            toasts: [...state.toasts, { id, type, message, duration }],
        }));

        // Auto-remove after duration
        setTimeout(() => {
            set((state) => ({
                toasts: state.toasts.filter((t) => t.id !== id),
            }));
        }, duration);
    },

    removeToast: (id) =>
        set((state) => ({
            toasts: state.toasts.filter((t) => t.id !== id),
        })),

    addAlert: (type, title, message) => {
        const id = crypto.randomUUID();
        set((state) => ({
            alerts: [...state.alerts, { id, type, title, message }],
        }));
    },

    removeAlert: (id) =>
        set((state) => ({
            alerts: state.alerts.filter((a) => a.id !== id),
        })),
}));

// Helper functions for easy usage: toast.success("Message")
export const toast = {
    success: (message: string) => useUIStore.getState().addToast('success', message),
    error: (message: string) => useUIStore.getState().addToast('error', message),
    info: (message: string) => useUIStore.getState().addToast('info', message),
    warning: (message: string) => useUIStore.getState().addToast('warning', message),
};

export const alert = {
    success: (title: string, message: string) => useUIStore.getState().addAlert('success', title, message),
    error: (title: string, message: string) => useUIStore.getState().addAlert('error', title, message),
    info: (title: string, message: string) => useUIStore.getState().addAlert('info', title, message),
    warning: (title: string, message: string) => useUIStore.getState().addAlert('warning', title, message),
};

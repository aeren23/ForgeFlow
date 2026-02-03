import { create } from 'zustand';
import type { AiProgressMessage, NotificationMessage } from '../services/signalRService';

export interface NotificationState {
    // Connection state
    isConnected: boolean;
    setConnected: (connected: boolean) => void;

    // AI Progress logs (for live terminal view)
    aiLogs: AiProgressMessage[];
    addAiLog: (log: AiProgressMessage) => void;
    clearAiLogs: () => void;

    // User notifications (toast-style)
    notifications: NotificationMessage[];
    addNotification: (notification: NotificationMessage) => void;
    removeNotification: (index: number) => void;
    clearNotifications: () => void;

    // Unread count
    unreadCount: number;
    incrementUnread: () => void;
    resetUnread: () => void;

    // LiveLog UI State
    isLiveLogOpen: boolean;
    setLiveLogOpen: (open: boolean) => void;
}

export const useNotificationStore = create<NotificationState>((set) => ({
    // Connection
    isConnected: false,
    setConnected: (connected) => set({ isConnected: connected }),

    // AI Progress Logs
    aiLogs: [],
    addAiLog: (log) => set((state) => {
        // Check for ANY duplicate in recent history (last 20 logs) to handle race conditions
        const isDuplicate = state.aiLogs.slice(-20).some(existingLog =>
            existingLog.message === log.message &&
            existingLog.progressPercentage === log.progressPercentage &&
            existingLog.requestId === log.requestId
        );

        if (isDuplicate) {
            return state;
        }

        return {
            aiLogs: [...state.aiLogs.slice(-99), log], // Keep last 100 logs
            isLiveLogOpen: true, // Auto-open on new log
        };
    }),
    clearAiLogs: () => set({ aiLogs: [] }),

    // Notifications
    notifications: [],
    addNotification: (notification) => set((state) => ({
        notifications: [notification, ...state.notifications.slice(0, 49)], // Keep last 50
        unreadCount: state.unreadCount + 1,
    })),
    removeNotification: (index) => set((state) => ({
        notifications: state.notifications.filter((_, i) => i !== index),
    })),
    clearNotifications: () => set({ notifications: [], unreadCount: 0 }),

    // Unread count
    unreadCount: 0,
    incrementUnread: () => set((state) => ({ unreadCount: state.unreadCount + 1 })),
    resetUnread: () => set({ unreadCount: 0 }),

    // LiveLog UI State
    isLiveLogOpen: false,
    setLiveLogOpen: (open) => set({ isLiveLogOpen: open }),
}));

import * as signalR from '@microsoft/signalr';
import { useAuthStore } from '../store/authStore';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:8090';
const HUB_URL = `${API_URL}/hubs/forge`;

let connection: signalR.HubConnection | null = null;

export interface AiProgressMessage {
    requestId: string;
    message: string;
    progressPercentage: number;
    isComplete: boolean;
    logEntries?: string[];
    requestedFiles?: string[];
}

export interface BoardUpdateMessage {
    projectId: string;
    issueKey: string;
    updateType: 'status_changed' | 'assigned' | 'created' | 'deleted';
    data?: unknown;
}

export interface NotificationMessage {
    type: string;
    title: string;
    message: string;
    data?: unknown;
    timestamp: string;
}

export interface ReviewUpdateMessage {
    issueKey: string;
    pullNumber: number;
    prStatus: 'open' | 'merged' | 'closed';
}

export interface CiCdUpdateMessage {
    issueKey: string;
    projectId: string;
    workflowName: string;
    status: 'queued' | 'in_progress' | 'success' | 'failure' | 'cancelled';
    htmlUrl?: string;
    timestamp: string;
}

type EventCallback<T> = (message: T) => void;

const eventHandlers: {
    aiProgress: EventCallback<AiProgressMessage>[];
    boardUpdate: EventCallback<BoardUpdateMessage>[];
    notification: EventCallback<NotificationMessage>[];
    installationListUpdated: EventCallback<{ installationId: number; accountLogin: string }>[];
    reviewUpdate: EventCallback<ReviewUpdateMessage>[];
    cicdUpdate: EventCallback<CiCdUpdateMessage>[];
} = {
    aiProgress: [],
    boardUpdate: [],
    notification: [],
    installationListUpdated: [],
    reviewUpdate: [],
    cicdUpdate: [],
};

export const signalRService = {
    /**
     * Start the SignalR connection.
     * Should be called after user authentication.
     */
    async start(): Promise<void> {
        if (connection?.state === signalR.HubConnectionState.Connected) {
            console.log('[SignalR] Already connected');
            return;
        }

        const accessToken = useAuthStore.getState().accessToken;
        if (!accessToken) {
            console.warn('[SignalR] No access token, cannot connect');
            return;
        }

        connection = new signalR.HubConnectionBuilder()
            .withUrl(HUB_URL, {
                accessTokenFactory: () => accessToken,
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // Register server-to-client event handlers
        connection.on('AiProgress', (msg: AiProgressMessage) => {
            console.log('[SignalR] AiProgress:', msg);
            eventHandlers.aiProgress.forEach(cb => cb(msg));
        });

        connection.on('BoardUpdate', (msg: BoardUpdateMessage) => {
            console.log('[SignalR] BoardUpdate:', msg);
            eventHandlers.boardUpdate.forEach(cb => cb(msg));
        });

        connection.on('Notification', (msg: NotificationMessage) => {
            console.log('[SignalR] Notification:', msg);
            eventHandlers.notification.forEach(cb => cb(msg));
        });

        connection.on('InstallationListUpdated', (msg: { installationId: number; accountLogin: string }) => {
            console.log('[SignalR] InstallationListUpdated:', msg);
            eventHandlers.installationListUpdated.forEach(cb => cb(msg));
        });

        connection.on('ReviewUpdate', (msg: ReviewUpdateMessage) => {
            console.log('[SignalR] ReviewUpdate:', msg);
            eventHandlers.reviewUpdate.forEach(cb => cb(msg));
        });

        connection.on('CiCdUpdate', (msg: CiCdUpdateMessage) => {
            console.log('[SignalR] CiCdUpdate:', msg);
            eventHandlers.cicdUpdate.forEach(cb => cb(msg));
        });

        // Connection state change handlers
        connection.onreconnecting((error) => {
            console.warn('[SignalR] Reconnecting...', error);
        });

        connection.onreconnected((connectionId) => {
            console.log('[SignalR] Reconnected:', connectionId);
        });

        connection.onclose((error) => {
            console.log('[SignalR] Connection closed', error);
        });

        try {
            await connection.start();
            console.log('[SignalR] Connected to ForgeHub');
        } catch (err) {
            console.error('[SignalR] Connection failed:', err);
            throw err;
        }
    },

    /**
     * Stop the SignalR connection.
     */
    async stop(): Promise<void> {
        if (connection) {
            await connection.stop();
            connection = null;
            console.log('[SignalR] Disconnected');
        }
    },

    /**
     * Join a project group to receive project-specific updates.
     */
    async joinProject(projectId: string): Promise<void> {
        if (connection?.state === signalR.HubConnectionState.Connected) {
            await connection.invoke('JoinProjectGroup', projectId);
            console.log('[SignalR] Joined project group:', projectId);
        }
    },

    /**
     * Leave a project group.
     */
    async leaveProject(projectId: string): Promise<void> {
        if (connection?.state === signalR.HubConnectionState.Connected) {
            await connection.invoke('LeaveProjectGroup', projectId);
            console.log('[SignalR] Left project group:', projectId);
        }
    },

    /**
     * Subscribe to AI progress events.
     */
    onAiProgress(callback: EventCallback<AiProgressMessage>): () => void {
        eventHandlers.aiProgress.push(callback);
        return () => {
            const idx = eventHandlers.aiProgress.indexOf(callback);
            if (idx > -1) eventHandlers.aiProgress.splice(idx, 1);
        };
    },

    /**
     * Subscribe to board update events.
     */
    onBoardUpdate(callback: EventCallback<BoardUpdateMessage>): () => void {
        eventHandlers.boardUpdate.push(callback);
        return () => {
            const idx = eventHandlers.boardUpdate.indexOf(callback);
            if (idx > -1) eventHandlers.boardUpdate.splice(idx, 1);
        };
    },

    /**
     * Subscribe to notification events.
     */
    onNotification(callback: EventCallback<NotificationMessage>): () => void {
        eventHandlers.notification.push(callback);
        return () => {
            const idx = eventHandlers.notification.indexOf(callback);
            if (idx > -1) eventHandlers.notification.splice(idx, 1);
        };
    },

    /**
     * Subscribe to installation list updates.
     */
    onInstallationListUpdated(callback: EventCallback<{ installationId: number; accountLogin: string }>): () => void {
        eventHandlers.installationListUpdated.push(callback);
        return () => {
            const idx = eventHandlers.installationListUpdated.indexOf(callback);
            if (idx > -1) eventHandlers.installationListUpdated.splice(idx, 1);
        };
    },

    /**
     * Subscribe to code review update events.
     */
    onReviewUpdate(callback: EventCallback<ReviewUpdateMessage>): () => void {
        eventHandlers.reviewUpdate.push(callback);
        return () => {
            const idx = eventHandlers.reviewUpdate.indexOf(callback);
            if (idx > -1) eventHandlers.reviewUpdate.splice(idx, 1);
        };
    },

    /**
     * Subscribe to CI/CD status update events.
     */
    onCiCdUpdate(callback: EventCallback<CiCdUpdateMessage>): () => void {
        eventHandlers.cicdUpdate.push(callback);
        return () => {
            const idx = eventHandlers.cicdUpdate.indexOf(callback);
            if (idx > -1) eventHandlers.cicdUpdate.splice(idx, 1);
        };
    },

    /**
     * Get current connection state.
     */
    getState(): signalR.HubConnectionState | null {
        return connection?.state ?? null;
    },

    /**
     * Check if connected.
     */
    isConnected(): boolean {
        return connection?.state === signalR.HubConnectionState.Connected;
    },
};

export default signalRService;

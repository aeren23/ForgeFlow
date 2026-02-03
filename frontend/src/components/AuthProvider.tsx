import { useEffect } from 'react';
import { useAuthStore } from '../store/authStore';
import { signalRService } from '../services/signalRService';
import { useNotificationStore } from '../store/notificationStore';

interface AuthProviderProps {
    children: React.ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
    const { isAuthenticated, accessToken } = useAuthStore();
    const setConnected = useNotificationStore((s) => s.setConnected);

    useEffect(() => {
        if (isAuthenticated && accessToken) {
            // Start SignalR connection when authenticated
            signalRService.start()
                .then(() => setConnected(true))
                .catch((err) => {
                    console.error('[AuthProvider] SignalR connection failed:', err);
                    setConnected(false);
                });
        } else {
            // Stop SignalR connection when logged out
            signalRService.stop()
                .then(() => setConnected(false));
        }

        return () => {
            // Cleanup on unmount
            signalRService.stop();
        };
    }, [isAuthenticated, accessToken, setConnected]);

    return <>{children}</>;
}

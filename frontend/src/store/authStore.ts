import { create } from 'zustand';
import { jwtDecode } from 'jwt-decode';

interface User {
    id: string;
    email: string;
    fullName?: string;
    isSystemAdmin?: boolean;
}

interface CustomJwtPayload {
    sub: string;
    email: string;
    fullName: string;
    isSystemAdmin: string; // serialized boolean
    [key: string]: any;
}

interface AuthState {
    accessToken: string | null;
    refreshToken: string | null;
    user: User | null;
    isAuthenticated: boolean;

    // Actions
    setTokens: (accessToken: string, refreshToken: string) => void;
    // setUser: (user: User) => void; // Deprecated with JWT sync
    login: (accessToken: string, refreshToken: string) => void;
    logout: () => void;
}

const getUserFromToken = (token: string | null): User | null => {
    if (!token) return null;
    try {
        const decoded = jwtDecode<CustomJwtPayload>(token);
        // Handle "true"/"false" string or boolean if changed later
        const isSystemAdmin = String(decoded.isSystemAdmin).toLowerCase() === 'true';

        return {
            id: decoded.sub,
            email: decoded.email,
            fullName: decoded.fullName,
            isSystemAdmin: isSystemAdmin
        };
    } catch (e) {
        console.error("Failed to decode token", e);
        return null;
    }
};

export const useAuthStore = create<AuthState>((set) => {
    const initialToken = localStorage.getItem('accessToken');
    const initialRefreshToken = localStorage.getItem('refreshToken');
    const initialUser = getUserFromToken(initialToken);

    return {
        accessToken: initialToken,
        refreshToken: initialRefreshToken,
        user: initialUser,
        isAuthenticated: !!initialToken && !!initialUser,

        setTokens: (accessToken, refreshToken) => {
            localStorage.setItem('accessToken', accessToken);
            localStorage.setItem('refreshToken', refreshToken);
            const user = getUserFromToken(accessToken);
            set({ accessToken, refreshToken, user, isAuthenticated: !!user });
        },

        // setUser: (user) => set({ user }), // No longer manually setting user from profile fetch

        login: (accessToken, refreshToken) => {
            localStorage.setItem('accessToken', accessToken);
            localStorage.setItem('refreshToken', refreshToken);
            const user = getUserFromToken(accessToken);
            set({ accessToken, refreshToken, user, isAuthenticated: !!user });
        },

        logout: () => {
            localStorage.removeItem('accessToken');
            localStorage.removeItem('refreshToken');
            set({ accessToken: null, refreshToken: null, user: null, isAuthenticated: false });
        },
    };
});

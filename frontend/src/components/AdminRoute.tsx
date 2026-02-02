import { Navigate, Outlet } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

interface AdminRouteProps {
    children?: React.ReactNode;
}

export function AdminRoute({ children }: AdminRouteProps) {
    // Use individual selectors with stable references to prevent re-renders
    const user = useAuthStore((state) => state.user);
    const isAuthenticated = useAuthStore((state) => state.isAuthenticated);

    if (!isAuthenticated) {
        console.warn("AdminRoute: Not authenticated");
        return <Navigate to="/login" replace />;
    }

    // Safety check: If user object is missing but we are authenticated, 
    // we can't verify admin status, so we block access.
    if (!user) {
        console.warn("AdminRoute: User object is null");
        return <Navigate to="/dashboard" replace />;
    }

    // DEBUG: Show why we are deciding what we are deciding
    if (user.isSystemAdmin !== true) {
        console.warn("AdminRoute: User is not system admin", user);
        // Redirect non-admin users to dashboard
        return <Navigate to="/dashboard" replace />;
    }

    return children ? <>{children}</> : <Outlet />;
}

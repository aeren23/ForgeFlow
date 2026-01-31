import { Flame, LogOut, ShieldCheck } from 'lucide-react';
import { useAuthStore } from '../../store/authStore';
import { toast } from '../../store/uiStore';

export function TopHeader() {
    const logout = useAuthStore((state) => state.logout);
    const user = useAuthStore((state) => state.user);

    const handleLogout = () => {
        logout();
        toast.info('You have been logged out.');
        window.location.href = '/login';
    };

    return (
        <header className="bg-surface border-b border-muted/20">
            <div className="max-w-7xl mx-auto px-4 py-4 flex items-center justify-between">
                <div className="flex items-center gap-3">
                    <div className="w-10 h-10 rounded-xl bg-primary/20 flex items-center justify-center">
                        <Flame className="w-5 h-5 text-primary" />
                    </div>
                    <span className="text-xl font-bold text-text">ForgeFlow</span>
                </div>

                <div className="flex items-center gap-4">
                    {user?.isSystemAdmin && (
                        <a
                            href="/admin"
                            className="hidden md:flex items-center gap-2 px-3 py-2 rounded-lg bg-red-500/10 hover:bg-red-500/20 text-red-500 hover:text-red-600 transition-all border border-red-500/20"
                        >
                            <ShieldCheck className="w-4 h-4" />
                            <span className="text-sm font-medium">System Admin</span>
                        </a>
                    )}
                    <span className="text-sm text-muted hidden md:inline">{user?.email}</span>

                    <div className="flex items-center gap-2">
                        <a
                            href="/profile"
                            className="flex items-center gap-2 px-3 py-2 rounded-lg bg-surface hover:bg-primary/20 text-muted hover:text-primary transition-all border border-muted/20"
                        >
                            <span className="text-sm font-medium">Profile</span>
                        </a>

                        <button
                            onClick={handleLogout}
                            className="flex items-center gap-2 px-3 py-2 rounded-lg bg-surface hover:bg-error/20 text-muted hover:text-error transition-all border border-muted/20"
                        >
                            <LogOut className="w-4 h-4" />
                        </button>
                    </div>
                </div>
            </div>
        </header>
    );
}

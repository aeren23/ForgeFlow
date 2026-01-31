import { Outlet, NavLink, useNavigate } from 'react-router-dom';
import { LayoutDashboard, Users, Layers, LogOut, ShieldCheck } from 'lucide-react';
import { useAuthStore } from '../store/authStore';

export function AdminLayout() {
    const logout = useAuthStore(state => state.logout);
    const navigate = useNavigate();

    const handleLogout = () => {
        logout();
        navigate('/login');
    };

    return (
        <div className="min-h-screen bg-background flex text-text font-sans selection:bg-primary/20">
            {/* Sidebar */}
            <aside className="w-64 bg-surface border-r border-muted/20 flex flex-col fixed h-full z-10 transition-all duration-300">
                <div className="p-6 border-b border-muted/10">
                    <div className="flex items-center gap-3">
                        <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-red-600 to-red-600 flex items-center justify-center shadow-lg shadow-red-500/20">
                            <ShieldCheck className="w-5 h-5 text-white" />
                        </div>
                        <div>
                            <h1 className="font-bold text-lg bg-gradient-to-r from-red-500 to-orange-500 bg-clip-text text-transparent">
                                ForgeFlow
                            </h1>
                            <span className="text-xs font-mono text-red-500 font-bold tracking-wider">ADMIN</span>
                        </div>
                    </div>
                </div>

                <nav className="flex-1 p-4 space-y-1 overflow-y-auto custom-scrollbar">
                    <NavLink
                        to="/admin"
                        end
                        className={({ isActive }) =>
                            `flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all duration-200 group
                            ${isActive
                                ? 'bg-red-500/10 text-red-500 shadow-sm border border-red-500/20'
                                : 'text-muted hover:text-text hover:bg-muted/10'}`
                        }
                    >
                        <LayoutDashboard className="w-4 h-4" />
                        Dashboard
                    </NavLink>

                    <NavLink
                        to="/admin/projects"
                        className={({ isActive }) =>
                            `flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all duration-200 group
                            ${isActive
                                ? 'bg-red-500/10 text-red-500 shadow-sm border border-red-500/20'
                                : 'text-muted hover:text-text hover:bg-muted/10'}`
                        }
                    >
                        <Layers className="w-4 h-4" />
                        Projects
                    </NavLink>

                    <NavLink
                        to="/admin/users"
                        className={({ isActive }) =>
                            `flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all duration-200 group
                            ${isActive
                                ? 'bg-red-500/10 text-red-500 shadow-sm border border-red-500/20'
                                : 'text-muted hover:text-text hover:bg-muted/10'}`
                        }
                    >
                        <Users className="w-4 h-4" />
                        User Management
                    </NavLink>
                </nav>

                <div className="p-4 border-t border-muted/10">
                    <button
                        onClick={handleLogout}
                        className="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium text-muted hover:text-error hover:bg-error/10 w-full transition-all duration-200"
                    >
                        <LogOut className="w-4 h-4" />
                        Sign Out
                    </button>
                </div>
            </aside>

            {/* Main Content */}
            <main className="flex-1 ml-64 p-8 overflow-y-auto">
                <Outlet />
            </main>
        </div>
    );
}

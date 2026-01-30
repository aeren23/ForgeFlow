import { Outlet, NavLink, useParams } from 'react-router-dom';
import { TopHeader } from '../components/layout/TopHeader';
import { LayoutDashboard, Settings, ListTodo } from 'lucide-react';

export function ProjectLayout() {
    const { key } = useParams();

    return (
        <div className="min-h-screen bg-background flex flex-col">
            <TopHeader />
            <div className="flex-1 flex overflow-hidden">
                {/* Project Sidebar */}
                <aside className="w-64 bg-surface border-r border-muted/20 flex flex-col">
                    <div className="p-4 border-b border-muted/20">
                        <div className="flex items-center gap-2 text-sm text-muted">
                            <span className="font-mono bg-muted/20 px-1.5 rounded">{key}</span>
                            <span>Project Workspace</span>
                        </div>
                    </div>

                    <nav className="flex-1 p-4 space-y-1">
                        <NavLink
                            to={`/project/${key}/board`}
                            className={({ isActive }) =>
                                `flex items-center gap-3 px-3 py-2 rounded-lg transition-colors ${isActive
                                    ? 'bg-primary/10 text-primary'
                                    : 'text-muted hover:text-text hover:bg-muted/10'
                                }`
                            }
                        >
                            <LayoutDashboard className="w-4 h-4" />
                            <span className="font-medium">Board</span>
                        </NavLink>

                        <NavLink
                            to={`/project/${key}/backlog`}
                            className={({ isActive }) =>
                                `flex items-center gap-3 px-3 py-2 rounded-lg transition-colors ${isActive
                                    ? 'bg-primary/10 text-primary'
                                    : 'text-muted hover:text-text hover:bg-muted/10'
                                }`
                            }
                        >
                            <ListTodo className="w-4 h-4" />
                            <span className="font-medium">Backlog</span>
                        </NavLink>

                        <div className="pt-4 mt-4 border-t border-muted/20">
                            <NavLink
                                to={`/project/${key}/settings`}
                                className={({ isActive }) =>
                                    `flex items-center gap-3 px-3 py-2 rounded-lg transition-colors ${isActive
                                        ? 'bg-primary/10 text-primary'
                                        : 'text-muted hover:text-text hover:bg-muted/10'
                                    }`
                                }
                            >
                                <Settings className="w-4 h-4" />
                                <span className="font-medium">Settings</span>
                            </NavLink>
                        </div>
                    </nav>
                </aside>

                {/* Main Content */}
                <main className="flex-1 overflow-auto bg-background p-6">
                    <Outlet />
                </main>
            </div>
        </div>
    );
}

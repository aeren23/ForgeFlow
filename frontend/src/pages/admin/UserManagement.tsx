import { useEffect, useState } from 'react';
import { Search, Loader2, Shield, Ban, CheckCircle, UserX } from 'lucide-react';
import { getAllUsers, toggleUserBan, type UserDto } from '../../services/api';
import { toast } from '../../store/uiStore';
import { debounce } from 'lodash';

export function UserManagement() {
    const [users, setUsers] = useState<UserDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [totalCount, setTotalCount] = useState(0);
    const [page, setPage] = useState(1);
    const [searchTerm, setSearchTerm] = useState('');
    const [actionLoading, setActionLoading] = useState<string | null>(null);

    const loadUsers = async (p: number, term: string) => {
        setLoading(true);
        try {
            const response = await getAllUsers(p, 20, term);
            setUsers(response.data.items);
            setTotalCount(response.data.totalCount);
        } catch (error) {
            toast.error('Failed to load users.');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadUsers(page, searchTerm);
    }, [page]);

    const debouncedSearch = debounce((term: string) => {
        setPage(1); // Reset to first page on search
        loadUsers(1, term);
    }, 500);

    const handleSearch = (e: React.ChangeEvent<HTMLInputElement>) => {
        setSearchTerm(e.target.value);
        debouncedSearch(e.target.value);
    };

    const handleToggleBan = async (user: UserDto) => {
        if (!window.confirm(`Are you sure you want to ${user.isActive ? 'ban' : 'unban'} ${user.userName}?`)) return;

        setActionLoading(user.id);
        try {
            const response = await toggleUserBan(user.id);
            toast.success(response.data.message);
            // Update local state
            setUsers(users.map(u => u.id === user.id ? { ...u, isActive: response.data.isActive } : u));
        } catch (error) {
            toast.error('Failed to update user status.');
        } finally {
            setActionLoading(null);
        }
    };

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-text">User Management</h1>
                    <p className="text-muted text-sm">Manage system users and access</p>
                </div>
            </div>

            <div className="bg-surface border border-muted/20 rounded-xl overflow-hidden shadow-sm">
                <div className="p-4 border-b border-muted/10 flex items-center justify-between gap-4">
                    <div className="relative flex-1 max-w-md">
                        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted" />
                        <input
                            type="text"
                            placeholder="Search users..."
                            onChange={handleSearch}
                            className="w-full bg-background border border-muted/20 rounded-lg pl-10 pr-4 py-2 text-text focus:outline-none focus:border-primary/50"
                        />
                    </div>
                </div>

                <div className="overflow-x-auto">
                    <table className="w-full text-left text-sm">
                        <thead className="bg-muted/5 text-muted font-medium border-b border-muted/10">
                            <tr>
                                <th className="px-6 py-3">User</th>
                                <th className="px-6 py-3">Email</th>
                                <th className="px-6 py-3">Role</th>
                                <th className="px-6 py-3">Status</th>
                                <th className="px-6 py-3">Joined</th>
                                <th className="px-6 py-3 text-right">Actions</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-muted/10">
                            {loading ? (
                                <tr>
                                    <td colSpan={6} className="px-6 py-8 text-center text-muted">
                                        <Loader2 className="w-6 h-6 animate-spin mx-auto mb-2" />
                                        Loading users...
                                    </td>
                                </tr>
                            ) : users.length === 0 ? (
                                <tr>
                                    <td colSpan={6} className="px-6 py-8 text-center text-muted">No users found.</td>
                                </tr>
                            ) : (
                                users.map(user => (
                                    <tr key={user.id} className="hover:bg-muted/5 transition-colors">
                                        <td className="px-6 py-4 font-medium text-text">
                                            <div className="flex items-center gap-3">
                                                <div className="w-8 h-8 rounded-full bg-primary/10 flex items-center justify-center text-primary font-bold">
                                                    {user.userName.substring(0, 2).toUpperCase()}
                                                </div>
                                                {user.userName}
                                            </div>
                                        </td>
                                        <td className="px-6 py-4 text-muted">{user.email}</td>
                                        <td className="px-6 py-4">
                                            {user.isSystemAdmin ? (
                                                <span className="inline-flex items-center gap-1 px-2 py-1 rounded bg-red-500/10 text-red-500 text-xs font-medium border border-red-500/20">
                                                    <Shield className="w-3 h-3" /> Admin
                                                </span>
                                            ) : (
                                                <span className="text-muted">User</span>
                                            )}
                                        </td>
                                        <td className="px-6 py-4">
                                            {user.isActive ? (
                                                <span className="inline-flex items-center gap-1 text-green-500 text-xs font-medium">
                                                    <CheckCircle className="w-3 h-3" /> Active
                                                </span>
                                            ) : (
                                                <span className="inline-flex items-center gap-1 text-red-500 text-xs font-medium">
                                                    <UserX className="w-3 h-3" /> Banned
                                                </span>
                                            )}
                                        </td>
                                        <td className="px-6 py-4 text-muted">
                                            {user.createdAtUtc ? new Date(user.createdAtUtc).toLocaleDateString() : '-'}
                                        </td>
                                        <td className="px-6 py-4 text-right">
                                            <button
                                                onClick={() => handleToggleBan(user)}
                                                disabled={actionLoading === user.id}
                                                className={`p-2 rounded-lg transition-colors ${user.isActive
                                                    ? 'text-red-500 hover:bg-red-500/10'
                                                    : 'text-green-500 hover:bg-green-500/10'
                                                    }`}
                                                title={user.isActive ? "Ban User" : "Activate User"}
                                            >
                                                {actionLoading === user.id ? (
                                                    <Loader2 className="w-4 h-4 animate-spin" />
                                                ) : user.isActive ? (
                                                    <Ban className="w-4 h-4" />
                                                ) : (
                                                    <CheckCircle className="w-4 h-4" />
                                                )}
                                            </button>
                                        </td>
                                    </tr>
                                ))
                            )}
                        </tbody>
                    </table>
                </div>

                {/* Pagination (Simple) */}
                <div className="px-6 py-4 border-t border-muted/10 flex items-center justify-between text-sm text-muted">
                    <div>
                        Showing {users.length} of {totalCount} users
                    </div>
                    <div className="flex gap-2">
                        <button
                            onClick={() => setPage(p => Math.max(1, p - 1))}
                            disabled={page === 1 || loading}
                            className="px-3 py-1 bg-surface border border-muted/20 rounded hover:bg-muted/5 disabled:opacity-50"
                        >
                            Previous
                        </button>
                        <button
                            onClick={() => setPage(p => p + 1)}
                            disabled={users.length < 20 || loading}
                            className="px-3 py-1 bg-surface border border-muted/20 rounded hover:bg-muted/5 disabled:opacity-50"
                        >
                            Next
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
}

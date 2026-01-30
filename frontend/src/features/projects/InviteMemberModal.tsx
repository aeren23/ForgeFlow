import { useState, useCallback } from 'react';
import { Search, UserPlus, Loader2, X } from 'lucide-react';
import { searchUsers, addProjectMember, type UserDto } from '../../services/api';
import { toast } from '../../store/uiStore';
import { debounce } from 'lodash';

interface InviteMemberModalProps {
    projectKey: string;
    isOpen: boolean;
    onClose: () => void;
    onMemberAdded: () => void;
}

export function InviteMemberModal({ projectKey, isOpen, onClose, onMemberAdded }: InviteMemberModalProps) {
    const [searchTerm, setSearchTerm] = useState('');
    const [results, setResults] = useState<UserDto[]>([]);
    const [loading, setLoading] = useState(false);
    const [inviting, setInviting] = useState<string | null>(null);

    const debouncedSearch = useCallback(
        debounce(async (term: string) => {
            if (!term.trim()) {
                setResults([]);
                return;
            }
            setLoading(true);
            try {
                const response = await searchUsers(term);
                setResults(response.data.items);
            } catch (error) {
                console.error(error);
            } finally {
                setLoading(false);
            }
        }, 300),
        []
    );

    const handleSearch = (e: React.ChangeEvent<HTMLInputElement>) => {
        setSearchTerm(e.target.value);
        debouncedSearch(e.target.value);
    };

    const handleInvite = async (user: UserDto) => {
        setInviting(user.id);
        try {
            await addProjectMember(projectKey, user.id, 'Member');
            toast.success(`${user.userName} added to project.`);
            onMemberAdded();
        } catch (error) {
            toast.error('Failed to add member.');
        } finally {
            setInviting(null);
        }
    };

    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm animate-in fade-in duration-200">
            <div className="w-full max-w-md bg-surface border border-muted/20 rounded-2xl shadow-xl overflow-hidden flex flex-col">
                <div className="px-6 py-4 border-b border-muted/10 flex items-center justify-between bg-surface-hover">
                    <h3 className="font-semibold text-text">Invite Members</h3>
                    <button onClick={onClose} className="p-2 hover:bg-muted/10 rounded-lg transition-colors text-muted hover:text-text">
                        <X className="w-5 h-5" />
                    </button>
                </div>

                <div className="p-6 space-y-4">
                    <div className="relative">
                        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted" />
                        <input
                            type="text"
                            placeholder="Search by username or email..."
                            value={searchTerm}
                            onChange={handleSearch}
                            autoFocus
                            className="w-full bg-background border border-muted/20 rounded-lg pl-10 pr-4 py-2 text-text focus:outline-none focus:border-primary/50 focus:ring-1 focus:ring-primary/50"
                        />
                        {loading && <Loader2 className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 animate-spin text-primary" />}
                    </div>

                    <div className="space-y-2 max-h-60 overflow-y-auto">
                        {results.length === 0 && searchTerm && !loading && (
                            <div className="text-center py-4 text-muted text-sm">No users found.</div>
                        )}

                        {results.map(user => (
                            <div key={user.id} className="flex items-center justify-between p-3 bg-muted/5 rounded-lg border border-muted/10 group hover:border-primary/20 transition-all">
                                <div>
                                    <div className="font-medium text-text text-sm">{user.userName}</div>
                                    <div className="text-xs text-muted">{user.fullName || user.email}</div>
                                </div>
                                <button
                                    onClick={() => handleInvite(user)}
                                    disabled={inviting !== null}
                                    className="p-1.5 bg-primary/10 text-primary hover:bg-primary hover:text-white rounded-md transition-colors disabled:opacity-50"
                                >
                                    {inviting === user.id ? <Loader2 className="w-4 h-4 animate-spin" /> : <UserPlus className="w-4 h-4" />}
                                </button>
                            </div>
                        ))}
                    </div>
                </div>
            </div>
        </div>
    );
}

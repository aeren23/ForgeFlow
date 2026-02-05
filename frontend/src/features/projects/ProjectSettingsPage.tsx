import { useEffect, useState, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Save, Trash2, Loader2, AlertCircle, Users, UserPlus, Settings as SettingsIcon, X, Github, Link as LinkIcon, ExternalLink } from 'lucide-react';
import {
    getProject, updateProject, deleteProject, updateProjectMemberRole, removeProjectMember, getUsersBatch,
    listGitHubInstallations, listGitHubRepositories, linkProjectToRepository, getProjectRepositoryConnection, unlinkProjectRepository,
    type UpdateProjectRequest, type ProjectDto, type UserDto, type GitHubInstallation, type GitHubRepository
} from '../../services/api';
import { toast } from '../../store/uiStore';
import { InviteMemberModal } from './InviteMemberModal';
import { useProjectPermissions } from '../../hooks/useProjectPermissions';
import { confirmAction, listConfirmOwnerTransfer, showSuccess, showError } from '../../utils/sweetAlert';
import { signalRService } from '../../services/signalRService';

export function ProjectSettingsPage() {
    const { key } = useParams();
    const navigate = useNavigate();
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);



    const [activeTab, setActiveTab] = useState<'settings' | 'members' | 'github'>('settings');
    const [project, setProject] = useState<ProjectDto | null>(null);
    const [showInviteModal, setShowInviteModal] = useState(false);

    // GitHub State
    const [ghLoading, setGhLoading] = useState(false);
    const [ghConnection, setGhConnection] = useState<{ repository: string; htmlUrl?: string } | null>(null);
    const [ghInstallations, setGhInstallations] = useState<GitHubInstallation[]>([]);
    const [ghRepos, setGhRepos] = useState<GitHubRepository[]>([]);
    const [ghSelectedInstId, setGhSelectedInstId] = useState<number | null>(null);
    const [ghSelectedRepo, setGhSelectedRepo] = useState<GitHubRepository | null>(null);

    // Listen for SignalR updates
    useEffect(() => {
        const unsubscribe = signalRService.onInstallationListUpdated((msg) => {
            console.log("Installations updated via SignalR", msg);
            // Refresh list if we are on GitHub tab
            if (activeTab === 'github') {
                loadGitHubData();
                showSuccess("GitHub Installations list updated.");
            }
        });
        return () => unsubscribe();
    }, [activeTab]);

    const permissions = useProjectPermissions(project);

    const [formData, setFormData] = useState<UpdateProjectRequest>({
        name: '',
        description: '',
        repositoryUrl: '',
        techStack: [],
        projectType: 0
    });

    const [usersMap, setUsersMap] = useState<Record<string, UserDto>>({});

    // Track which user IDs we've already fetched to prevent loops
    const fetchedUserIds = useRef<Set<string>>(new Set());

    // Batch fetch users when project members change
    useEffect(() => {
        if (project?.members) {
            const missingUserIds = project.members
                .map(m => m.userId)
                .filter(id => !fetchedUserIds.current.has(id));

            if (missingUserIds.length > 0) {
                // De-duplicate IDs
                const uniqueIds = Array.from(new Set(missingUserIds));
                // Mark as fetched immediately to prevent duplicate requests
                uniqueIds.forEach(id => fetchedUserIds.current.add(id));

                getUsersBatch(uniqueIds).then(response => {
                    const newUsers = response.data;
                    setUsersMap(prev => {
                        const next = { ...prev };
                        newUsers.forEach(u => {
                            next[u.id] = u;
                        });
                        return next;
                    });
                }).catch(err => console.error("Failed to fetch user batch", err));
            }
        }
    }, [project?.members]);

    useEffect(() => {
        if (key) loadProject(key);
    }, [key]);

    // Redirect or adjust tab if permission changes (e.g. after load)
    useEffect(() => {
        if (project && !loading) {
            if (!permissions.canEditProject && (activeTab === 'settings' || activeTab === 'github')) {
                setActiveTab('members');
            }
        }
    }, [project, loading, permissions.canEditProject, activeTab]);

    // Load GitHub Data when tab changes
    useEffect(() => {
        if (activeTab === 'github' && project) {
            loadGitHubData();
        }
    }, [activeTab, project]);

    const loadGitHubData = async () => {
        setGhLoading(true);
        try {
            // 1. Check existing connection
            try {
                const connRes = await getProjectRepositoryConnection(project!.id);
                setGhConnection(connRes.data);
            } catch (e) {
                setGhConnection(null);
            }

            // 2. Load installations
            const instRes = await listGitHubInstallations();
            setGhInstallations(instRes.data);
        } catch (error) {
            console.error("Failed to load GitHub data", error);
            // toast.error("Failed to load GitHub info");
        } finally {
            setGhLoading(false);
        }
    };

    const handleInstallationChange = async (instId: number) => {
        setGhSelectedInstId(instId);
        setGhRepos([]);
        setGhSelectedRepo(null);
        if (!instId) return;

        setGhLoading(true);
        try {
            const res = await listGitHubRepositories(instId);
            setGhRepos(res.data);
        } catch (error) {
            showError("Failed to fetch repositories");
        } finally {
            setGhLoading(false);
        }
    };

    const handleLinkRepository = async () => {
        if (!ghSelectedInstId || !ghSelectedRepo || !project) return;

        const inst = ghInstallations.find(i => i.installationId == ghSelectedInstId);

        setGhLoading(true);
        try {
            await linkProjectToRepository({
                projectId: project.id,
                installationId: ghSelectedInstId,
                repositoryFullName: ghSelectedRepo.fullName,
                defaultBranch: ghSelectedRepo.defaultBranch,
                repositoryId: ghSelectedRepo.id,
                accountLogin: inst?.accountLogin,
                accountType: inst?.accountType
            });
            showSuccess("Repository linked successfully!");
            loadGitHubData(); // refresh
            setGhSelectedRepo(null);
        } catch (error) {
            showError("Failed to link repository.");
        } finally {
            setGhLoading(false);
        }
    };

    const handleUnlinkRepository = async () => {
        const confirmed = await confirmAction({
            title: 'Unlink Repository?',
            text: 'This will disconnect the project from GitHub. Issues will no longer be synced.',
            confirmButtonText: 'Yes, Unlink',
            icon: 'warning'
        });

        if (confirmed) {
            setGhLoading(true);
            try {
                await unlinkProjectRepository(project!.id);
                showSuccess("Repository unlinked.");
                setGhConnection(null);
                loadGitHubData();
            } catch (error) {
                showError("Failed to unlink.");
            } finally {
                setGhLoading(false);
            }
        }
    };


    const loadProject = async (projectKey: string) => {
        try {
            const response = await getProject(projectKey);
            const p = response.data;
            setProject(p);
            setFormData({
                name: p.name,
                description: p.description || '',
                repositoryUrl: p.repositoryUrl || '',
                techStack: p.techStack || [],
                projectType: p.projectType
            });
        } catch (error) {
            toast.error('Failed to load project details.');
        } finally {
            setLoading(false);
        }
    };

    const handleUpdate = async (e: React.FormEvent) => {
        e.preventDefault();
        setSaving(true);
        try {
            await updateProject(key!, formData);
            showSuccess('Project updated successfully.');
        } catch (error) {
            showError('Failed to update project.');
        } finally {
            setSaving(false);
        }
    };

    const handleDelete = async () => {
        const confirmed = await confirmAction({
            title: 'Delete Project?',
            text: 'Are you sure you want to delete this project? This action cannot be undone.',
            confirmButtonText: 'Yes, Delete Project',
            icon: 'warning'
        });

        if (confirmed) {
            try {
                await deleteProject(key!);
                showSuccess('Project deleted.');
                navigate('/dashboard');
            } catch (error) {
                showError('Failed to delete project.');
            }
        }
    };

    const handleRoleChange = async (userId: string, newRole: string) => {
        const member = project?.members.find(m => m.userId === userId);
        const resolvedName = usersMap[userId]?.fullName;
        const memberName = resolvedName || (member ? `User ${member.userId.substring(0, 8)}...` : 'this user');

        let confirmed = false;

        // Warning for Owner transfer
        if (newRole === 'Owner') {
            confirmed = await listConfirmOwnerTransfer(memberName);
        } else {
            // Confirmation for other roles
            confirmed = await confirmAction({
                title: 'Change Member Role?',
                text: `Are you sure you want to change ${memberName}'s role to ${newRole}?`,
                confirmButtonText: `Yes, Change to ${newRole}`,
                icon: 'question'
            });
        }

        if (!confirmed) {
            // Revert the visual change if the user cancels
            // Since the select is controlled by project state, and we haven't changed project state,
            // we force a re-render or re-fetch to ensure UI is consistent.
            // Simple re-fetch is safest to ensure sync.
            // loadProject(key!); // optional, but might be overkill. 
            // Usually, React handles this if we trigger a state update.
            return;
        }

        try {
            await updateProjectMemberRole(key!, userId, newRole);
            showSuccess('Role updated successfully.');
            loadProject(key!); // Refresh list
        } catch (error) {
            showError('Failed to update role.');
        }
    };

    const handleRemoveMember = async (userId: string) => {
        const confirmed = await confirmAction({
            title: 'Remove Member?',
            text: 'Are you sure you want to remove this member from the project?',
            confirmButtonText: 'Yes, Remove Member',
            icon: 'warning'
        });

        if (confirmed) {
            try {
                await removeProjectMember(key!, userId);
                showSuccess('Member removed.');
                loadProject(key!);
            } catch (error) {
                showError('Failed to remove member.');
            }
        }
    };

    if (loading) return <div className="p-8"><Loader2 className="w-8 h-8 animate-spin text-primary" /></div>;
    if (!project) return <div>Project not found.</div>;

    return (
        <div className="max-w-4xl">
            <div className="flex items-center justify-between mb-8">
                <div>
                    <h1 className="text-2xl font-bold text-text mb-2">{project.name}</h1>
                    <p className="text-muted text-sm">Manage project settings and members</p>
                </div>

                <div className="flex bg-muted/10 p-1 rounded-lg">
                    {permissions.canEditProject && (
                        <button
                            onClick={() => setActiveTab('settings')}
                            className={`px-4 py-2 rounded-md text-sm font-medium transition-all flex items-center gap-2
                                ${activeTab === 'settings' ? 'bg-surface shadow-sm text-text' : 'text-muted hover:text-text'}`}
                        >
                            <SettingsIcon className="w-4 h-4" />
                            Settings
                        </button>
                    )}
                    <button
                        onClick={() => setActiveTab('members')}
                        className={`px-4 py-2 rounded-md text-sm font-medium transition-all flex items-center gap-2
                            ${activeTab === 'members' ? 'bg-surface shadow-sm text-text' : 'text-muted hover:text-text'}`}
                    >
                        <Users className="w-4 h-4" />
                        Members
                    </button>
                    {permissions.canEditProject && (
                        <button
                            onClick={() => setActiveTab('github')}
                            className={`px-4 py-2 rounded-md text-sm font-medium transition-all flex items-center gap-2
                                ${activeTab === 'github' ? 'bg-surface shadow-sm text-text' : 'text-muted hover:text-text'}`}
                        >
                            <Github className="w-4 h-4" />
                            GitHub
                        </button>
                    )}
                </div>
            </div>

            {activeTab === 'settings' && permissions.canEditProject && (
                <>
                    <form onSubmit={handleUpdate} className="bg-surface border border-muted/20 rounded-xl p-6 space-y-6 mb-8">
                        <div className="space-y-2">
                            <label className="text-sm font-medium text-muted">Project Name</label>
                            <input
                                type="text"
                                value={formData.name}
                                onChange={e => setFormData({ ...formData, name: e.target.value })}
                                className="w-full bg-background border border-muted/20 rounded-lg px-4 py-2 text-text"
                            />
                        </div>

                        <div className="space-y-2">
                            <label className="text-sm font-medium text-muted">Description</label>
                            <textarea
                                value={formData.description}
                                onChange={e => setFormData({ ...formData, description: e.target.value })}
                                rows={3}
                                className="w-full bg-background border border-muted/20 rounded-lg px-4 py-2 text-text resize-none"
                            />
                        </div>

                        <div className="space-y-2">
                            <label className="text-sm font-medium text-muted">Repository URL</label>
                            <input
                                type="text"
                                value={formData.repositoryUrl}
                                onChange={e => setFormData({ ...formData, repositoryUrl: e.target.value })}
                                className="w-full bg-background border border-muted/20 rounded-lg px-4 py-2 text-text"
                            />
                        </div>

                        <div className="flex justify-end">
                            <button
                                type="submit"
                                disabled={saving}
                                className="flex items-center gap-2 px-6 py-2 bg-primary hover:bg-primary/90 text-white rounded-lg font-medium transition-all disabled:opacity-50"
                            >
                                {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <Save className="w-4 h-4" />}
                                Save Changes
                            </button>
                        </div>
                    </form>

                    {permissions.canDeleteProject && (
                        <div className="bg-error/5 border border-error/20 rounded-xl p-6">
                            <h3 className="text-lg font-semibold text-error mb-2 flex items-center gap-2">
                                <AlertCircle className="w-5 h-5" />
                                Danger Zone
                            </h3>
                            <p className="text-muted text-sm mb-4">
                                Deleting a project involves unrecoverable data loss. Please be certain.
                            </p>
                            <button
                                onClick={handleDelete}
                                className="flex items-center gap-2 px-4 py-2 bg-white border border-error text-error hover:bg-error hover:text-white rounded-lg font-medium transition-all"
                            >
                                <Trash2 className="w-4 h-4" />
                                Delete Project
                            </button>
                        </div>
                    )}
                </>
            )}

            {activeTab === 'members' && (
                <div className="bg-surface border border-muted/20 rounded-xl overflow-hidden">
                    <div className="border-b border-muted/10 p-6 flex items-center justify-between">
                        <div>
                            <h3 className="font-semibold text-text">Team Members</h3>
                            <p className="text-sm text-muted">Manage who has access to this project.</p>
                        </div>
                        {permissions.canManageMembers && (
                            <button
                                onClick={() => setShowInviteModal(true)}
                                className="flex items-center gap-2 px-4 py-2 bg-primary hover:bg-primary/90 text-white rounded-lg text-sm font-medium transition-colors"
                            >
                                <UserPlus className="w-4 h-4" />
                                Invite Member
                            </button>
                        )}
                    </div>

                    <div className="divide-y divide-muted/10">
                        {project.members && project.members.length > 0 ? (
                            project.members.map(member => (
                                <div key={member.userId} className="p-4 flex items-center justify-between hover:bg-muted/5 transition-colors">
                                    <div className="flex items-center gap-4">
                                        <div className="w-10 h-10 rounded-full bg-primary/10 flex items-center justify-center text-primary font-bold">
                                            {usersMap[member.userId]?.fullName
                                                ? usersMap[member.userId].fullName.charAt(0).toUpperCase()
                                                : member.userId.substring(0, 2).toUpperCase()}
                                        </div>
                                        <div>
                                            <div className="font-medium text-text">
                                                {usersMap[member.userId]?.fullName || `User ${member.userId.substring(0, 8)}...`}
                                            </div>
                                            <div className="text-xs text-muted">Joined {new Date(member.joinedAtUtc).toLocaleDateString()}</div>
                                        </div>
                                    </div>

                                    <div className="flex items-center gap-2">
                                        {permissions.canManageMembers && (() => {
                                            const getRoleRank = (r?: string) => {
                                                switch (r) {
                                                    case 'Owner': return 1;
                                                    case 'Admin': return 2;
                                                    case 'Member': return 3;
                                                    case 'Viewer': return 4;
                                                    default: return 5;
                                                }
                                            };
                                            const myRank = getRoleRank(project.currentUserRole);
                                            const memberRank = getRoleRank(member.role);
                                            // Owner(1) can edit everyone else (rank > 1).
                                            // Admin(2) can edit Member(3) and Viewer(4).
                                            // 1 < 2 is TRUE (Owner < Admin).
                                            // So "canEdit" means I have a "smaller" number (better rank) than them.
                                            const canEdit = myRank < memberRank;

                                            // Helper to check if a specific role option should be disabled
                                            // I cannot assign a role that is <= myRank (Higher/Equal to me)
                                            // Unless I am Owner (Rank 1), I can assign anything (except I can't assign Owner.. wait, Owner Transfer is special).
                                            // Actually, Owner Transfer is handled via "Owner" option selection + Confirmation. 
                                            // So Owner should be able to select "Owner".
                                            const isOptionDisabled = (optionRank: number) => {
                                                if (myRank === 1) return false; // Owner can select anything
                                                return optionRank <= myRank; // Cannot promote to equal or higher
                                            };

                                            return canEdit ? (
                                                <select
                                                    value={member.role}
                                                    onChange={(e) => handleRoleChange(member.userId, e.target.value)}
                                                    className="px-3 py-1 rounded-full bg-surface border border-border text-xs font-medium focus:outline-none focus:ring-2 focus:ring-primary/50"
                                                >
                                                    <option value="Owner" disabled={isOptionDisabled(1)}>Owner</option>
                                                    <option value="Admin" disabled={isOptionDisabled(2)}>Admin</option>
                                                    <option value="Member" disabled={isOptionDisabled(3)}>Member</option>
                                                    <option value="Viewer" disabled={isOptionDisabled(4)}>Viewer</option>
                                                </select>
                                            ) : (
                                                <div className="px-3 py-1 rounded-full bg-blue-500/10 text-blue-500 text-xs font-medium border border-blue-500/20 cursor-not-allowed opacity-70" title="You cannot change the role of this user (Equal or Higher Rank)">
                                                    {member.role}
                                                </div>
                                            );
                                        })()}

                                        {permissions.canManageMembers && member.role !== 'Owner' && (() => {
                                            const getRoleRank = (r?: string) => {
                                                switch (r) {
                                                    case 'Owner': return 0;
                                                    case 'Admin': return 1;
                                                    case 'Member': return 2;
                                                    case 'Viewer': return 3;
                                                    default: return 4;
                                                }
                                            };
                                            const myRank = getRoleRank(project.currentUserRole);
                                            const memberRank = getRoleRank(member.role);
                                            // Can only remove people below me
                                            const canRemove = myRank < memberRank;

                                            return canRemove ? (
                                                <button
                                                    onClick={() => handleRemoveMember(member.userId)}
                                                    className="p-1.5 text-muted hover:text-error hover:bg-error/10 rounded-full transition-colors"
                                                    title="Remove Member"
                                                >
                                                    <X className="w-4 h-4" />
                                                </button>
                                            ) : null;
                                        })()}
                                    </div>
                                </div>
                            ))
                        ) : (
                            <div className="p-12 text-center text-muted">
                                No members found. Invite someone to get started!
                            </div>
                        )}
                    </div>
                </div>

            )
            }

            {
                activeTab === 'github' && permissions.canEditProject && (
                    <div className="bg-surface border border-muted/20 rounded-xl overflow-hidden p-6">
                        <div className="flex items-center gap-4 mb-6">
                            <div className="p-3 bg-black/5 rounded-full">
                                <Github className="w-8 h-8" />
                            </div>
                            <div>
                                <h3 className="text-lg font-semibold text-text">GitHub Integration</h3>
                                <p className="text-sm text-muted">Connect your project to a GitHub repository for automated GitOps.</p>
                            </div>
                        </div>

                        {ghLoading && !ghConnection && !ghInstallations.length ? (
                            <div className="py-8 flex justify-center"><Loader2 className="animate-spin text-primary" /></div>
                        ) : ghConnection ? (
                            <div className="bg-green-500/10 border border-green-500/20 rounded-lg p-6 flex items-center justify-between">
                                <div className="flex items-center gap-3">
                                    <LinkIcon className="w-5 h-5 text-green-600" />
                                    <div>
                                        <div className="font-medium text-green-900">Connected to Repository</div>
                                        <a
                                            href={`https://github.com/${ghConnection.repository}`}
                                            target="_blank"
                                            rel="noreferrer"
                                            className="text-green-700 hover:underline flex items-center gap-1 text-sm"
                                        >
                                            {ghConnection.repository}
                                            <ExternalLink className="w-3 h-3" />
                                        </a>
                                    </div>
                                </div>
                                <button
                                    onClick={handleUnlinkRepository}
                                    className="px-4 py-2 bg-white text-error border border-error/20 hover:bg-error/5 rounded-lg text-sm font-medium transition-colors"
                                >
                                    Unlink
                                </button>
                            </div>
                        ) : (
                            <div className="space-y-6">
                                {/* Step 1: Install App */}
                                <div className="bg-muted/5 p-4 rounded-lg border border-muted/10">
                                    <h4 className="font-medium text-text mb-2">1. GitHub Configuration</h4>
                                    <div className="text-sm text-muted mb-4">
                                        Can't see your repository? Make sure the ForgeFlow App is installed on your GitHub account or organization.
                                    </div>
                                    <a
                                        href={`https://github.com/apps/ForgeFlow-Project/installations/new`}
                                        target="_blank"
                                        rel="noreferrer"
                                        className="inline-flex items-center gap-2 px-4 py-2 bg-gray-900 text-white rounded-lg hover:bg-gray-800 transition-colors text-sm font-medium"
                                    >
                                        <Github className="w-4 h-4" />
                                        Manage Installations on GitHub
                                    </a>
                                </div>

                                {/* Step 2: Select Repo */}
                                <div className="space-y-4">
                                    <h4 className="font-medium text-text">2. Link Repository</h4>

                                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                        <div>
                                            <label className="text-sm font-medium text-muted mb-1 block">Account / Organization</label>
                                            <select
                                                className="w-full bg-background border border-muted/20 rounded-lg px-3 py-2 text-text"
                                                onChange={(e) => handleInstallationChange(Number(e.target.value))}
                                                value={ghSelectedInstId || ''}
                                            >
                                                <option value="">Select Account...</option>
                                                {ghInstallations.map(inst => (
                                                    <option key={inst.id} value={inst.installationId}>
                                                        {inst.accountLogin} ({inst.accountType})
                                                    </option>
                                                ))}
                                            </select>
                                        </div>

                                        <div>
                                            <label className="text-sm font-medium text-muted mb-1 block">Repository</label>
                                            <select
                                                className="w-full bg-background border border-muted/20 rounded-lg px-3 py-2 text-text"
                                                disabled={!ghSelectedInstId || ghLoading}
                                                onChange={(e) => {
                                                    const repo = ghRepos.find(r => r.id === Number(e.target.value));
                                                    setGhSelectedRepo(repo || null);
                                                }}
                                                value={ghSelectedRepo?.id || ''}
                                            >
                                                <option value="">Select Repository...</option>
                                                {ghRepos.map(repo => (
                                                    <option key={repo.id} value={repo.id}>
                                                        {repo.fullName} {repo.private ? '(Private)' : ''}
                                                    </option>
                                                ))}
                                            </select>
                                        </div>
                                    </div>

                                    <div className="pt-2 flex justify-end">
                                        <button
                                            onClick={handleLinkRepository}
                                            disabled={!ghSelectedInstId || !ghSelectedRepo || ghLoading}
                                            className="flex items-center gap-2 px-6 py-2 bg-primary hover:bg-primary/90 text-white rounded-lg font-medium transition-all disabled:opacity-50 disabled:cursor-not-allowed"
                                        >
                                            {ghLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : <LinkIcon className="w-4 h-4" />}
                                            Connect Repository
                                        </button>
                                    </div>
                                </div>
                            </div>
                        )}
                    </div>
                )}

            {/* Invite Modal */}
            {
                key && (
                    <InviteMemberModal
                        projectKey={key}
                        isOpen={showInviteModal}
                        onClose={() => setShowInviteModal(false)}
                        onMemberAdded={() => loadProject(key)}
                    />
                )
            }
        </div >
    );
}

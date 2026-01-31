import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Save, Trash2, Loader2, AlertCircle, Users, UserPlus, Settings as SettingsIcon, X } from 'lucide-react';
import { getProject, updateProject, deleteProject, updateProjectMemberRole, removeProjectMember, type UpdateProjectRequest, type ProjectDto } from '../../services/api';
import { toast } from '../../store/uiStore';
import { InviteMemberModal } from './InviteMemberModal';
import { useProjectPermissions } from '../../hooks/useProjectPermissions';
import { confirmAction, listConfirmOwnerTransfer, showSuccess, showError } from '../../utils/sweetAlert';

export function ProjectSettingsPage() {
    const { key } = useParams();
    const navigate = useNavigate();
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [activeTab, setActiveTab] = useState<'settings' | 'members'>('settings');
    const [project, setProject] = useState<ProjectDto | null>(null);
    const [showInviteModal, setShowInviteModal] = useState(false);

    const permissions = useProjectPermissions(project);

    const [formData, setFormData] = useState<UpdateProjectRequest>({
        name: '',
        description: '',
        repositoryUrl: '',
        techStack: [],
        projectType: 0
    });

    useEffect(() => {
        if (key) loadProject(key);
    }, [key]);

    // Redirect or adjust tab if permission changes (e.g. after load)
    useEffect(() => {
        if (project && !loading) {
            if (!permissions.canEditProject && activeTab === 'settings') {
                setActiveTab('members');
            }
        }
    }, [project, loading, permissions.canEditProject]);


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
        const memberName = member ? `User ${member.userId.substring(0, 8)}...` : 'this user';

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
                </div>
            </div>

            {activeTab === 'settings' && permissions.canEditProject ? (
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
            ) : (
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
                                            {member.userId.substring(0, 2).toUpperCase()}
                                        </div>
                                        <div>
                                            <div className="font-medium text-text">User {member.userId.substring(0, 8)}...</div>
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
            )}

            {/* Invite Modal */}
            {key && (
                <InviteMemberModal
                    projectKey={key}
                    isOpen={showInviteModal}
                    onClose={() => setShowInviteModal(false)}
                    onMemberAdded={() => loadProject(key)}
                />
            )}
        </div>
    );
}

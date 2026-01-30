import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Save, Trash2, Loader2, AlertCircle, Users, UserPlus, Settings as SettingsIcon } from 'lucide-react';
import { getProject, updateProject, deleteProject, type UpdateProjectRequest, type ProjectDto } from '../../services/api';
import { toast } from '../../store/uiStore';
import { InviteMemberModal } from './InviteMemberModal';

export function ProjectSettingsPage() {
    const { key } = useParams();
    const navigate = useNavigate();
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [activeTab, setActiveTab] = useState<'settings' | 'members'>('settings');
    const [project, setProject] = useState<ProjectDto | null>(null);
    const [showInviteModal, setShowInviteModal] = useState(false);

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
            toast.success('Project updated successfully.');
        } catch (error) {
            toast.error('Failed to update project.');
        } finally {
            setSaving(false);
        }
    };

    const handleDelete = async () => {
        if (window.confirm('Are you sure you want to delete this project? This action cannot be undone.')) {
            try {
                await deleteProject(key!);
                toast.success('Project deleted.');
                navigate('/dashboard');
            } catch (error) {
                toast.error('Failed to delete project.');
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
                    <button
                        onClick={() => setActiveTab('settings')}
                        className={`px-4 py-2 rounded-md text-sm font-medium transition-all flex items-center gap-2
                            ${activeTab === 'settings' ? 'bg-surface shadow-sm text-text' : 'text-muted hover:text-text'}`}
                    >
                        <SettingsIcon className="w-4 h-4" />
                        Settings
                    </button>
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

            {activeTab === 'settings' ? (
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
                </>
            ) : (
                <div className="bg-surface border border-muted/20 rounded-xl overflow-hidden">
                    <div className="border-b border-muted/10 p-6 flex items-center justify-between">
                        <div>
                            <h3 className="font-semibold text-text">Team Members</h3>
                            <p className="text-sm text-muted">Manage who has access to this project.</p>
                        </div>
                        <button
                            onClick={() => setShowInviteModal(true)}
                            className="flex items-center gap-2 px-4 py-2 bg-primary hover:bg-primary/90 text-white rounded-lg text-sm font-medium transition-colors"
                        >
                            <UserPlus className="w-4 h-4" />
                            Invite Member
                        </button>
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
                                    <div className="px-3 py-1 rounded-full bg-blue-500/10 text-blue-500 text-xs font-medium border border-blue-500/20">
                                        {member.role}
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

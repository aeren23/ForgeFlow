import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Save, Trash2, Loader2, AlertCircle } from 'lucide-react';
import { getProject, updateProject, deleteProject, type UpdateProjectRequest } from '../../services/api';
import { toast } from '../../store/uiStore';

export function ProjectSettingsPage() {
    const { key } = useParams();
    const navigate = useNavigate();
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);

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

    return (
        <div className="max-w-3xl">
            <h1 className="text-2xl font-bold text-text mb-6">Project Settings</h1>

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
        </div>
    );
}

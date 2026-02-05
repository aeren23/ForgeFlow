import { useEffect, useState } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { Wand2, Loader2, AlertCircle, Trash2, Settings } from 'lucide-react';
import { getProject, deleteProject, type ProjectDto } from '../../services/api';
import { toast } from '../../store/uiStore';
import { ProjectBoard } from '../../features/issues/ProjectBoard';
import { AiPlanModal } from './AiPlanModal';
import { useProjectPermissions } from '../../hooks/useProjectPermissions';
import { confirmAction, showSuccess, showError } from '../../utils/sweetAlert';

export function ProjectDetailPage() {
    const { key } = useParams();
    const navigate = useNavigate();
    const [project, setProject] = useState<ProjectDto | null>(null);
    const [loading, setLoading] = useState(true);
    const [isAiModalOpen, setAiModalOpen] = useState(false);

    const permissions = useProjectPermissions(project);

    useEffect(() => {
        if (key) loadProject(key);
    }, [key]);

    const loadProject = async (projectKey: string) => {
        try {
            const response = await getProject(projectKey);
            setProject(response.data);
        } catch (error) {
            toast.error('Failed to load project details.');
        } finally {
            setLoading(false);
        }
    };

    const handleAiSuccess = () => {
        // Board logic will handle the update via real-time events
    };

    const handleDeleteProject = async () => {
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

    if (loading) return (
        <div className="flex items-center justify-center h-full">
            <Loader2 className="w-8 h-8 text-primary animate-spin" />
        </div>
    );

    if (!project) return (
        <div className="flex flex-col items-center justify-center h-full text-muted">
            <AlertCircle className="w-12 h-12 mb-4" />
            <p>Project not found.</p>
        </div>
    );

    return (
        <div>
            {/* Header */}
            <div className="flex items-center justify-between mb-8">
                <div>
                    <h1 className="text-2xl font-bold text-text">{project.name}</h1>
                    <p className="text-muted mt-1">{project.description || 'No description provided.'}</p>
                </div>
                <div className="flex items-center gap-3">
                    {/* Settings Link */}
                    {permissions.canEditProject && (
                        <Link
                            to={`/project/${project.key}/settings`}
                            className="p-2 text-muted hover:text-text transition-colors"
                            title="Project Settings"
                        >
                            <Settings className="w-5 h-5" />
                        </Link>
                    )}

                    {/* Delete Button */}
                    {permissions.canDeleteProject && (
                        <button
                            onClick={handleDeleteProject}
                            className="p-2 text-red-500 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                            title="Delete Project"
                        >
                            <Trash2 className="w-5 h-5" />
                        </button>
                    )}

                    <button
                        onClick={() => setAiModalOpen(true)}
                        className="flex items-center gap-2 px-4 py-2 bg-gradient-to-r from-purple-600 to-indigo-600 hover:from-purple-700 hover:to-indigo-700 text-white rounded-lg font-medium transition-all shadow-lg hover:shadow-primary/25"
                    >
                        <Wand2 className="w-4 h-4" />
                        Generate AI Plan
                    </button>
                </div>
            </div>

            {/* Board */}
            <div className="flex-1 h-[calc(100vh-14rem)]">
                <ProjectBoard project={project} />
            </div>

            <AiPlanModal
                isOpen={isAiModalOpen}
                onClose={() => setAiModalOpen(false)}
                projectKey={project.key}
                onSuccess={handleAiSuccess}
            />
        </div>
    );
}

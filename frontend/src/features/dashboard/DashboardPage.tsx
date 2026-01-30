import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { FolderKanban, Plus, Loader2 } from 'lucide-react';
import api from '../../services/api';
import { toast } from '../../store/uiStore';
import { AlertContainer } from '../../components/ui/Alert';
import { CreateProjectModal } from '../../features/projects/CreateProjectModal';

interface Project {
    id: string;
    key: string;
    name: string;
    description?: string;
    techStack: string[];
    issueCount: number;
    createdAtUtc: string;
}

export function DashboardPage() {
    const navigate = useNavigate();
    const [projects, setProjects] = useState<Project[]>([]);
    const [loading, setLoading] = useState(true);
    const [isCreateModalOpen, setCreateModalOpen] = useState(false);

    useEffect(() => {
        fetchProjects();
    }, []);

    const fetchProjects = async () => {
        try {
            const response = await api.get('/api/projects');
            setProjects(response.data.items || []);
        } catch (error) {
            toast.error('Failed to load projects.');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="min-h-screen bg-background">
            {/* Main Content */}
            <main className="max-w-7xl mx-auto px-4 py-8">
                <AlertContainer />

                {/* Title */}
                <div className="flex items-center justify-between mb-8">
                    <div>
                        <h1 className="text-2xl font-bold text-text">Projects</h1>
                        <p className="text-muted mt-1">Manage all your projects and issues</p>
                    </div>
                    <button
                        onClick={() => setCreateModalOpen(true)}
                        className="flex items-center gap-2 px-4 py-2 bg-primary hover:bg-primary/90 text-white rounded-lg font-medium transition-all"
                    >
                        <Plus className="w-4 h-4" />
                        New Project
                    </button>
                </div>

                {/* Project Grid */}
                {loading ? (
                    <div className="flex items-center justify-center py-20">
                        <Loader2 className="w-8 h-8 text-primary animate-spin" />
                    </div>
                ) : projects.length === 0 ? (
                    <div className="text-center py-20">
                        <FolderKanban className="w-16 h-16 text-muted/50 mx-auto mb-4" />
                        <h3 className="text-lg font-medium text-text mb-2">No projects yet</h3>
                        <p className="text-muted">Use the button above to create your first project.</p>
                    </div>
                ) : (
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                        {projects.map((project) => (
                            <div
                                key={project.id}
                                onClick={() => navigate(`/project/${project.key}`)}
                                className="bg-surface rounded-xl p-6 border border-muted/20 hover:border-primary/50 transition-all cursor-pointer group"
                            >
                                <div className="flex items-start justify-between mb-4">
                                    <div className="w-12 h-12 rounded-xl bg-primary/10 flex items-center justify-center group-hover:bg-primary/20 transition-all">
                                        <FolderKanban className="w-6 h-6 text-primary" />
                                    </div>
                                    <span className="text-xs font-mono bg-background px-2 py-1 rounded text-muted">
                                        {project.key}
                                    </span>
                                </div>

                                <h3 className="text-lg font-semibold text-text mb-2 group-hover:text-primary transition-colors">
                                    {project.name}
                                </h3>
                                <p className="text-sm text-muted line-clamp-2 mb-4">
                                    {project.description || 'No description'}
                                </p>

                                <div className="flex items-center justify-between text-sm">
                                    <span className="text-muted">{project.issueCount} issues</span>
                                    <div className="flex gap-1">
                                        {project.techStack.slice(0, 3).map((tech) => (
                                            <span
                                                key={tech}
                                                className="bg-secondary/10 text-secondary px-2 py-0.5 rounded text-xs"
                                            >
                                                {tech}
                                            </span>
                                        ))}
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                )}

                <CreateProjectModal
                    isOpen={isCreateModalOpen}
                    onClose={() => setCreateModalOpen(false)}
                    onSuccess={fetchProjects}
                />
            </main>
        </div>
    );
}

import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Wand2, Loader2, AlertCircle } from 'lucide-react';
import { getProject } from '../../services/api';
import { toast } from '../../store/uiStore';
import { ProjectBoard } from '../../features/issues/ProjectBoard';

// Temporary Project interface (should ideally be shared)
interface Project {
    id: string;
    key: string;
    name: string;
    description?: string;
    techStack: string[];
    repositoryUrl?: string;
    projectType: number;
}

export function ProjectDetailPage() {
    const { key } = useParams();
    const [project, setProject] = useState<Project | null>(null);
    const [loading, setLoading] = useState(true);

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
                <button
                    className="flex items-center gap-2 px-4 py-2 bg-gradient-to-r from-purple-600 to-indigo-600 hover:from-purple-700 hover:to-indigo-700 text-white rounded-lg font-medium transition-all shadow-lg hover:shadow-primary/25"
                >
                    <Wand2 className="w-4 h-4" />
                    Generate AI Plan
                </button>
            </div>

            {/* Board */}
            <div className="flex-1 h-[calc(100vh-14rem)]">
                <ProjectBoard />
            </div>
        </div>
    );
}

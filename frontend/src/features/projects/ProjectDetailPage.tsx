import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Wand2, Loader2, AlertCircle } from 'lucide-react';
import { getProject } from '../../services/api';
import { toast } from '../../store/uiStore';
import { ProjectBoard } from '../../features/issues/ProjectBoard';
import { AiPlanModal } from './AiPlanModal';

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
    const [isAiModalOpen, setAiModalOpen] = useState(false);

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
        // Force refresh board? 
        // ProjectBoard uses its own effect based on key. 
        // Ideally we might pass a refreshTrigger or context.
        // For now, auto-refresh might not happen instantly unless we signal Board.
        // But user can refresh page.
        // Or we can add a simple key-based refresh to ProjectBoard if we lift state.
        // Let's keep it simple: ProjectBoard handles itself, maybe we just wait.
        // Or we can modify key slightly? No. 
        // Let's trust optimistic updates or user manually refreshing for now, or use window.location.reload() for MVP.
        setTimeout(() => window.location.reload(), 2000); // Dirty but effective for ensuring Epic shows up
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
                    onClick={() => setAiModalOpen(true)}
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

            <AiPlanModal
                isOpen={isAiModalOpen}
                onClose={() => setAiModalOpen(false)}
                projectKey={project.key}
                onSuccess={handleAiSuccess}
            />
        </div>
    );
}

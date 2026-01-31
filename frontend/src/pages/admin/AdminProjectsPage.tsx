import { useState, useEffect } from 'react';
import { Layers, Search, Calendar, Users, ExternalLink, GitBranch, AlertCircle, Loader2 } from 'lucide-react';
import api, { type ProjectDto } from '../../services/api';
import { toast } from '../../store/uiStore';

export function AdminProjectsPage() {
    const [projects, setProjects] = useState<ProjectDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [search, setSearch] = useState('');

    useEffect(() => {
        fetchProjects();
    }, []);

    const fetchProjects = async () => {
        try {
            setLoading(true);
            setError(null);
            // Admin olarak çağrıldığında backend tüm projeleri döner
            const response = await api.get('/api/projects?pageSize=100');
            setProjects(response.data.items);
        } catch (err) {
            console.error('Failed to fetch projects:', err);
            setError('Failed to load projects. Please try again.');
            toast.error('Failed to load projects');
        } finally {
            setLoading(false);
        }
    };

    const filteredProjects = projects.filter(p =>
        p.name.toLowerCase().includes(search.toLowerCase()) ||
        p.key.toLowerCase().includes(search.toLowerCase())
    );

    if (loading) {
        return (
            <div className="flex items-center justify-center p-12">
                <Loader2 className="w-8 h-8 animate-spin text-primary" />
            </div>
        );
    }

    if (error) {
        return (
            <div className="flex flex-col items-center justify-center p-12 text-center">
                <div className="w-12 h-12 rounded-full bg-error/10 flex items-center justify-center mb-4">
                    <AlertCircle className="w-6 h-6 text-error" />
                </div>
                <h3 className="text-lg font-semibold text-text mb-2">Error Loading Projects</h3>
                <p className="text-muted mb-4">{error}</p>
                <button
                    onClick={fetchProjects}
                    className="px-4 py-2 bg-primary text-white rounded-lg hover:bg-primary/90 transition-colors"
                >
                    Retry
                </button>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-text mb-1">Project Management</h1>
                    <p className="text-muted">Manage all projects in the system</p>
                </div>

                <div className="relative">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted" />
                    <input
                        type="text"
                        placeholder="Search projects..."
                        value={search}
                        onChange={(e) => setSearch(e.target.value)}
                        className="pl-9 pr-4 py-2 bg-surface border border-muted/20 rounded-lg text-sm text-text placeholder:text-muted/60 focus:outline-none focus:ring-2 focus:ring-primary/50 w-full md:w-64"
                    />
                </div>
            </div>

            {/* Stats Cards */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div className="bg-surface p-4 rounded-xl border border-muted/20">
                    <div className="flex items-center justify-between mb-2">
                        <span className="text-sm text-muted">Total Projects</span>
                        <Layers className="w-4 h-4 text-primary" />
                    </div>
                    <div className="text-2xl font-bold text-text">{projects.length}</div>
                </div>

                <div className="bg-surface p-4 rounded-xl border border-muted/20">
                    <div className="flex items-center justify-between mb-2">
                        <span className="text-sm text-muted">Total Issues</span>
                        <AlertCircle className="w-4 h-4 text-warning" />
                    </div>
                    <div className="text-2xl font-bold text-text">
                        {projects.reduce((acc, p) => acc + (p.issueCount || 0), 0)}
                    </div>
                </div>

                <div className="bg-surface p-4 rounded-xl border border-muted/20">
                    <div className="flex items-center justify-between mb-2">
                        <span className="text-sm text-muted">Active Connectors</span>
                        <GitBranch className="w-4 h-4 text-success" />
                    </div>
                    <div className="text-2xl font-bold text-text">
                        {projects.filter(p => p.repositoryUrl).length}
                    </div>
                </div>
            </div>

            {/* Projects List */}
            <div className="bg-surface rounded-xl border border-muted/20 overflow-hidden">
                <div className="overflow-x-auto">
                    <table className="w-full text-left">
                        <thead>
                            <tr className="bg-muted/5 border-b border-muted/10">
                                <th className="px-6 py-4 text-xs font-semibold text-muted uppercase tracking-wider">Project</th>
                                <th className="px-6 py-4 text-xs font-semibold text-muted uppercase tracking-wider">Tech Stack</th>
                                <th className="px-6 py-4 text-xs font-semibold text-muted uppercase tracking-wider">Metrics</th>
                                <th className="px-6 py-4 text-xs font-semibold text-muted uppercase tracking-wider">Created</th>
                                <th className="px-6 py-4 text-xs font-semibold text-muted uppercase tracking-wider text-right">Actions</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-muted/10">
                            {filteredProjects.map((project) => (
                                <tr key={project.id} className="group hover:bg-muted/5 transition-colors">
                                    <td className="px-6 py-4">
                                        <div className="flex items-start gap-3">
                                            <div className="w-10 h-10 rounded-lg bg-primary/10 flex items-center justify-center flex-shrink-0">
                                                <span className="text-sm font-bold text-primary">{project.key}</span>
                                            </div>
                                            <div>
                                                <div className="font-medium text-text group-hover:text-primary transition-colors">
                                                    {project.name}
                                                </div>
                                                <div className="text-xs text-muted line-clamp-1 max-w-[200px]">
                                                    {project.description || 'No description'}
                                                </div>
                                                {project.repositoryUrl && (
                                                    <a
                                                        href={project.repositoryUrl}
                                                        target="_blank"
                                                        rel="noopener noreferrer"
                                                        className="inline-flex items-center gap-1 mt-1 text-xs text-muted hover:text-text"
                                                    >
                                                        <GitBranch className="w-3 h-3" />
                                                        {project.repositoryUrl.replace('https://github.com/', '')}
                                                    </a>
                                                )}
                                            </div>
                                        </div>
                                    </td>
                                    <td className="px-6 py-4">
                                        <div className="flex flex-wrap gap-1">
                                            {project.techStack?.slice(0, 3).map((tech) => (
                                                <span key={tech} className="px-2 py-0.5 text-xs rounded-full bg-muted/10 text-muted border border-muted/10">
                                                    {tech}
                                                </span>
                                            ))}
                                            {(project.techStack?.length || 0) > 3 && (
                                                <span className="px-2 py-0.5 text-xs rounded-full bg-muted/5 text-muted">
                                                    +{project.techStack!.length - 3}
                                                </span>
                                            )}
                                        </div>
                                    </td>
                                    <td className="px-6 py-4">
                                        <div className="flex flex-col gap-1">
                                            <div className="flex items-center gap-2 text-sm text-text">
                                                <AlertCircle className="w-4 h-4 text-muted" />
                                                <span>{project.issueCount || 0} Issues</span>
                                            </div>
                                            <div className="flex items-center gap-2 text-xs text-muted">
                                                <Users className="w-3 h-3" />
                                                <span>{project.members?.length || 1} Members</span>
                                            </div>
                                        </div>
                                    </td>
                                    <td className="px-6 py-4">
                                        <div className="flex items-center gap-2 text-sm text-text">
                                            <Calendar className="w-4 h-4 text-muted" />
                                            <span>{new Date(project.createdAtUtc).toLocaleDateString()}</span>
                                        </div>
                                    </td>
                                    <td className="px-6 py-4 text-right">
                                        <a
                                            href={`/projects/${project.key}`}
                                            className="inline-flex items-center gap-2 px-3 py-1.5 rounded-lg bg-primary/10 hover:bg-primary/20 text-primary transition-colors text-sm font-medium"
                                        >
                                            <span>View</span>
                                            <ExternalLink className="w-3 h-3" />
                                        </a>
                                    </td>
                                </tr>
                            ))}

                            {filteredProjects.length === 0 && (
                                <tr>
                                    <td colSpan={5} className="px-6 py-12 text-center text-muted">
                                        No projects found matching your search.
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    );
}

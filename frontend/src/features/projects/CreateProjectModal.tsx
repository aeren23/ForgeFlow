import { useState } from 'react';
import { X, Loader2, Code, Layout } from 'lucide-react';
import { createProject, type CreateProjectRequest } from '../../services/api';
import { toast } from '../../store/uiStore';

interface CreateProjectModalProps {
    isOpen: boolean;
    onClose: () => void;
    onSuccess: () => void;
}

export function CreateProjectModal({ isOpen, onClose, onSuccess }: CreateProjectModalProps) {
    const [loading, setLoading] = useState(false);
    const [formData, setFormData] = useState<CreateProjectRequest>({
        key: '',
        name: '',
        description: '',
        repositoryUrl: '',
        techStack: [],
        projectType: 0, // 0: Scrum, 1: Kanban
    });
    const [currentTech, setCurrentTech] = useState('');

    if (!isOpen) return null;

    const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const name = e.target.value;
        const suggestedKey = name.replace(/\s+/g, '').substring(0, 4).toUpperCase();

        setFormData(prev => ({
            ...prev,
            name,
            key: prev.key === '' || prev.key.length < 2 ? suggestedKey : prev.key
        }));
    };

    const handleTechInput = (e: React.ChangeEvent<HTMLInputElement>) => {
        const val = e.target.value;
        if (val.includes(',')) {
            const tags = val.split(',').map(t => t.trim()).filter(t => t);
            if (tags.length > 0) {
                setFormData(prev => ({
                    ...prev,
                    techStack: [...new Set([...prev.techStack, ...tags])]
                }));
            }
            setCurrentTech('');
        } else {
            setCurrentTech(val);
        }
    };

    const handleAddTech = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter' && currentTech.trim()) {
            e.preventDefault();
            if (!formData.techStack.includes(currentTech.trim())) {
                setFormData(prev => ({
                    ...prev,
                    techStack: [...prev.techStack, currentTech.trim()]
                }));
            }
            setCurrentTech('');
        }
    };

    const removeTech = (tech: string) => {
        setFormData(prev => ({
            ...prev,
            techStack: prev.techStack.filter(t => t !== tech)
        }));
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!formData.key || !formData.name) {
            toast.warning('Project Name and Key are required.');
            return;
        }

        if (formData.key.length < 2 || formData.key.length > 5) {
            toast.warning('Project Key must be between 2 and 5 characters.');
            return;
        }

        setLoading(true);
        try {
            await createProject(formData);
            toast.success('Project created successfully!');
            onSuccess();
            onClose();
        } catch (error) {
            toast.error('Failed to create project.');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm animate-in fade-in duration-200">
            <div className="w-full max-w-lg bg-surface border border-muted/20 rounded-2xl shadow-xl overflow-hidden">
                <div className="px-6 py-4 border-b border-muted/10 flex items-center justify-between bg-surface-hover">
                    <h2 className="text-xl font-bold text-text flex items-center gap-2">
                        <Layout className="w-5 h-5 text-primary" />
                        Create New Project
                    </h2>
                    <button onClick={onClose} className="text-muted hover:text-text transition-colors">
                        <X className="w-5 h-5" />
                    </button>
                </div>

                <form onSubmit={handleSubmit} className="p-6 space-y-4">
                    <div className="grid grid-cols-4 gap-4">
                        <div className="col-span-3 space-y-2">
                            <label className="text-sm font-medium text-muted">Project Name</label>
                            <input
                                type="text"
                                value={formData.name}
                                onChange={handleNameChange}
                                placeholder="E.g. ForgeFlow"
                                className="w-full bg-background border border-muted/20 rounded-lg px-4 py-2 text-text focus:outline-none focus:border-primary/50 transition-colors"
                            />
                        </div>
                        <div className="col-span-1 space-y-2">
                            <label className="text-sm font-medium text-muted">Key</label>
                            <input
                                type="text"
                                value={formData.key}
                                onChange={(e) => setFormData({ ...formData, key: e.target.value.toUpperCase() })}
                                maxLength={5}
                                placeholder="KEY"
                                className="w-full bg-background border border-muted/20 rounded-lg px-4 py-2 text-text font-mono text-center focus:outline-none focus:border-primary/50 transition-colors"
                            />
                        </div>
                    </div>

                    <div className="space-y-2">
                        <label className="text-sm font-medium text-muted">GitHub Repository (Optional)</label>
                        <input
                            type="text"
                            value={formData.repositoryUrl || ''}
                            onChange={(e) => setFormData({ ...formData, repositoryUrl: e.target.value })}
                            placeholder="https://github.com/username/repo"
                            className="w-full bg-background border border-muted/20 rounded-lg px-4 py-2 text-text focus:outline-none focus:border-primary/50 transition-colors"
                        />
                    </div>

                    <div className="space-y-2">
                        <label className="text-sm font-medium text-muted">Description</label>
                        <textarea
                            value={formData.description}
                            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                            placeholder="What is this project about?"
                            rows={3}
                            className="w-full bg-background border border-muted/20 rounded-lg px-4 py-2 text-text focus:outline-none focus:border-primary/50 transition-colors resize-none"
                        />
                    </div>

                    <div className="space-y-2">
                        <label className="text-sm font-medium text-muted flex items-center gap-2">
                            <Code className="w-4 h-4" />
                            Tech Stack
                        </label>
                        <input
                            type="text"
                            value={currentTech}
                            onChange={handleTechInput}
                            onKeyDown={handleAddTech}
                            placeholder="Type and press Enter (e.g. React, .NET)"
                            className="w-full bg-background border border-muted/20 rounded-lg px-4 py-2 text-text focus:outline-none focus:border-primary/50 transition-colors"
                        />
                        <p className="text-xs text-muted">Separate tags with commas or press Enter.</p>
                        <div className="flex flex-wrap gap-2 mt-2">
                            {formData.techStack.map(tech => (
                                <span key={tech} className="bg-primary/10 text-primary text-xs px-2 py-1 rounded-full flex items-center gap-1">
                                    {tech}
                                    <button type="button" onClick={() => removeTech(tech)} className="hover:text-error">
                                        <X className="w-3 h-3" />
                                    </button>
                                </span>
                            ))}
                        </div>
                    </div>

                    <div className="space-y-2">
                        <label className="text-sm font-medium text-muted block mb-2">Project Type</label>
                        <div className="grid grid-cols-2 gap-4">
                            <button
                                type="button"
                                onClick={() => setFormData({ ...formData, projectType: 0 })}
                                className={`p-4 rounded-xl border transition-all text-left ${formData.projectType === 0
                                    ? 'bg-primary/10 border-primary/50 ring-1 ring-primary/50'
                                    : 'bg-background border-muted/20 hover:border-muted/50'
                                    }`}
                            >
                                <span className="block font-semibold text-text mb-1">Scrum</span>
                                <span className="text-xs text-muted">Sprint-based agile methodology.</span>
                            </button>
                            <button
                                type="button"
                                onClick={() => setFormData({ ...formData, projectType: 1 })}
                                className={`p-4 rounded-xl border transition-all text-left ${formData.projectType === 1
                                    ? 'bg-primary/10 border-primary/50 ring-1 ring-primary/50'
                                    : 'bg-background border-muted/20 hover:border-muted/50'
                                    }`}
                            >
                                <span className="block font-semibold text-text mb-1">Kanban</span>
                                <span className="text-xs text-muted">Continuous flow work management.</span>
                            </button>
                        </div>
                    </div>

                    <div className="pt-4 flex items-center justify-end gap-3">
                        <button
                            type="button"
                            onClick={onClose}
                            className="px-4 py-2 rounded-lg text-muted hover:text-text hover:bg-muted/10 transition-colors"
                        >
                            Cancel
                        </button>
                        <button
                            type="submit"
                            disabled={loading}
                            className="px-6 py-2 bg-primary hover:bg-primary/90 text-white rounded-lg font-medium transition-all flex items-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                            {loading && <Loader2 className="w-4 h-4 animate-spin" />}
                            Create Project
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}

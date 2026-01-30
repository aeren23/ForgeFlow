import { useState } from 'react';
import { X, Loader2, CheckSquare } from 'lucide-react';
import { createIssue, IssueType, IssuePriority, type CreateIssueRequest } from '../../services/api';
import { toast } from '../../store/uiStore';

interface CreateIssueModalProps {
    isOpen: boolean;
    onClose: () => void;
    onSuccess: () => void;
    projectKey: string;
}

export function CreateIssueModal({ isOpen, onClose, onSuccess, projectKey }: CreateIssueModalProps) {
    const [loading, setLoading] = useState(false);
    const [formData, setFormData] = useState<Partial<CreateIssueRequest>>({
        title: '',
        description: '',
        type: IssueType.Task,
        priority: IssuePriority.Medium,
        projectKey: projectKey
    });

    if (!isOpen) return null;

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!formData.title) {
            toast.warning('Title is required.');
            return;
        }

        setLoading(true);
        try {
            await createIssue({
                ...formData,
                projectKey
            } as CreateIssueRequest);
            toast.success('Issue created!');
            onSuccess();
            onClose();
            // Reset form
            setFormData({
                title: '',
                description: '',
                type: IssueType.Task,
                priority: IssuePriority.Medium,
                projectKey: projectKey
            });
        } catch (error: any) {
            console.error(error);
            const message = error.response?.data?.title || error.response?.data || 'Failed to create issue.';
            toast.error(typeof message === 'string' ? message : JSON.stringify(message));
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm animate-in fade-in duration-200">
            <div className="w-full max-w-lg bg-surface border border-muted/20 rounded-2xl shadow-xl overflow-hidden">
                <div className="px-6 py-4 border-b border-muted/10 flex items-center justify-between bg-surface-hover">
                    <h2 className="text-xl font-bold text-text flex items-center gap-2">
                        <CheckSquare className="w-5 h-5 text-primary" />
                        New Issue
                    </h2>
                    <button onClick={onClose} className="text-muted hover:text-text transition-colors">
                        <X className="w-5 h-5" />
                    </button>
                </div>

                <form onSubmit={handleSubmit} className="p-6 space-y-4">
                    <div className="space-y-2">
                        <label className="text-sm font-medium text-muted">Summary</label>
                        <input
                            type="text"
                            value={formData.title}
                            onChange={e => setFormData({ ...formData, title: e.target.value })}
                            placeholder="What needs to be done?"
                            className="w-full bg-background border border-muted/20 rounded-lg px-4 py-2 text-text focus:outline-none focus:border-primary/50"
                            autoFocus
                        />
                    </div>

                    <div className="grid grid-cols-2 gap-4">
                        <div className="space-y-2">
                            <label className="text-sm font-medium text-muted">Type</label>
                            <select
                                value={formData.type}
                                onChange={e => setFormData({ ...formData, type: Number(e.target.value) as IssueType })}
                                className="w-full bg-background border border-muted/20 rounded-lg px-4 py-2 text-text focus:outline-none focus:border-primary/50"
                            >
                                <option value={IssueType.Task}>Task</option>
                                <option value={IssueType.Bug}>Bug</option>
                                <option value={IssueType.Feature}>Feature</option>
                                <option value={IssueType.Story}>Story</option>
                            </select>
                        </div>
                        <div className="space-y-2">
                            <label className="text-sm font-medium text-muted">Priority</label>
                            <select
                                value={formData.priority}
                                onChange={e => setFormData({ ...formData, priority: Number(e.target.value) as IssuePriority })}
                                className="w-full bg-background border border-muted/20 rounded-lg px-4 py-2 text-text focus:outline-none focus:border-primary/50"
                            >
                                <option value={IssuePriority.Low}>Low</option>
                                <option value={IssuePriority.Medium}>Medium</option>
                                <option value={IssuePriority.High}>High</option>
                                <option value={IssuePriority.Critical}>Critical</option>
                            </select>
                        </div>
                    </div>

                    <div className="space-y-2">
                        <label className="text-sm font-medium text-muted">Description</label>
                        <textarea
                            value={formData.description}
                            onChange={e => setFormData({ ...formData, description: e.target.value })}
                            placeholder="Add more details..."
                            rows={4}
                            className="w-full bg-background border border-muted/20 rounded-lg px-4 py-2 text-text focus:outline-none focus:border-primary/50 resize-none"
                        />
                    </div>

                    <div className="pt-4 flex justify-end gap-3">
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
                            className="px-6 py-2 bg-primary hover:bg-primary/90 text-white rounded-lg font-medium transition-all flex items-center gap-2"
                        >
                            {loading && <Loader2 className="w-4 h-4 animate-spin" />}
                            Create Issue
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}

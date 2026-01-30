import { useState } from 'react';
import { Wand2, Loader2, X } from 'lucide-react';
import { generateProjectAiPlan, generateAiPlan } from '../../services/api';
import { toast } from '../../store/uiStore';

interface AiPlanModalProps {
    isOpen: boolean;
    onClose: () => void;
    projectKey: string;
    onSuccess: () => void;
}

export function AiPlanModal({ isOpen, onClose, projectKey, onSuccess }: AiPlanModalProps) {
    const [planName, setPlanName] = useState('');
    const [description, setDescription] = useState('');
    const [loading, setLoading] = useState(false);

    if (!isOpen) return null;

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setLoading(true);

        try {
            const response = await generateProjectAiPlan(projectKey, {
                planName,
                description,
                bundleType: 'FullStack'
            });

            if (response.data?.epicKey) {
                await generateAiPlan(response.data.epicKey);
                toast.success('AI Plan started! Tasks will appear in "To Do" shortly.');
            } else {
                toast.warning('Plan created but AI start failed. Please recreate.');
            }

            onSuccess();
            onClose();
        } catch (error) {
            toast.error('Failed to start AI generation.');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm">
            <div className="bg-surface border border-muted/20 rounded-xl shadow-2xl w-full max-w-lg p-6 relative">
                <button
                    onClick={onClose}
                    className="absolute top-4 right-4 text-muted hover:text-text transition-colors"
                >
                    <X className="w-5 h-5" />
                </button>

                <div className="flex items-center gap-3 mb-6">
                    <div className="w-10 h-10 rounded-lg bg-gradient-to-br from-purple-500/20 to-indigo-500/20 flex items-center justify-center">
                        <Wand2 className="w-5 h-5 text-indigo-400" />
                    </div>
                    <div>
                        <h2 className="text-xl font-semibold text-text">Generate AI Plan</h2>
                        <p className="text-sm text-muted">Describe your goal, and let AI build the tasks.</p>
                    </div>
                </div>

                <form onSubmit={handleSubmit} className="space-y-4">
                    <div>
                        <label className="block text-sm font-medium text-muted mb-1">
                            Plan Name (Epic Title)
                        </label>
                        <input
                            type="text"
                            value={planName}
                            onChange={(e) => setPlanName(e.target.value)}
                            placeholder="e.g. E-Commerce MVP"
                            className="w-full bg-background border border-muted/20 rounded-lg px-4 py-2 text-text focus:outline-none focus:border-primary/50"
                            required
                        />
                    </div>

                    <div>
                        <label className="block text-sm font-medium text-muted mb-1">
                            Description (AI Prompt)
                        </label>
                        <textarea
                            value={description}
                            onChange={(e) => setDescription(e.target.value)}
                            placeholder="Describe what you want to build in detail... (e.g. A React frontend with a .NET backend for a blog system)"
                            className="w-full h-32 bg-background border border-muted/20 rounded-lg px-4 py-2 text-text focus:outline-none focus:border-primary/50 resize-none"
                            required
                        />
                    </div>

                    <div className="pt-2 flex justify-end gap-3">
                        <button
                            type="button"
                            onClick={onClose}
                            className="px-4 py-2 text-muted hover:text-text transition-colors"
                        >
                            Cancel
                        </button>
                        <button
                            type="submit"
                            disabled={loading}
                            className="flex items-center gap-2 px-6 py-2 bg-gradient-to-r from-purple-600 to-indigo-600 hover:from-purple-700 hover:to-indigo-700 text-white rounded-lg font-medium transition-all shadow-lg hover:shadow-primary/25 disabled:opacity-50"
                        >
                            {loading ? (
                                <>
                                    <Loader2 className="w-4 h-4 animate-spin" />
                                    Starting...
                                </>
                            ) : (
                                <>
                                    <Wand2 className="w-4 h-4" />
                                    Generate
                                </>
                            )}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}

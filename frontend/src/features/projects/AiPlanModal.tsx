import { useState, useEffect } from 'react';
import { Wand2, X, Play, CheckCircle2 } from 'lucide-react';
import { generateProjectAiPlan, generateAiPlan } from '../../services/api';
import { signalRService, type AiProgressMessage } from '../../services/signalRService';
import { AiLiveLogPanel } from '../../components/common/AiLiveLogPanel';
import { toast } from '../../store/uiStore';

interface AiPlanModalProps {
    isOpen: boolean;
    onClose: () => void;
    projectKey: string;
    onSuccess: () => void;
}

type GeneratorState = 'idle' | 'generating' | 'success' | 'error';

export function AiPlanModal({ isOpen, onClose, projectKey, onSuccess }: AiPlanModalProps) {
    const [planName, setPlanName] = useState('');
    const [description, setDescription] = useState('');
    const [state, setState] = useState<GeneratorState>('idle');

    // Progress State
    const [progress, setProgress] = useState<number>(0);
    const [currentMessage, setCurrentMessage] = useState<string>('');
    const [logEntries, setLogEntries] = useState<string[]>([]);
    const [requestedFiles, setRequestedFiles] = useState<string[]>([]);

    useEffect(() => {
        if (!isOpen) {
            setTimeout(() => {
                setPlanName('');
                setDescription('');
                setState('idle');
                setProgress(0);
                setCurrentMessage('');
                setLogEntries([]);
                setRequestedFiles([]);
            }, 300);
            return;
        }

        const handleProgress = (msg: AiProgressMessage) => {
            setProgress(msg.progressPercentage);
            setCurrentMessage(msg.message);
            if (msg.logEntries && msg.logEntries.length > 0) {
                // The backend sends the full list of log entries each time, so replace the array 
                // instead of appending to prevent duplicates.
                setLogEntries(msg.logEntries!);
            }
            if (msg.requestedFiles && msg.requestedFiles.length > 0) {
                setRequestedFiles(msg.requestedFiles);
            }
        };

        const handleNotification = (msg: any) => {
            if (msg.type === 'ai_plan_complete') {
                setState('success');
                toast.success('AI Plan completed! Tasks have been added to your board.');
                onSuccess();
            } else if (msg.type === 'ai_plan_failed') {
                setState('error');
                setCurrentMessage(msg.message || 'Bilinmeyen bir hata oluştu');
            }
        };

        const unsubProgress = signalRService.onAiProgress(handleProgress);
        const unsubNotification = signalRService.onNotification(handleNotification);

        return () => {
            unsubProgress();
            unsubNotification();
        };
    }, [isOpen, onSuccess]);


    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        
        setState('generating');
        setProgress(5);
        setCurrentMessage('Epic oluşturuluyor...');
        setLogEntries(['[SYSTEM] AI Plan API isteği başlatıldı.']);
        setRequestedFiles([]);

        try {
            const response = await generateProjectAiPlan(projectKey, {
                planName,
                description,
                bundleType: 'FullStack'
            });

            if (response.data?.epicKey) {
                setLogEntries(prev => [...prev, '[SYSTEM] Epic oluşturuldu, context analizi başlıyor...']);
                await generateAiPlan(response.data.epicKey);
            } else {
                setState('error');
                setCurrentMessage('Epic okunamadı. Lütfen tekrar deneyin.');
            }
        } catch (error) {
            console.error("Failed to start AI generation", error);
            setState('error');
            setCurrentMessage('API isteği başarısız oldu.');
        }
    };

    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm">
            <div className={`bg-surface border border-muted/20 rounded-xl shadow-2xl w-full max-h-[90vh] flex flex-col transition-all duration-300 ${state === 'idle' ? 'max-w-xl' : 'max-w-4xl'}`}>
                {/* Header */}
                <div className="relative px-6 py-4 border-b border-border/50 flex items-center shrink-0">
                    <button
                        onClick={onClose}
                        className="absolute top-4 right-4 text-muted hover:text-text transition-colors"
                    >
                        <X className="w-5 h-5" />
                    </button>

                    <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-lg bg-gradient-to-br from-purple-500/20 to-indigo-500/20 flex items-center justify-center border border-purple-500/20">
                            <Wand2 className="w-5 h-5 text-indigo-400" />
                        </div>
                        <div>
                            <h2 className="text-xl font-semibold text-text">Generate AI Tasks</h2>
                            <p className="text-sm text-muted">Describe your goal, and let AI build the tasks reading your repo context.</p>
                        </div>
                    </div>
                </div>

                {/* Content */}
                <div className="p-6 overflow-y-auto flex-1">
                    {state === 'idle' && (
                        <form id="ai-plan-form" onSubmit={handleSubmit} className="space-y-4">
                            <div>
                                <label className="block text-sm font-medium text-muted mb-1">
                                    Plan Name (Epic Title)
                                </label>
                                <input
                                    type="text"
                                    value={planName}
                                    onChange={(e) => setPlanName(e.target.value)}
                                    placeholder="e.g. E-Commerce Checkout Flow"
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
                                    placeholder="Describe what you want to build in detail. Mentions specific files or folders if needed so the AI can fetch them for deeper context."
                                    className="w-full h-32 bg-background border border-muted/20 rounded-lg px-4 py-2 text-text focus:outline-none focus:border-primary/50 resize-none"
                                    required
                                />
                            </div>
                        </form>
                    )}

                    {state === 'generating' && (
                        <div className="space-y-4">
                            <AiLiveLogPanel 
                                logEntries={logEntries}
                                requestedFiles={requestedFiles}
                                progressPercentage={progress}
                                message={currentMessage}
                                isComplete={false}
                            />
                        </div>
                    )}

                    {state === 'error' && (
                        <div className="space-y-6">
                            <div className="bg-error/10 border border-error/20 p-6 rounded-lg text-center">
                                <X className="w-12 h-12 text-error mx-auto mb-4" />
                                <h3 className="text-lg font-medium text-error mb-2">Generation Failed</h3>
                                <p className="text-sm text-error/80">{currentMessage || 'An unknown error occurred.'}</p>
                            </div>
                            <div className="flex justify-center">
                                <button 
                                    onClick={() => setState('idle')}
                                    className="px-6 py-2 bg-surface border border-border rounded-lg text-text hover:bg-muted/10 transition-colors"
                                >
                                    Try Again
                                </button>
                            </div>
                        </div>
                    )}

                    {state === 'success' && (
                        <div className="flex flex-col items-center justify-center space-y-4 py-8">
                            <CheckCircle2 className="w-16 h-16 text-emerald-500" />
                            <h3 className="text-xl font-medium text-emerald-500">Plan Generated Successfully!</h3>
                            <p className="text-muted text-center max-w-sm">The background AI worker successfully generated all tasks and created the Epic container. Check your Board.</p>
                        </div>
                    )}
                </div>

                {/* Footer */}
                <div className="px-6 py-4 border-t border-border/50 flex justify-end gap-3 shrink-0 bg-muted/5">
                    {state === 'idle' && (
                        <>
                            <button
                                type="button"
                                onClick={onClose}
                                className="px-4 py-2 text-muted hover:text-text transition-colors"
                            >
                                Cancel
                            </button>
                            <button
                                type="submit"
                                form="ai-plan-form"
                                className="flex items-center gap-2 px-6 py-2 bg-indigo-500 hover:bg-indigo-600 text-white rounded-lg font-medium transition-all shadow-lg hover:shadow-indigo-500/25"
                            >
                                <Play className="w-4 h-4 fill-current" />
                                Generate Tasks
                            </button>
                        </>
                    )}
                    {state === 'success' && (
                        <button
                            onClick={onClose}
                            className="px-6 py-2 bg-primary hover:bg-primary/90 text-white rounded-lg font-medium transition-colors"
                        >
                            Back to Board
                        </button>
                    )}
                </div>
            </div>
        </div>
    );
}

import { useState, useEffect } from 'react';
import { Play, X, CheckCircle2, FileCode, Check, Copy } from 'lucide-react';
import { generateWorkflow } from '../../services/api';
import { signalRService, type AiProgressMessage } from '../../services/signalRService';
import { AiLiveLogPanel } from '../../components/common/AiLiveLogPanel';
import { toast } from '../../store/uiStore';
import { showError, showSuccess } from '../../utils/sweetAlert';

interface WorkflowGeneratorModalProps {
    projectKey: string;
    isOpen: boolean;
    onClose: () => void;
    onWorkflowGenerated?: () => void;
}

type GeneratorState = 'idle' | 'generating' | 'success' | 'error';

export function WorkflowGeneratorModal({ projectKey, isOpen, onClose, onWorkflowGenerated }: WorkflowGeneratorModalProps) {
    const [state, setState] = useState<GeneratorState>('idle');
    const [provider, setProvider] = useState<'openai' | 'anthropic' | 'gemini'>('anthropic');
    
    // Progress State
    const [progress, setProgress] = useState<number>(0);
    const [currentMessage, setCurrentMessage] = useState<string>('');
    const [logEntries, setLogEntries] = useState<string[]>([]);
    const [requestedFiles, setRequestedFiles] = useState<string[]>([]);
    
    // Result State
    const [workflowYaml, setWorkflowYaml] = useState<string>('');
    const [workflowFileName, setWorkflowFileName] = useState<string>('');
    const [aiMetrics, setAiMetrics] = useState<any>(null);
    const [isCopied, setIsCopied] = useState(false);

    useEffect(() => {
        if (!isOpen) {
            // Reset state when modal closes
            setTimeout(() => {
                setState('idle');
                setProgress(0);
                setCurrentMessage('');
                setLogEntries([]);
                setRequestedFiles([]);
                setWorkflowYaml('');
                setWorkflowFileName('');
            }, 300);
            return;
        }

        // Subscribe to SignalR events when open
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
            if (msg.type === 'WorkflowGenerationCompleted') {
                setState('success');
                setWorkflowYaml(msg.data.workflowYaml);
                setWorkflowFileName(msg.data.workflowFileName);
                setAiMetrics({
                    durationMs: msg.data.durationMs,
                    promptTokens: msg.data.promptTokens,
                    completionTokens: msg.data.completionTokens,
                    provider: msg.data.usedProvider
                });
                showSuccess('Workflow başarıyla oluşturuldu!');
                if (onWorkflowGenerated) onWorkflowGenerated();
            } else if (msg.type === 'WorkflowGenerationFailed') {
                setState('error');
                showError(msg.message);
            }
        };

        const unsubProgress = signalRService.onAiProgress(handleProgress);
        const unsubNotification = signalRService.onNotification(handleNotification);

        return () => {
            unsubProgress();
            unsubNotification();
        };
    }, [isOpen]);

    const handleGenerate = async () => {
        try {
            setState('generating');
            setProgress(5);
            setCurrentMessage('GitHub analizi başlatılıyor...');
            setLogEntries(['[SYSTEM] Workflow oluşturma isteği gönderildi.']);
            setRequestedFiles([]);
            
            await generateWorkflow(projectKey, { preferredProvider: provider });
            
        } catch (error) {
            console.error("Failed to start workflow generation", error);
            setState('error');
            showError('İşlem başlatılamadı.');
        }
    };

    const handleCopyYaml = () => {
        navigator.clipboard.writeText(workflowYaml);
        setIsCopied(true);
        setTimeout(() => setIsCopied(false), 2000);
        toast.success('YAML kopyalandı.');
    };

    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
            <div className={`bg-surface w-full max-w-4xl rounded-xl shadow-2xl flex flex-col border border-border/40 max-h-[90vh] transition-all duration-300 transform scale-100 opacity-100 ${state === 'idle' ? 'max-w-xl' : ''}`}>
                
                {/* Header */}
                <div className="px-6 py-4 border-b border-border/50 flex items-center justify-between shrink-0">
                    <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-lg bg-indigo-500/10 flex items-center justify-center">
                            <Play className="w-5 h-5 text-indigo-500" />
                        </div>
                        <div>
                            <h2 className="text-lg font-semibold text-text">AI Workflow Generator</h2>
                            <p className="text-sm text-muted">GitHub Actions CI/CD pipleline otomatik oluştur</p>
                        </div>
                    </div>
                    <button onClick={onClose} className="p-2 hover:bg-muted/10 rounded-full transition-colors text-muted hover:text-text">
                        <X className="w-5 h-5" />
                    </button>
                </div>

                {/* Content */}
                <div className="p-6 overflow-y-auto flex-1">
                    
                    {state === 'idle' && (
                        <div className="space-y-6">
                            <div className="bg-indigo-500/10 border border-indigo-500/20 rounded-lg p-4 text-sm text-indigo-400">
                                <p>Yapay Zeka kod tabanınızı analiz edecek, kritik dosyaları okuyacak ve projeniz için en uygun GitHub Actions workflow YAML dosyasını üretecektir.</p>
                            </div>
                            
                            <div className="space-y-3">
                                <label className="text-sm font-medium text-text">AI Sağlayıcısı (Opsiyonel)</label>
                                <div className="grid grid-cols-3 gap-3">
                                    <button 
                                        onClick={() => setProvider('anthropic')}
                                        className={`px-4 py-3 rounded-lg border text-sm font-medium transition-all ${provider === 'anthropic' ? 'bg-indigo-500/20 border-indigo-500 text-indigo-400' : 'bg-surface border-border hover:bg-muted/5 text-muted'}`}
                                    >
                                        Claude 3.5 Sonnet
                                    </button>
                                    <button 
                                        onClick={() => setProvider('openai')}
                                        className={`px-4 py-3 rounded-lg border text-sm font-medium transition-all ${provider === 'openai' ? 'bg-indigo-500/20 border-indigo-500 text-indigo-400' : 'bg-surface border-border hover:bg-muted/5 text-muted'}`}
                                    >
                                        GPT-4o
                                    </button>
                                    <button 
                                        onClick={() => setProvider('gemini')}
                                        className={`px-4 py-3 rounded-lg border text-sm font-medium transition-all ${provider === 'gemini' ? 'bg-indigo-500/20 border-indigo-500 text-indigo-400' : 'bg-surface border-border hover:bg-muted/5 text-muted'}`}
                                    >
                                        Gemini 1.5 Pro
                                    </button>
                                </div>
                            </div>
                            
                            <button 
                                onClick={handleGenerate}
                                className="w-full flex items-center justify-center gap-2 py-3 bg-indigo-500 hover:bg-indigo-600 text-white rounded-lg font-medium transition-all"
                            >
                                <Play className="w-4 h-4 fill-current" />
                                Analizi Başlat ve Oluştur
                            </button>
                        </div>
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
                                <h3 className="text-lg font-medium text-error mb-2">Oluşturma Başarısız</h3>
                                <p className="text-sm text-error/80">{currentMessage || 'Bilinmeyen bir hata oluştu.'}</p>
                            </div>
                            <div className="flex justify-center">
                                <button 
                                    onClick={() => setState('idle')}
                                    className="px-6 py-2 bg-surface border border-border rounded-lg text-text hover:bg-muted/10 transition-colors"
                                >
                                    Tekrar Dene
                                </button>
                            </div>
                        </div>
                    )}

                    {state === 'success' && (
                        <div className="space-y-6">
                            <div className="flex items-start justify-between bg-emerald-500/10 border border-emerald-500/20 p-4 rounded-lg">
                                <div className="flex items-center gap-3">
                                    <CheckCircle2 className="w-6 h-6 text-emerald-500" />
                                    <div>
                                        <h3 className="font-medium text-emerald-500">Workflow başarıyla oluşturuldu</h3>
                                        <p className="text-xs text-emerald-500/80 mt-1 flex items-center gap-4">
                                            <span>Dosya: <strong className="font-mono text-emerald-400">{workflowFileName}</strong></span>
                                            {aiMetrics && (
                                                <span className="flex items-center gap-3">
                                                    <span className="opacity-50">|</span>
                                                    <span>Süre: {(aiMetrics.durationMs / 1000).toFixed(1)}s</span>
                                                    <span>API: {aiMetrics.provider}</span>
                                                    <span title="Tokens">🪙 {aiMetrics.promptTokens + aiMetrics.completionTokens}</span>
                                                </span>
                                            )}
                                        </p>
                                    </div>
                                </div>
                                <button 
                                    onClick={handleCopyYaml}
                                    className="flex items-center gap-2 px-3 py-1.5 bg-emerald-500/20 hover:bg-emerald-500/30 text-emerald-400 rounded-md transition-colors text-sm font-medium"
                                >
                                    {isCopied ? <Check className="w-4 h-4" /> : <Copy className="w-4 h-4" />}
                                    {isCopied ? 'Kopyalandı' : 'Kopyala'}
                                </button>
                            </div>

                            <div className="bg-gray-950 rounded-lg border border-gray-800 overflow-hidden">
                                <div className="bg-gray-900 px-4 py-2 border-b border-gray-800 flex items-center gap-2 text-sm text-gray-400 font-mono">
                                    <FileCode className="w-4 h-4 text-indigo-400" />
                                    {workflowFileName}
                                </div>
                                <div className="p-4 overflow-x-auto">
                                    <pre className="text-sm text-gray-300 font-mono">
                                        <code>{workflowYaml}</code>
                                    </pre>
                                </div>
                            </div>
                        </div>
                    )}
                </div>
                
                {state === 'success' && (
                    <div className="px-6 py-4 border-t border-border/50 bg-muted/5 flex justify-end shrink-0">
                        <button 
                            onClick={onClose}
                            className="px-6 py-2 bg-primary hover:bg-primary/90 text-white rounded-lg font-medium transition-colors"
                        >
                            Kapat
                        </button>
                    </div>
                )}
            </div>
        </div>
    );
}

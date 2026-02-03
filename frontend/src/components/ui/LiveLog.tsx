import { useEffect, useRef } from 'react';
import { Terminal, X, Minimize2, Maximize2 } from 'lucide-react';
import { useNotificationStore } from '../../store/notificationStore';
import { signalRService } from '../../services/signalRService';

interface LiveLogProps {
    isOpen: boolean;
    onClose: () => void;
    isMinimized?: boolean;
    onToggleMinimize?: () => void;
}

export function LiveLog({ isOpen, onClose, isMinimized = false, onToggleMinimize }: LiveLogProps) {
    const { aiLogs, addAiLog, clearAiLogs, isConnected } = useNotificationStore();
    const logContainerRef = useRef<HTMLDivElement>(null);

    // Subscribe to AI progress events
    useEffect(() => {
        const unsubscribe = signalRService.onAiProgress((msg) => {
            addAiLog(msg);
        });

        return () => unsubscribe();
    }, [addAiLog]);

    // Auto-scroll to bottom when new logs arrive
    useEffect(() => {
        if (logContainerRef.current && !isMinimized) {
            logContainerRef.current.scrollTop = logContainerRef.current.scrollHeight;
        }
    }, [aiLogs, isMinimized]);

    if (!isOpen) return null;

    return (
        <div className={`fixed bottom-0 right-4 z-50 transition-all duration-300 ${isMinimized ? 'h-10' : 'h-80'
            } w-96 bg-gray-900 rounded-t-lg shadow-2xl border border-gray-700 flex flex-col`}>
            {/* Header */}
            <div
                className="flex items-center justify-between px-3 py-2 bg-gray-800 rounded-t-lg cursor-pointer border-b border-gray-700"
                onClick={onToggleMinimize}
            >
                <div className="flex items-center gap-2">
                    <Terminal size={16} className="text-green-400" />
                    <span className="text-sm font-medium text-gray-200">Live Log</span>
                    {isConnected ? (
                        <span className="w-2 h-2 bg-green-500 rounded-full animate-pulse" title="Connected" />
                    ) : (
                        <span className="w-2 h-2 bg-red-500 rounded-full" title="Disconnected" />
                    )}
                </div>
                <div className="flex items-center gap-1">
                    <button
                        onClick={(e) => {
                            e.stopPropagation();
                            clearAiLogs();
                        }}
                        className="p-1 text-gray-400 hover:text-gray-200 transition-colors"
                        title="Clear logs"
                    >
                        <span className="text-xs">Clear</span>
                    </button>
                    <button
                        onClick={(e) => {
                            e.stopPropagation();
                            onToggleMinimize?.();
                        }}
                        className="p-1 text-gray-400 hover:text-gray-200 transition-colors"
                    >
                        {isMinimized ? <Maximize2 size={14} /> : <Minimize2 size={14} />}
                    </button>
                    <button
                        onClick={(e) => {
                            e.stopPropagation();
                            onClose();
                        }}
                        className="p-1 text-gray-400 hover:text-red-400 transition-colors"
                    >
                        <X size={14} />
                    </button>
                </div>
            </div>

            {/* Log content */}
            {!isMinimized && (
                <div
                    ref={logContainerRef}
                    className="flex-1 overflow-y-auto p-3 font-mono text-xs space-y-1"
                >
                    {aiLogs.length === 0 ? (
                        <div className="text-gray-500 text-center py-4">
                            Waiting for AI activity...
                        </div>
                    ) : (
                        aiLogs.map((log, index) => (
                            <div key={index} className="flex items-start gap-2">
                                <span className={`flex-shrink-0 px-1.5 py-0.5 rounded text-xs font-semibold ${log.isComplete
                                        ? 'bg-green-900/50 text-green-400'
                                        : 'bg-blue-900/50 text-blue-400'
                                    }`}>
                                    {log.progressPercentage}%
                                </span>
                                <span className="text-gray-300 break-all">{log.message}</span>
                            </div>
                        ))
                    )}
                </div>
            )}
        </div>
    );
}

import React, { useEffect, useRef } from 'react';
import { FileCode2, ChevronDown, ChevronRight, Activity } from 'lucide-react';

interface AiLiveLogPanelProps {
    logEntries: string[];
    requestedFiles: string[];
    progressPercentage: number;
    message: string;
    isComplete: boolean;
}

export const AiLiveLogPanel: React.FC<AiLiveLogPanelProps> = ({
    logEntries,
    requestedFiles,
    progressPercentage,
    message,
    isComplete
}) => {
    const logsEndRef = useRef<HTMLDivElement>(null);
    const [isFilesExpanded, setIsFilesExpanded] = React.useState(false);

    // Auto-scroll to bottom of logs
    useEffect(() => {
        logsEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [logEntries]);

    return (
        <div className="bg-gray-900 rounded-lg overflow-hidden border border-gray-800 flex flex-col h-full max-h-96">
            {/* Header / Status Bar */}
            <div className="bg-gray-800 px-4 py-3 flex items-center justify-between border-b border-gray-700 shrink-0">
                <div className="flex items-center space-x-3">
                    {isComplete ? (
                        <div className="w-2 h-2 rounded-full bg-emerald-500 shadow-[0_0_8px_rgba(16,185,129,0.8)]" />
                    ) : (
                        <div className="w-2 h-2 rounded-full bg-indigo-500 animate-pulse shadow-[0_0_8px_rgba(99,102,241,0.8)]" />
                    )}
                    <span className="text-gray-200 font-medium text-sm flex items-center">
                        <Activity className="w-4 h-4 mr-2 text-indigo-400" />
                        {message || (isComplete ? 'İşlem tamamlandı' : 'İşleniyor...')}
                    </span>
                </div>
                <div className="flex items-center space-x-3">
                    <span className="text-indigo-400 font-mono text-sm">{progressPercentage}%</span>
                    <div className="w-24 h-1.5 bg-gray-700 rounded-full overflow-hidden">
                        <div 
                            className={`h-full transition-all duration-500 ease-out ${isComplete ? 'bg-emerald-500' : 'bg-indigo-500'}`}
                            style={{ width: `${progressPercentage}%` }}
                        />
                    </div>
                </div>
            </div>

            {/* Requested Files Collapse (Phase 1 Code Context Bridge) */}
            {requestedFiles && requestedFiles.length > 0 && (
                <div className="border-b border-gray-800 bg-gray-800/50 shrink-0">
                    <button 
                        onClick={() => setIsFilesExpanded(!isFilesExpanded)}
                        className="w-full px-4 py-2 flex items-center justify-between text-sm text-gray-400 hover:text-gray-200 hover:bg-gray-800 transition-colors"
                    >
                        <div className="flex items-center">
                            <FileCode2 className="w-4 h-4 mr-2 text-emerald-500" />
                            <span>AI Code Context: {requestedFiles.length} dosya yüklendi</span>
                        </div>
                        {isFilesExpanded ? <ChevronDown className="w-4 h-4" /> : <ChevronRight className="w-4 h-4" />}
                    </button>
                    
                    {isFilesExpanded && (
                        <div className="px-4 py-2 bg-gray-950/50 max-h-32 overflow-y-auto">
                            <ul className="space-y-1">
                                {requestedFiles.map((file, idx) => (
                                    <li key={idx} className="text-xs font-mono text-emerald-400/80 flex items-center before:content-['>'] before:mr-2 before:text-gray-600">
                                        {file}
                                    </li>
                                ))}
                            </ul>
                        </div>
                    )}
                </div>
            )}

            {/* Live Terminal Logs */}
            <div className="p-4 bg-gray-950 font-mono text-xs overflow-y-auto flex-1 h-48 custom-scrollbar">
                {logEntries.length === 0 ? (
                    <div className="text-gray-600 flex items-center justify-center h-full italic">
                        Logs waiting...
                    </div>
                ) : (
                    <div className="space-y-2">
                        {logEntries.map((log, index) => {
                            // Basic log styling based on keywords
                            let colorClass = 'text-gray-300';
                            if (log.includes('[ERROR]') || log.includes('❌')) colorClass = 'text-red-400';
                            else if (log.includes('[SUCCESS]') || log.includes('✅')) colorClass = 'text-emerald-400';
                            else if (log.includes('[WARNING]') || log.includes('⚠️')) colorClass = 'text-amber-400';
                            else if (log.includes('[AI]')) colorClass = 'text-fuchsia-400';
                            else if (log.includes('[GITHUB]') || log.includes('[REPO]')) colorClass = 'text-blue-400';
                            else if (log.includes('[PHASE-1]') || log.includes('[PHASE-2]')) colorClass = 'text-indigo-400';

                            return (
                                <div key={index} className={`flex items-start ${colorClass}`}>
                                    <span className="text-gray-600 mr-2 shrink-0">{'>'}</span>
                                    <span className="break-all whitespace-pre-wrap">{log}</span>
                                </div>
                            );
                        })}
                        <div ref={logsEndRef} />
                    </div>
                )}
            </div>
            
            <style>{`
                .custom-scrollbar::-webkit-scrollbar { width: 6px; }
                .custom-scrollbar::-webkit-scrollbar-track { background: rgba(17, 24, 39, 1); }
                .custom-scrollbar::-webkit-scrollbar-thumb { background: rgba(75, 85, 99, 1); border-radius: 3px; }
                .custom-scrollbar::-webkit-scrollbar-thumb:hover { background: rgba(107, 114, 128, 1); }
            `}</style>
        </div>
    );
};

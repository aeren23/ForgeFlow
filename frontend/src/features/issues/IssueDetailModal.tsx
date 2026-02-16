import { useState, useEffect } from 'react';
import { X, CheckSquare, User, Clock, ArrowUp, ArrowRight, AlertOctagon, FileCode, Trash2, ChevronDown, Loader2, MessageSquare, Bot, RefreshCw } from 'lucide-react';
import { getIssues, deleteIssue, assignIssue, getCodeReviews, IssueType, IssuePriority, IssueStatus, IssueStatusLabels, type Issue, type UserDto, type CodeReviewDto } from '../../services/api';
import { signalRService, type ReviewUpdateMessage } from '../../services/signalRService';
import { toast } from '../../store/uiStore';
import type { ProjectPermissions } from '../../hooks/useProjectPermissions';
import { confirmAction, confirmBranchCreation, showSuccess, showError } from '../../utils/sweetAlert';

interface ProjectMember {
    userId: string;
    role: string;
}

interface IssueDetailModalProps {
    isOpen: boolean;
    onClose: () => void;
    issue: Issue | null;
    permissions?: ProjectPermissions;
    usersMap?: Record<string, UserDto>;
    projectMembers?: ProjectMember[];
    onDeleteSuccess?: () => void;
    onAssignSuccess?: () => void;
}

type TabType = 'details' | 'reviews';

// Parsed review content structure
interface ReviewContent {
    summary?: string;
    overallRating?: string;
    codeQualityScore?: number;
    planComplianceScore?: number | null;
    findings?: ReviewFinding[];
    metrics?: {
        filesReviewed?: number;
        totalAdditions?: number;
        totalDeletions?: number;
        criticalIssues?: number;
        warnings?: number;
        suggestions?: number;
    };
}

interface ReviewFinding {
    severity: string;
    category: string;
    file?: string;
    line?: number;
    message: string;
    suggestion?: string;
}

export function IssueDetailModal({ isOpen, onClose, issue, permissions, usersMap, projectMembers, onDeleteSuccess, onAssignSuccess }: IssueDetailModalProps) {
    const [subTasks, setSubTasks] = useState<Issue[]>([]);
    const [loadingSubTasks, setLoadingSubTasks] = useState(false);
    const [showAssigneeDropdown, setShowAssigneeDropdown] = useState(false);
    const [assigningTo, setAssigningTo] = useState<string | null>(null);
    const [activeTab, setActiveTab] = useState<TabType>('details');
    const [reviews, setReviews] = useState<CodeReviewDto[]>([]);
    const [loadingReviews, setLoadingReviews] = useState(false);
    const [expandedReview, setExpandedReview] = useState<number | null>(null);
    const [prStatuses, setPrStatuses] = useState<Record<number, string>>({});

    useEffect(() => {
        if (isOpen && issue && issue.type === IssueType.Epic) {
            fetchSubTasks();
        } else {
            setSubTasks([]);
        }
        // Reset tab when modal opens
        setActiveTab('details');
        setReviews([]);
        setExpandedReview(null);
    }, [isOpen, issue]);

    useEffect(() => {
        if (activeTab === 'reviews' && issue && reviews.length === 0 && !loadingReviews) {
            fetchReviews();
        }
    }, [activeTab, issue]);

    // Subscribe to real-time PR status updates
    useEffect(() => {
        if (!issue || activeTab !== 'reviews') return;

        const unsubscribe = signalRService.onReviewUpdate((msg: ReviewUpdateMessage) => {
            if (msg.issueKey === issue.key) {
                console.log('[Reviews] Received PR status update:', msg);
                setPrStatuses(prev => ({
                    ...prev,
                    [msg.pullNumber]: msg.prStatus
                }));
            }
        });

        // Initialize statuses from fetched reviews
        if (reviews.length > 0) {
            const initialStatuses: Record<number, string> = {};
            reviews.forEach(r => {
                const prNum = getPrNumber(r.correlationId);
                if (prNum) {
                    const num = parseInt(prNum.replace('#', ''));
                    try {
                        if (r.metadata) {
                            const meta = JSON.parse(r.metadata);
                            if (meta.PrStatus) {
                                initialStatuses[num] = meta.PrStatus;
                            }
                        }
                    } catch (e) { }
                }
            });
            setPrStatuses(prev => ({ ...prev, ...initialStatuses }));
        }

        return () => {
            unsubscribe();
        };
    }, [issue, activeTab, reviews]);

    const fetchSubTasks = async () => {
        if (!issue) return;
        setLoadingSubTasks(true);
        try {
            const projectKey = issue.key.split('-')[0];
            const response = await getIssues(projectKey, issue.id);
            setSubTasks(response.data.items || []);
        } catch (error) {
            toast.error('Failed to load sub-tasks.');
        } finally {
            setLoadingSubTasks(false);
        }
    };

    const fetchReviews = async () => {
        if (!issue) return;
        setLoadingReviews(true);
        try {
            console.log('[Reviews] Fetching reviews for', { issueKey: issue.key, projectId: issue.projectId });
            const response = await getCodeReviews(issue.key, issue.projectId);
            console.log('[Reviews] API response:', response.status, response.data);
            setReviews(response.data || []);
        } catch (error: any) {
            console.error('[Reviews] Failed to fetch reviews:', error?.response?.status, error?.response?.data, error?.message);
        } finally {
            setLoadingReviews(false);
        }
    };

    const handleDelete = async () => {
        if (!issue) return;

        const confirmed = await confirmAction({
            title: 'Delete Issue?',
            text: 'Are you sure you want to delete this issue? This action cannot be undone.',
            confirmButtonText: 'Yes, Delete Issue',
            icon: 'warning'
        });

        if (confirmed) {
            try {
                await deleteIssue(issue.key);
                showSuccess('Issue deleted successfully.');
                onClose();
                if (onDeleteSuccess) onDeleteSuccess();
            } catch (error) {
                showError('Failed to delete issue.');
            }
        }
    };

    if (!isOpen || !issue) return null;

    const getTypeIcon = (type: IssueType) => {
        switch (type) {
            case IssueType.Bug: return <AlertOctagon className="w-5 h-5 text-red-500" />;
            case IssueType.Feature: return <CheckSquare className="w-5 h-5 text-green-500" />;
            case IssueType.Story: return <FileCode className="w-5 h-5 text-blue-500" />;
            case IssueType.Epic: return <AlertOctagon className="w-5 h-5 text-purple-500" />;
            default: return <CheckSquare className="w-5 h-5 text-blue-400" />;
        }
    };

    const parseReviewContent = (contentJson: string): ReviewContent | null => {
        try {
            let content = contentJson.trim();
            // Strip markdown code block wrapper if present
            if (content.startsWith('```')) {
                const firstNewline = content.indexOf('\n');
                if (firstNewline > 0) content = content.substring(firstNewline + 1);
                if (content.endsWith('```')) content = content.substring(0, content.length - 3).trim();
            }
            return JSON.parse(content);
        } catch {
            return null;
        }
    };

    const getRatingBadge = (rating?: string) => {
        switch (rating) {
            case 'APPROVE':
                return (
                    <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-emerald-500/15 text-emerald-400 border border-emerald-500/20">
                        ✅ Approved
                    </span>
                );
            case 'REQUEST_CHANGES':
                return (
                    <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-red-500/15 text-red-400 border border-red-500/20">
                        ❌ Changes Requested
                    </span>
                );
            default:
                return (
                    <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-blue-500/15 text-blue-400 border border-blue-500/20">
                        💬 Comment
                    </span>
                );
        }
    };

    const getSeverityStyle = (severity: string) => {
        switch (severity) {
            case 'error': return { emoji: '🔴', bg: 'bg-red-500/10', border: 'border-red-500/20', text: 'text-red-400' };
            case 'warning': return { emoji: '🟡', bg: 'bg-yellow-500/10', border: 'border-yellow-500/20', text: 'text-yellow-400' };
            case 'suggestion': return { emoji: '🔵', bg: 'bg-blue-500/10', border: 'border-blue-500/20', text: 'text-blue-400' };
            case 'suggestion': return { emoji: '🔵', bg: 'bg-blue-500/10', border: 'border-blue-500/20', text: 'text-blue-400' };
            default: return { emoji: '🟢', bg: 'bg-emerald-500/10', border: 'border-emerald-500/20', text: 'text-emerald-400' };
        }
    };

    const getPrStatusBadge = (status: string) => {
        const s = status.toLowerCase();
        switch (s) {
            case 'merged':
                return (
                    <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold bg-violet-500/15 text-violet-400 border border-violet-500/20 uppercase tracking-wider">
                        🟣 Merged
                    </span>
                );
            case 'closed':
                return (
                    <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold bg-red-500/15 text-red-400 border border-red-500/20 uppercase tracking-wider">
                        🔴 Closed
                    </span>
                );
            case 'open':
            default:
                return (
                    <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold bg-green-500/15 text-green-400 border border-green-500/20 uppercase tracking-wider">
                        🟢 Open
                    </span>
                );
        }
    };

    const getScoreColor = (score: number) => {
        if (score >= 90) return 'text-emerald-400';
        if (score >= 70) return 'text-yellow-400';
        if (score >= 50) return 'text-orange-400';
        return 'text-red-400';
    };

    const getScoreGradient = (score: number) => {
        if (score >= 90) return 'from-emerald-500 to-emerald-400';
        if (score >= 70) return 'from-yellow-500 to-yellow-400';
        if (score >= 50) return 'from-orange-500 to-orange-400';
        return 'from-red-500 to-red-400';
    };

    const formatTimeAgo = (dateStr: string) => {
        const date = new Date(dateStr);
        const now = new Date();
        const diffMs = now.getTime() - date.getTime();
        const diffMin = Math.floor(diffMs / 60000);
        if (diffMin < 1) return 'Just now';
        if (diffMin < 60) return `${diffMin}m ago`;
        const diffH = Math.floor(diffMin / 60);
        if (diffH < 24) return `${diffH}h ago`;
        const diffD = Math.floor(diffH / 24);
        return `${diffD}d ago`;
    };

    const getPrNumber = (correlationId?: string) => {
        if (!correlationId) return null;
        const match = correlationId.match(/pr-(\d+)/);
        return match ? `#${match[1]}` : null;
    };

    const renderReviewCard = (review: CodeReviewDto, index: number) => {
        const parsed = parseReviewContent(review.contentJson);
        const prNumber = getPrNumber(review.correlationId);
        const isExpanded = expandedReview === index;

        if (!parsed) {
            return (
                <div key={index} className="p-4 bg-muted/5 border border-muted/10 rounded-xl">
                    <p className="text-sm text-muted italic">Unable to parse review content.</p>
                </div>
            );
        }

        return (
            <div key={index} className="group bg-surface border border-muted/15 rounded-xl overflow-hidden hover:border-primary/30 transition-all duration-300">
                {/* Card Header */}
                <button
                    onClick={() => setExpandedReview(isExpanded ? null : index)}
                    className="w-full px-5 py-4 flex items-center justify-between hover:bg-muted/5 transition-colors"
                >
                    <div className="flex items-center gap-3">
                        <div className="w-9 h-9 rounded-lg bg-gradient-to-br from-violet-500/20 to-blue-500/20 flex items-center justify-center border border-violet-500/20">
                            <Bot className="w-4.5 h-4.5 text-violet-400" />
                        </div>
                        <div className="text-left">
                            <div className="flex items-center gap-2">
                                {prNumber && (
                                    <span className="text-sm font-semibold text-text">PR {prNumber}</span>
                                )}
                                {prNumber && (() => {
                                    const num = parseInt(prNumber.replace('#', ''));
                                    const status = prStatuses[num] || 'open'; // Default to open
                                    return getPrStatusBadge(status);
                                })()}
                                {getRatingBadge(parsed.overallRating)}
                            </div>
                            <div className="flex items-center gap-2 mt-0.5">
                                <span className="text-xs text-muted">{formatTimeAgo(review.createdAtUtc)}</span>
                                {parsed.codeQualityScore != null && (
                                    <>
                                        <span className="text-xs text-muted/50">•</span>
                                        <span className={`text-xs font-medium ${getScoreColor(parsed.codeQualityScore)}`}>
                                            Quality: {parsed.codeQualityScore}/100
                                        </span>
                                    </>
                                )}
                            </div>
                        </div>
                    </div>
                    <ChevronDown className={`w-4 h-4 text-muted transition-transform duration-200 ${isExpanded ? 'rotate-180' : ''}`} />
                </button>

                {/* Expanded Content */}
                {isExpanded && (
                    <div className="px-5 pb-5 space-y-4 animate-in slide-in-from-top-2 duration-200">
                        {/* Score Bar */}
                        {parsed.codeQualityScore != null && (
                            <div className="flex items-center gap-4">
                                <div className="flex-1">
                                    <div className="flex items-center justify-between mb-1.5">
                                        <span className="text-xs font-medium text-muted">Code Quality</span>
                                        <span className={`text-sm font-bold ${getScoreColor(parsed.codeQualityScore)}`}>
                                            {parsed.codeQualityScore}
                                        </span>
                                    </div>
                                    <div className="h-2 bg-muted/10 rounded-full overflow-hidden">
                                        <div
                                            className={`h-full rounded-full bg-gradient-to-r ${getScoreGradient(parsed.codeQualityScore)} transition-all duration-500`}
                                            style={{ width: `${parsed.codeQualityScore}%` }}
                                        />
                                    </div>
                                </div>
                                {parsed.planComplianceScore != null && (
                                    <div className="flex-1">
                                        <div className="flex items-center justify-between mb-1.5">
                                            <span className="text-xs font-medium text-muted">Plan Compliance</span>
                                            <span className={`text-sm font-bold ${getScoreColor(parsed.planComplianceScore)}`}>
                                                {parsed.planComplianceScore}
                                            </span>
                                        </div>
                                        <div className="h-2 bg-muted/10 rounded-full overflow-hidden">
                                            <div
                                                className={`h-full rounded-full bg-gradient-to-r ${getScoreGradient(parsed.planComplianceScore)} transition-all duration-500`}
                                                style={{ width: `${parsed.planComplianceScore}%` }}
                                            />
                                        </div>
                                    </div>
                                )}
                            </div>
                        )}

                        {/* Summary */}
                        {parsed.summary && (
                            <div className="bg-muted/5 p-3.5 rounded-lg border border-muted/10">
                                <p className="text-sm text-text/80 leading-relaxed">{parsed.summary}</p>
                            </div>
                        )}

                        {/* Findings */}
                        {parsed.findings && parsed.findings.length > 0 && (
                            <div className="space-y-2">
                                <h4 className="text-xs font-semibold text-muted uppercase tracking-wider">
                                    Findings ({parsed.findings.length})
                                </h4>
                                {parsed.findings.map((finding, fIdx) => {
                                    const style = getSeverityStyle(finding.severity);
                                    return (
                                        <div key={fIdx} className={`p-3 rounded-lg border ${style.bg} ${style.border}`}>
                                            <div className="flex items-start gap-2">
                                                <span className="text-sm mt-0.5">{style.emoji}</span>
                                                <div className="flex-1 min-w-0">
                                                    <div className="flex items-center gap-2 mb-1">
                                                        <span className={`text-xs font-semibold uppercase ${style.text}`}>
                                                            {finding.severity}
                                                        </span>
                                                        {finding.file && (
                                                            <code className="text-xs text-muted bg-muted/10 px-1.5 py-0.5 rounded">
                                                                {finding.file}{finding.line ? `:${finding.line}` : ''}
                                                            </code>
                                                        )}
                                                    </div>
                                                    <p className="text-sm text-text/80">{finding.message}</p>
                                                    {finding.suggestion && finding.suggestion !== 'N/A' && (
                                                        <div className="mt-2 pl-3 border-l-2 border-primary/30">
                                                            <p className="text-xs text-muted">💡 {finding.suggestion}</p>
                                                        </div>
                                                    )}
                                                </div>
                                            </div>
                                        </div>
                                    );
                                })}
                            </div>
                        )}

                        {/* Metrics */}
                        {parsed.metrics && (
                            <div className="flex flex-wrap gap-3 pt-2 border-t border-muted/10">
                                {parsed.metrics.filesReviewed != null && (
                                    <span className="text-xs text-muted">📁 {parsed.metrics.filesReviewed} files</span>
                                )}
                                {parsed.metrics.totalAdditions != null && (
                                    <span className="text-xs text-emerald-400">+{parsed.metrics.totalAdditions}</span>
                                )}
                                {parsed.metrics.totalDeletions != null && (
                                    <span className="text-xs text-red-400">-{parsed.metrics.totalDeletions}</span>
                                )}
                                {parsed.metrics.criticalIssues != null && parsed.metrics.criticalIssues > 0 && (
                                    <span className="text-xs text-red-400">🔴 {parsed.metrics.criticalIssues} critical</span>
                                )}
                                {parsed.metrics.warnings != null && parsed.metrics.warnings > 0 && (
                                    <span className="text-xs text-yellow-400">🟡 {parsed.metrics.warnings} warnings</span>
                                )}
                                {parsed.metrics.suggestions != null && parsed.metrics.suggestions > 0 && (
                                    <span className="text-xs text-blue-400">🔵 {parsed.metrics.suggestions} suggestions</span>
                                )}
                            </div>
                        )}
                    </div>
                )}
            </div>
        );
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm animate-in fade-in duration-200">
            <div className="w-full max-w-4xl bg-surface border border-muted/20 rounded-2xl shadow-xl overflow-hidden flex flex-col max-h-[90vh]">

                {/* Header */}
                <div className="px-6 py-4 border-b border-muted/10 flex items-center justify-between bg-surface-hover">
                    <div className="flex items-center gap-3">
                        {getTypeIcon(issue.type)}
                        <span className="text-sm font-mono text-muted">{issue.key}</span>
                    </div>
                    <div className="flex items-center gap-2">
                        {permissions?.canDeleteIssue && (
                            <button
                                onClick={handleDelete}
                                className="p-2 hover:bg-red-500/10 rounded-lg transition-colors text-muted hover:text-red-500 mr-2"
                                title="Delete Issue"
                            >
                                <Trash2 className="w-5 h-5" />
                            </button>
                        )}
                        <button onClick={onClose} className="p-2 hover:bg-muted/10 rounded-lg transition-colors text-muted hover:text-text">
                            <X className="w-5 h-5" />
                        </button>
                    </div>
                </div>

                {/* Tab Bar */}
                <div className="px-6 border-b border-muted/10 flex gap-1">
                    <button
                        onClick={() => setActiveTab('details')}
                        className={`px-4 py-3 text-sm font-medium border-b-2 transition-colors ${activeTab === 'details'
                            ? 'border-primary text-primary'
                            : 'border-transparent text-muted hover:text-text hover:border-muted/30'
                            }`}
                    >
                        <span className="flex items-center gap-2">
                            <FileCode className="w-4 h-4" />
                            Details
                        </span>
                    </button>
                    <button
                        onClick={() => setActiveTab('reviews')}
                        className={`px-4 py-3 text-sm font-medium border-b-2 transition-colors ${activeTab === 'reviews'
                            ? 'border-primary text-primary'
                            : 'border-transparent text-muted hover:text-text hover:border-muted/30'
                            }`}
                    >
                        <span className="flex items-center gap-2">
                            <Bot className="w-4 h-4" />
                            AI Reviews
                            {reviews.length > 0 && (
                                <span className="px-1.5 py-0.5 text-xs rounded-full bg-primary/15 text-primary font-semibold">
                                    {reviews.length}
                                </span>
                            )}
                        </span>
                    </button>
                </div>

                {/* Content - Scrollable */}
                <div className="flex-1 overflow-y-auto p-6">
                    <div className="flex flex-col md:flex-row gap-8">
                        {/* Left Column: Main Content */}
                        <div className="flex-1 space-y-6">
                            {activeTab === 'details' && (
                                <>
                                    <div>
                                        <h1 className="text-2xl font-bold text-text mb-4">{issue.title}</h1>

                                        <div className="space-y-2">
                                            <h3 className="text-sm font-semibold text-muted uppercase tracking-wider">Description</h3>
                                            <div className="bg-muted/5 p-4 rounded-lg border border-muted/10 text-text/80 leading-relaxed whitespace-pre-wrap">
                                                {issue.description || 'No description provided.'}
                                            </div>
                                        </div>
                                    </div>

                                    {/* Sub-Tasks Section (For Epics) */}
                                    {issue.type === IssueType.Epic && (
                                        <div className="space-y-3">
                                            <div className="flex items-center justify-between">
                                                <h3 className="text-sm font-semibold text-muted uppercase tracking-wider">
                                                    Child Issues ({subTasks.length})
                                                </h3>
                                                <button
                                                    onClick={fetchSubTasks}
                                                    className="text-xs text-primary hover:underline"
                                                    disabled={loadingSubTasks}
                                                >
                                                    {loadingSubTasks ? 'Loading...' : 'Refresh'}
                                                </button>
                                            </div>

                                            <div className="space-y-2">
                                                {subTasks.length === 0 && !loadingSubTasks ? (
                                                    <div className="text-sm text-muted italic">No sub-tasks found.</div>
                                                ) : (
                                                    subTasks.map(sub => (
                                                        <div key={sub.id} className="flex items-center gap-3 p-3 bg-card border border-muted/20 rounded-lg hover:border-primary/30 transition-colors">
                                                            {getTypeIcon(sub.type)}
                                                            <div className="flex-1">
                                                                <div className="flex items-center gap-2">
                                                                    <span className="text-xs font-mono text-muted">{sub.key}</span>
                                                                    <span className="text-sm font-medium text-text">{sub.title}</span>
                                                                </div>
                                                            </div>
                                                            <div className={`px-2 py-1 rounded text-xs font-medium 
                                                                ${sub.status === IssueStatus.Done ? 'bg-green-500/10 text-green-500' : 'bg-muted/10 text-muted'}`}>
                                                                {IssueStatusLabels[sub.status]}
                                                            </div>
                                                        </div>
                                                    ))
                                                )}
                                                {loadingSubTasks && (
                                                    <div className="py-2 text-center text-muted text-sm">Loading sub-tasks...</div>
                                                )}
                                            </div>
                                        </div>
                                    )}
                                </>
                            )}

                            {activeTab === 'reviews' && (
                                <div className="space-y-4">
                                    <div className="flex items-center justify-between">
                                        <h3 className="text-lg font-semibold text-text flex items-center gap-2">
                                            <Bot className="w-5 h-5 text-violet-400" />
                                            AI Code Reviews
                                        </h3>
                                        <button
                                            onClick={fetchReviews}
                                            className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-muted hover:text-text bg-muted/5 hover:bg-muted/10 rounded-lg border border-muted/10 transition-colors"
                                            disabled={loadingReviews}
                                        >
                                            <RefreshCw className={`w-3 h-3 ${loadingReviews ? 'animate-spin' : ''}`} />
                                            Refresh
                                        </button>
                                    </div>

                                    {loadingReviews && reviews.length === 0 ? (
                                        <div className="space-y-3">
                                            {[1, 2].map(i => (
                                                <div key={i} className="h-24 bg-muted/5 border border-muted/10 rounded-xl animate-pulse" />
                                            ))}
                                        </div>
                                    ) : reviews.length === 0 ? (
                                        <div className="flex flex-col items-center justify-center py-12 text-center">
                                            <div className="w-16 h-16 rounded-2xl bg-violet-500/10 flex items-center justify-center mb-4">
                                                <MessageSquare className="w-8 h-8 text-violet-400" />
                                            </div>
                                            <h4 className="text-sm font-medium text-text mb-1">No AI Reviews Yet</h4>
                                            <p className="text-xs text-muted max-w-xs">
                                                AI code reviews are automatically generated when a Pull Request is opened for this issue.
                                            </p>
                                        </div>
                                    ) : (
                                        <div className="space-y-3">
                                            {reviews.map((review, index) => renderReviewCard(review, index))}
                                        </div>
                                    )}
                                </div>
                            )}
                        </div>

                        {/* Right Column: Metadata */}
                        <div className="w-full md:w-80 space-y-6">
                            <div className="bg-muted/5 p-4 rounded-lg border border-muted/10 space-y-4">
                                <div className="space-y-1">
                                    <label className="text-xs font-medium text-muted">Status</label>
                                    <div className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium bg-blue-500/10 text-blue-500 border border-blue-500/20">
                                        {IssueStatusLabels[issue.status]}
                                    </div>
                                </div>

                                <div className="space-y-1">
                                    <label className="text-xs font-medium text-muted">Priority</label>
                                    <div className="flex items-center gap-2 text-sm text-text">
                                        {issue.priority === IssuePriority.High ? <ArrowUp className="w-4 h-4 text-orange-500" /> : <ArrowRight className="w-4 h-4 text-yellow-500" />}
                                        {Object.keys(IssuePriority).find(k => IssuePriority[k as keyof typeof IssuePriority] === issue.priority)}
                                    </div>
                                </div>

                                <div className="space-y-1 relative">
                                    <label className="text-xs font-medium text-muted">Assignee</label>
                                    {permissions?.canAssignIssue && projectMembers && projectMembers.length > 0 ? (
                                        <div className="relative">
                                            <button
                                                onClick={() => setShowAssigneeDropdown(!showAssigneeDropdown)}
                                                className="w-full flex items-center justify-between gap-2 px-3 py-2 bg-background border border-muted/20 rounded-lg hover:border-primary/50 transition-colors text-sm text-text"
                                                disabled={assigningTo !== null}
                                            >
                                                <div className="flex items-center gap-2">
                                                    <div className="w-6 h-6 rounded-full bg-primary/20 flex items-center justify-center text-xs text-primary">
                                                        {assigningTo !== null ? (
                                                            <Loader2 className="w-3 h-3 animate-spin" />
                                                        ) : (
                                                            <User className="w-3 h-3" />
                                                        )}
                                                    </div>
                                                    <span>
                                                        {issue.assigneeId
                                                            ? (usersMap?.[issue.assigneeId]?.fullName || `User ${issue.assigneeId.substring(0, 8)}...`)
                                                            : 'Unassigned'}
                                                    </span>
                                                </div>
                                                <ChevronDown className={`w-4 h-4 text-muted transition-transform ${showAssigneeDropdown ? 'rotate-180' : ''}`} />
                                            </button>

                                            {showAssigneeDropdown && (
                                                <div className="absolute top-full left-0 right-0 mt-1 bg-surface border border-muted/20 rounded-lg shadow-xl z-50 max-h-48 overflow-y-auto">
                                                    <button
                                                        onClick={async () => {
                                                            setAssigningTo('unassign');
                                                            setShowAssigneeDropdown(false);
                                                            try {
                                                                await assignIssue(issue.key, null);
                                                                toast.success('Issue unassigned successfully');
                                                                onAssignSuccess?.();
                                                            } catch (error) {
                                                                toast.error('Failed to unassign issue');
                                                            } finally {
                                                                setAssigningTo(null);
                                                            }
                                                        }}
                                                        className={`w-full text-left px-3 py-2 text-sm hover:bg-muted/10 transition-colors flex items-center gap-2 ${!issue.assigneeId ? 'bg-primary/10 text-primary' : 'text-text'}`}
                                                    >
                                                        <div className="w-6 h-6 rounded-full bg-muted/20 flex items-center justify-center text-xs text-muted">
                                                            <User className="w-3 h-3" />
                                                        </div>
                                                        Unassigned
                                                    </button>
                                                    {projectMembers.map(member => {
                                                        const user = usersMap?.[member.userId];
                                                        const displayName = user?.fullName || `User ${member.userId.substring(0, 8)}...`;
                                                        const isSelected = issue.assigneeId === member.userId;
                                                        return (
                                                            <button
                                                                key={member.userId}
                                                                onClick={async () => {
                                                                    setShowAssigneeDropdown(false);
                                                                    // Ask user if they want to create a branch
                                                                    const choice = await confirmBranchCreation(issue.key);
                                                                    if (choice === 'cancel') return;

                                                                    const createBranch = choice === 'branch';
                                                                    setAssigningTo(member.userId);
                                                                    try {
                                                                        await assignIssue(issue.key, member.userId, createBranch);
                                                                        toast.success(
                                                                            createBranch
                                                                                ? `Issue assigned to ${displayName} (branch will be created)`
                                                                                : `Issue assigned to ${displayName}`
                                                                        );
                                                                        onAssignSuccess?.();
                                                                    } catch (error) {
                                                                        toast.error('Failed to assign issue');
                                                                    } finally {
                                                                        setAssigningTo(null);
                                                                    }
                                                                }}
                                                                className={`w-full text-left px-3 py-2 text-sm hover:bg-muted/10 transition-colors flex items-center gap-2 ${isSelected ? 'bg-primary/10 text-primary' : 'text-text'}`}
                                                            >
                                                                <div className="w-6 h-6 rounded-full bg-primary/20 flex items-center justify-center text-xs text-primary font-medium">
                                                                    {displayName.charAt(0).toUpperCase()}
                                                                </div>
                                                                <span className="flex-1">{displayName}</span>
                                                                <span className="text-xs text-muted">{member.role}</span>
                                                            </button>
                                                        );
                                                    })}
                                                </div>
                                            )}
                                        </div>
                                    ) : (
                                        <div className="flex items-center gap-2 text-sm text-text">
                                            <div className="w-6 h-6 rounded-full bg-primary/20 flex items-center justify-center text-xs text-primary">
                                                <User className="w-3 h-3" />
                                            </div>
                                            {issue.assigneeId
                                                ? (usersMap?.[issue.assigneeId]?.fullName || `User ${issue.assigneeId.substring(0, 8)}...`)
                                                : 'Unassigned'}
                                        </div>
                                    )}
                                </div>

                                <div className="pt-4 border-t border-muted/10 space-y-2">
                                    <div className="flex items-center gap-2 text-xs text-muted">
                                        <Clock className="w-3 h-3" />
                                        Created {new Date(issue.createdAtUtc).toLocaleDateString()}
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

            </div>
        </div>
    );
}

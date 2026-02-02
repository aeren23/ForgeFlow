import { useState, useEffect } from 'react';
import { X, CheckSquare, User, Clock, ArrowUp, ArrowRight, AlertOctagon, FileCode, Trash2, ChevronDown, Loader2 } from 'lucide-react';
import { getIssues, deleteIssue, assignIssue, IssueType, IssuePriority, IssueStatus, IssueStatusLabels, type Issue, type UserDto } from '../../services/api';
import { toast } from '../../store/uiStore';
import type { ProjectPermissions } from '../../hooks/useProjectPermissions';
import { confirmAction, showSuccess, showError } from '../../utils/sweetAlert';

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

export function IssueDetailModal({ isOpen, onClose, issue, permissions, usersMap, projectMembers, onDeleteSuccess, onAssignSuccess }: IssueDetailModalProps) {
    const [subTasks, setSubTasks] = useState<Issue[]>([]);
    const [loadingSubTasks, setLoadingSubTasks] = useState(false);
    const [showAssigneeDropdown, setShowAssigneeDropdown] = useState(false);
    const [assigningTo, setAssigningTo] = useState<string | null>(null);

    useEffect(() => {
        if (isOpen && issue && issue.type === IssueType.Epic) {
            fetchSubTasks();
        } else {
            setSubTasks([]);
        }
    }, [isOpen, issue]);

    const fetchSubTasks = async () => {
        if (!issue) return;
        setLoadingSubTasks(true);
        try {
            const projectKey = issue.key.split('-')[0];
            const response = await getIssues(projectKey, issue.id);
            setSubTasks(response.data.items || []);
        } catch (error) {
            // console.error(error);
            toast.error('Failed to load sub-tasks.');
        } finally {
            setLoadingSubTasks(false);
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

                {/* Content - Scrollable */}
                <div className="flex-1 overflow-y-auto p-6">
                    <div className="flex flex-col md:flex-row gap-8">
                        {/* Left Column: Main Content */}
                        <div className="flex-1 space-y-6">
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
                                                                    setAssigningTo(member.userId);
                                                                    setShowAssigneeDropdown(false);
                                                                    try {
                                                                        await assignIssue(issue.key, member.userId);
                                                                        toast.success(`Issue assigned to ${displayName}`);
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

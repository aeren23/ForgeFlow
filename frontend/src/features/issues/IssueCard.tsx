import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import {
    Bug, FileCode, CheckSquare, Bookmark, AlertOctagon,
    ArrowUp, ArrowRight, ArrowDown, User, GitBranch, Clock
} from 'lucide-react';
import { IssueType, IssuePriority, IssueTypeLabels, type Issue } from '../../services/api';

interface IssueCardProps {
    issue: Issue;
    assigneeName?: string;
    onClick?: () => void;
}

export function IssueCard({ issue, assigneeName, onClick }: IssueCardProps) {
    const {
        attributes,
        listeners,
        setNodeRef,
        transform,
        transition,
        isDragging
    } = useSortable({
        id: issue.id,
        data: {
            type: 'Issue',
            issue
        }
    });

    const style = {
        transform: CSS.Transform.toString(transform),
        transition,
        opacity: isDragging ? 0.5 : 1,
    };

    const getTypeIcon = (type: IssueType) => {
        switch (type) {
            case IssueType.Bug: return <Bug className="w-4 h-4 text-error" />;
            case IssueType.Feature: return <Bookmark className="w-4 h-4 text-success" />;
            case IssueType.Story: return <FileCode className="w-4 h-4 text-primary" />;
            case IssueType.Epic: return <AlertOctagon className="w-4 h-4 text-purple-500" />;
            default: return <CheckSquare className="w-4 h-4 text-blue-400" />;
        }
    };

    const getPriorityIcon = (priority: IssuePriority) => {
        switch (priority) {
            case IssuePriority.Critical: return <ArrowUp className="w-4 h-4 text-error" />;
            case IssuePriority.High: return <ArrowUp className="w-4 h-4 text-orange-500" />;
            case IssuePriority.Medium: return <ArrowRight className="w-4 h-4 text-yellow-500" />;
            case IssuePriority.Low: return <ArrowDown className="w-4 h-4 text-green-500" />;
            default: return null;
        }
    };

    const getTimeElapsed = (date: string) => {
        const start = new Date(date);
        const now = new Date();
        const diffMs = now.getTime() - start.getTime();
        const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
        const diffHours = Math.floor((diffMs % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
        if (diffDays > 0) return `${diffDays}d`;
        if (diffHours > 0) return `${diffHours}h`;
        return '<1h';
    };

    return (
        <div
            ref={setNodeRef}
            style={style}
            {...attributes}
            {...listeners}
            onClick={onClick}
            className="bg-surface border border-muted/20 rounded-lg p-3 shadow-sm hover:border-primary/50 hover:shadow-md transition-all cursor-grab active:cursor-grabbing group relative"
        >
            <div className="flex items-start justify-between mb-2">
                <span className="text-xs font-mono text-muted group-hover:text-primary transition-colors">
                    {issue.key}
                </span>
                <div className="flex gap-1">
                    {getPriorityIcon(issue.priority)}
                </div>
            </div>

            <h4 className="text-sm font-medium text-text mb-3 line-clamp-2">
                {issue.title}
            </h4>

            <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                    <div title={IssueTypeLabels[issue.type]}>
                        {getTypeIcon(issue.type)}
                    </div>
                    {issue.branchName && (
                        <div
                            className="flex items-center gap-1 text-xs text-muted hover:text-primary cursor-pointer transition-colors"
                            title={`Branch: ${issue.branchName}`}
                        >
                            <GitBranch className="w-3.5 h-3.5" />
                        </div>
                    )}
                    {issue.startedAtUtc && (
                        <div
                            className="flex items-center gap-1 text-xs text-muted"
                            title={`Started: ${new Date(issue.startedAtUtc).toLocaleString()}`}
                        >
                            <Clock className="w-3 h-3" />
                            <span>{getTimeElapsed(issue.startedAtUtc)}</span>
                        </div>
                    )}
                </div>

                <div
                    className={`w-6 h-6 rounded-full flex items-center justify-center text-xs text-white border border-surface shadow-sm ${issue.assigneeId ? 'bg-gradient-to-br from-blue-500 to-indigo-600' : 'bg-muted/20 text-muted'}`}
                    title={assigneeName || issue.assigneeId || 'Unassigned'}
                >
                    {assigneeName ? assigneeName.charAt(0).toUpperCase() : (issue.assigneeId ? issue.assigneeId.substring(0, 1).toUpperCase() : <User className="w-3 h-3" />)}
                </div>
            </div>
        </div>
    );
}

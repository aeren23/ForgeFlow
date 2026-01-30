import { useDroppable } from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { Plus } from 'lucide-react';
import { IssueStatus, type Issue } from '../../services/api';
import { IssueCard } from './IssueCard';

interface KanbanColumnProps {
    id: string;
    title: string;
    status: IssueStatus;
    issues: Issue[];
    colorClass: string;
    onAddClick?: () => void;
    onIssueClick?: (issue: Issue) => void;
}

export function KanbanColumn({ id, title, status, issues, colorClass, onAddClick, onIssueClick }: KanbanColumnProps) {
    const { setNodeRef } = useDroppable({
        id: id,
        data: {
            type: 'Column',
            status: status
        }
    });

    return (
        <div
            ref={setNodeRef}
            className="flex flex-col h-full bg-surface/50 rounded-xl border border-muted/20 overflow-hidden"
        >
            <div className={`p-3 border-b border-muted/10 font-medium flex items-center justify-between ${colorClass}`}>
                <span>{title}</span>
                <span className="text-xs bg-background/50 px-2 py-0.5 rounded-full">
                    {issues.length}
                </span>
            </div>

            <div className="flex-1 p-2 overflow-y-auto space-y-2">
                <SortableContext
                    items={issues.map(i => i.id)}
                    strategy={verticalListSortingStrategy}
                >
                    {issues.map(issue => (
                        <IssueCard
                            key={issue.id}
                            issue={issue}
                            onClick={() => onIssueClick?.(issue)}
                        />
                    ))}
                </SortableContext>

                {issues.length === 0 && (
                    <div className="h-20 flex items-center justify-center text-muted/30 text-sm border-2 border-dashed border-muted/10 rounded-lg">
                        Drop here
                    </div>
                )}
            </div>

            {status === IssueStatus.Open && onAddClick && (
                <button
                    onClick={onAddClick}
                    className="m-2 p-2 border border-dashed border-muted/30 rounded-lg text-sm text-muted hover:text-primary hover:border-primary/30 transition-all flex items-center justify-center gap-2"
                >
                    <Plus className="w-4 h-4" /> Add Task
                </button>
            )}
        </div>
    );
}

import { useEffect, useState, useRef } from 'react';
import { Loader2, Layers, Layout, ChevronDown, ChevronRight, Plus } from 'lucide-react';
import {
    DndContext,
    DragOverlay,
    closestCorners,
    KeyboardSensor,
    PointerSensor,
    useSensor,
    useSensors,
    type DragStartEvent,
    type DragEndEvent
} from '@dnd-kit/core';
import { sortableKeyboardCoordinates } from '@dnd-kit/sortable';

import { getIssues, updateIssueStatus, IssueStatus, IssueType, getUsersBatch, type Issue, type ProjectDto, type UserDto } from '../../services/api';
import { signalRService } from '../../services/signalRService';
import { KanbanColumn } from './KanbanColumn';
import { IssueCard } from './IssueCard';
import { CreateIssueModal } from './CreateIssueModal';
import { IssueDetailModal } from './IssueDetailModal';
import { toast } from '../../store/uiStore';
import { useAuthStore } from '../../store/authStore';
import { confirmInReview } from '../../utils/sweetAlert';
import { useProjectPermissions } from '../../hooks/useProjectPermissions';

interface ProjectBoardProps {
    project: ProjectDto;
}

export function ProjectBoard({ project }: ProjectBoardProps) {
    const key = project.key;
    const [issues, setIssues] = useState<Issue[]>([]);
    const [loading, setLoading] = useState(true);
    const [isCreateOpen, setCreateOpen] = useState(false);
    const [selectedIssue, setSelectedIssue] = useState<Issue | null>(null);
    const [activeIssue, setActiveIssue] = useState<Issue | null>(null);
    const [viewMode, setViewMode] = useState<'kanban' | 'swimlanes'>('kanban');
    const [collapsedEpics, setCollapsedEpics] = useState<Record<string, boolean>>({});
    const [usersMap, setUsersMap] = useState<Record<string, UserDto>>({});

    const currentUser = useAuthStore(state => state.user);
    const permissions = useProjectPermissions(project);

    // Track which user IDs we've already fetched to prevent loops
    const fetchedUserIds = useRef<Set<string>>(new Set());

    // Batch fetch users for assignees AND project members
    useEffect(() => {
        // Collect all user IDs we need: issue assignees + project members
        const allUserIds: string[] = [];

        // Add issue assignees
        issues.forEach(i => {
            if (i.assigneeId) allUserIds.push(i.assigneeId);
        });

        // Add project members
        project.members?.forEach(m => {
            allUserIds.push(m.userId);
        });

        // Filter out already fetched
        const missingIds = allUserIds.filter(id => !fetchedUserIds.current.has(id));
        const uniqueIds = Array.from(new Set(missingIds));

        if (uniqueIds.length > 0) {
            // Mark as fetched immediately to prevent duplicate requests
            uniqueIds.forEach(id => fetchedUserIds.current.add(id));

            getUsersBatch(uniqueIds)
                .then(res => {
                    setUsersMap(prev => {
                        const next = { ...prev };
                        res.data.forEach(u => next[u.id] = u);
                        return next;
                    });
                })
                .catch(e => console.error("Failed to fetch users", e));
        }
    }, [issues, project.members]);

    // Disable DND sensors if user cannot assign/move issues
    // We use canAssignIssue as proxy for "can move card"
    const canMove = permissions.canAssignIssue;

    const sensors = useSensors(
        useSensor(PointerSensor, {
            activationConstraint: { distance: 5 },
            disabled: !canMove
        }),
        useSensor(KeyboardSensor, {
            coordinateGetter: sortableKeyboardCoordinates,
            disabled: !canMove
        })
    );

    useEffect(() => {
        if (key) fetchIssues();
    }, [key]);

    // Real-time updates subscription
    useEffect(() => {
        if (!key) return;

        // Join the project group for real-time messages (using project KEY, not ID)
        signalRService.joinProject(key);

        const handleBoardUpdate = (msg: any) => {
            // Refresh board if update is for this project
            // Consumer sends projectId as projectKey (e.g., "PROJ")
            if (msg.projectId === key) {
                fetchIssues();
            }
        };

        const handleNotification = (msg: any) => {
            // If AI plan applied, refresh the board
            if (msg.type === 'ai_plan_complete') {
                // Check data if possible, or just refresh
                fetchIssues();
                toast.success('AI Plan tasks created! Board updated.');
            }
        };

        const handleCiCdUpdate = (msg: any) => {
            // CI/CD status güncellemesi geldiğinde ilgili issue'yu güncelle
            setIssues(prev => prev.map(issue =>
                issue.key === msg.issueKey
                    ? { ...issue, ciCdStatus: msg.status, ciCdRunUrl: msg.htmlUrl }
                    : issue
            ));
        };

        const unsubscribeBoard = signalRService.onBoardUpdate(handleBoardUpdate);
        const unsubscribeNotify = signalRService.onNotification(handleNotification);
        const unsubscribeCiCd = signalRService.onCiCdUpdate(handleCiCdUpdate);

        return () => {
            unsubscribeBoard();
            unsubscribeNotify();
            unsubscribeCiCd();
            signalRService.leaveProject(key);
        };
    }, [key]);

    const fetchIssues = async () => {
        setLoading(true);
        try {
            const response = await getIssues(key!);
            setIssues(response.data.items || []);
        } catch (error) {
            toast.error('Failed to load issues');
        } finally {
            setLoading(false);
        }
    };

    const handleCreateSuccess = () => {
        fetchIssues();
    };

    const toggleEpicCollapse = (epicId: string) => {
        setCollapsedEpics(prev => ({ ...prev, [epicId]: !prev[epicId] }));
    };

    const handleDragStart = (event: DragStartEvent) => {
        const { active } = event;
        const issue = issues.find(i => i.id === active.id);
        if (issue) setActiveIssue(issue);
    };

    const handleDragEnd = async (event: DragEndEvent) => {
        const { active, over } = event;
        setActiveIssue(null);

        if (!over) return;

        const issueId = active.id as string;
        const issue = issues.find(i => i.id === issueId);

        if (!issue) return;

        // Permission check: Only assignee, Admin, or Owner can move issues
        const isAssignee = issue.assigneeId === currentUser?.id;
        const isAdminOrOwner = project.currentUserRole === 'Admin' || project.currentUserRole === 'Owner';

        if (!isAssignee && !isAdminOrOwner) {
            toast.error('You can only move issues assigned to you, or you need Admin/Owner role.');
            return;
        }

        // Determine new status based on drop target (Column ID)
        // Ensure the ID of the container includes the status
        // e.g. "todo-epic1", "inprogress-epic1"
        const containerId = over.id as string;
        let newStatus: IssueStatus | undefined;

        if (containerId.includes('todo')) newStatus = IssueStatus.Open;
        else if (containerId.includes('inreview')) newStatus = IssueStatus.InReview;
        else if (containerId.includes('inprogress')) newStatus = IssueStatus.InProgress;
        else if (containerId.includes('done')) newStatus = IssueStatus.Done;

        // If dropped on an issue card (Sortable), we check that issue's status
        // But for simplicity in this MVP, we rely on Column/Container IDs primarily.
        // If dropped on an item, dnd-kit reports over.id as that item's ID.
        if (newStatus === undefined) {
            const overIssue = issues.find(i => i.id === over.id);
            if (overIssue) {
                if (overIssue.status === IssueStatus.Open) newStatus = IssueStatus.Open;
                else if (overIssue.status === IssueStatus.InReview) newStatus = IssueStatus.InReview;
                else if (overIssue.status === IssueStatus.Done || overIssue.status === IssueStatus.Closed) newStatus = IssueStatus.Done;
                else newStatus = IssueStatus.InProgress;
            }
        }

        if (newStatus === undefined || newStatus === issue.status) return;

        // InReview'a geçişte onay dialogu göster
        if (newStatus === IssueStatus.InReview && issue.status !== IssueStatus.InReview) {
            const confirmed = await confirmInReview(issue.key);
            if (!confirmed) return;
        }

        // --- Smart Validation Logic ---
        let shouldAutoAssign = false;
        if ((newStatus === IssueStatus.InProgress || newStatus === IssueStatus.InReview) && !issue.assigneeId) {
            shouldAutoAssign = true;
            toast.success('Issue automatically assigned to you.');
        }

        // Optimistic Update
        const oldStatus = issue.status;
        setIssues(prev => prev.map(i => {
            if (i.id === issueId) {
                return {
                    ...i,
                    status: newStatus!,
                    assigneeId: shouldAutoAssign && currentUser ? currentUser.id : i.assigneeId
                };
            }
            return i;
        }));

        try {
            await updateIssueStatus(issue.key, newStatus!);
        } catch (error) {
            toast.error('Failed to update status');
            setIssues(prev => prev.map(i => i.id === issueId ? { ...i, status: oldStatus } : i));
        }
    };

    const getColumnIssues = (status: IssueStatus, subset: Issue[]) => {
        if (status === IssueStatus.Open)
            return subset.filter(i => i.status === IssueStatus.Open);
        if (status === IssueStatus.InProgress)
            return subset.filter(i => i.status === IssueStatus.InProgress);
        if (status === IssueStatus.InReview)
            return subset.filter(i => i.status === IssueStatus.InReview);
        if (status === IssueStatus.Done)
            return subset.filter(i => i.status === IssueStatus.Done || i.status === IssueStatus.Closed);
        return [];
    };

    // Separate Epics from regular tasks
    const epics = issues.filter(i => i.type === IssueType.Epic);
    // Tasks should not include Epics themselves in the columns
    const tasks = issues.filter(i => i.type !== IssueType.Epic);

    const renderKanbanBoard = () => (
        <div className="flex-1 grid grid-cols-1 md:grid-cols-4 gap-4 min-h-0 bg-muted/5 p-4 rounded-xl border border-muted/10 overflow-hidden">
            <KanbanColumn
                id="column-todo-main"
                title="To Do"
                status={IssueStatus.Open}
                issues={getColumnIssues(IssueStatus.Open, tasks)}
                colorClass="bg-slate-500/5 text-slate-600"
                usersMap={usersMap}
                onAddClick={() => setCreateOpen(true)}
                onIssueClick={setSelectedIssue}
            />
            <KanbanColumn
                id="column-inprogress-main"
                title="In Progress"
                status={IssueStatus.InProgress}
                issues={getColumnIssues(IssueStatus.InProgress, tasks)}
                colorClass="bg-blue-500/5 text-blue-600"
                usersMap={usersMap}
                onIssueClick={setSelectedIssue}
            />
            <KanbanColumn
                id="column-inreview-main"
                title="In Review"
                status={IssueStatus.InReview}
                issues={getColumnIssues(IssueStatus.InReview, tasks)}
                colorClass="bg-amber-500/5 text-amber-600"
                usersMap={usersMap}
                onIssueClick={setSelectedIssue}
            />
            <KanbanColumn
                id="column-done-main"
                title="Done"
                status={IssueStatus.Done}
                issues={getColumnIssues(IssueStatus.Done, tasks)}
                colorClass="bg-green-500/5 text-green-600"
                usersMap={usersMap}
                onIssueClick={setSelectedIssue}
            />
        </div>
    );

    const renderSwimlanes = () => {
        // Group tasks by ParentIssueId
        const orphanTasks = tasks.filter(t => !t.parentIssueId);

        return (
            <div className="flex-1 overflow-y-auto min-h-0 space-y-6 pr-2">
                {/* Orphan Tasks (No Epic) */}
                {orphanTasks.length > 0 && (
                    <div className="bg-muted/5 border border-muted/10 rounded-xl overflow-hidden">
                        <div
                            className="p-3 bg-muted/10 flex items-center justify-between cursor-pointer hover:bg-muted/20 transition-colors"
                            onClick={() => toggleEpicCollapse('orphans')}
                        >
                            <div className="flex items-center gap-2">
                                {collapsedEpics['orphans'] ? <ChevronRight className="w-4 h-4 text-muted" /> : <ChevronDown className="w-4 h-4 text-muted" />}
                                <h3 className="font-medium text-text text-sm">Issues without Epic</h3>
                                <span className="bg-muted/20 text-text/60 px-2 py-0.5 rounded text-xs font-mono">{orphanTasks.length}</span>
                            </div>
                        </div>

                        {!collapsedEpics['orphans'] && (
                            <div className="p-4 grid grid-cols-1 md:grid-cols-4 gap-4">
                                <KanbanColumn
                                    id="column-todo-orphans"
                                    title="To Do"
                                    status={IssueStatus.Open}
                                    issues={getColumnIssues(IssueStatus.Open, orphanTasks)}
                                    colorClass="bg-slate-500/5 text-slate-600"
                                    usersMap={usersMap}
                                    onAddClick={() => setCreateOpen(true)}
                                    onIssueClick={setSelectedIssue}
                                />
                                <KanbanColumn
                                    id="column-inprogress-orphans"
                                    title="In Progress"
                                    status={IssueStatus.InProgress}
                                    issues={getColumnIssues(IssueStatus.InProgress, orphanTasks)}
                                    colorClass="bg-blue-500/5 text-blue-600"
                                    usersMap={usersMap}
                                    onIssueClick={setSelectedIssue}
                                />
                                <KanbanColumn
                                    id="column-inreview-orphans"
                                    title="In Review"
                                    status={IssueStatus.InReview}
                                    issues={getColumnIssues(IssueStatus.InReview, orphanTasks)}
                                    colorClass="bg-amber-500/5 text-amber-600"
                                    usersMap={usersMap}
                                    onIssueClick={setSelectedIssue}
                                />
                                <KanbanColumn
                                    id="column-done-orphans"
                                    title="Done"
                                    status={IssueStatus.Done}
                                    issues={getColumnIssues(IssueStatus.Done, orphanTasks)}
                                    colorClass="bg-green-500/5 text-green-600"
                                    usersMap={usersMap}
                                    onIssueClick={setSelectedIssue}
                                />
                            </div>
                        )}
                    </div>
                )}

                {/* Epic Swimlanes */}
                {epics.map(epic => {
                    const epicTasks = tasks.filter(t => t.parentIssueId === epic.id);
                    const isCollapsed = collapsedEpics[epic.id];

                    return (
                        <div key={epic.id} className="bg-surface border border-muted/20 rounded-xl overflow-hidden shadow-sm">
                            <div
                                className="p-3 bg-gradient-to-r from-purple-500/5 to-transparent border-b border-muted/10 flex items-center justify-between cursor-pointer hover:bg-muted/5 transition-colors"
                                onClick={() => toggleEpicCollapse(epic.id)}
                            >
                                <div className="flex items-center gap-2">
                                    {isCollapsed ? <ChevronRight className="w-4 h-4 text-muted" /> : <ChevronDown className="w-4 h-4 text-muted" />}
                                    <span className="px-2 py-0.5 rounded bg-purple-500/10 text-purple-600 text-xs font-bold uppercase tracking-wider">Epic</span>
                                    <h3 className="font-medium text-text">{epic.title}</h3>
                                    <span className="text-muted text-sm ml-2">({epicTasks.length} issues)</span>
                                </div>
                                <div className="flex items-center gap-2">
                                    {/* Progress Bar could go here */}
                                    <div className="text-xs text-muted font-mono">
                                        {epicTasks.filter(t => t.status === IssueStatus.Done || t.status === IssueStatus.Closed).length} / {epicTasks.length} Done
                                    </div>
                                </div>
                            </div>

                            {!isCollapsed && (
                                <div className="p-4 grid grid-cols-1 md:grid-cols-4 gap-4">
                                    <KanbanColumn
                                        id={`column-todo-${epic.id}`}
                                        title="To Do"
                                        status={IssueStatus.Open}
                                        issues={getColumnIssues(IssueStatus.Open, epicTasks)}
                                        colorClass="bg-slate-500/5 text-slate-600"
                                        usersMap={usersMap}
                                        onAddClick={() => setCreateOpen(true)}
                                        onIssueClick={setSelectedIssue}
                                    />
                                    <KanbanColumn
                                        id={`column-inprogress-${epic.id}`}
                                        title="In Progress"
                                        status={IssueStatus.InProgress}
                                        issues={getColumnIssues(IssueStatus.InProgress, epicTasks)}
                                        colorClass="bg-blue-500/5 text-blue-600"
                                        usersMap={usersMap}
                                        onIssueClick={setSelectedIssue}
                                    />
                                    <KanbanColumn
                                        id={`column-inreview-${epic.id}`}
                                        title="In Review"
                                        status={IssueStatus.InReview}
                                        issues={getColumnIssues(IssueStatus.InReview, epicTasks)}
                                        colorClass="bg-amber-500/5 text-amber-600"
                                        usersMap={usersMap}
                                        onIssueClick={setSelectedIssue}
                                    />
                                    <KanbanColumn
                                        id={`column-done-${epic.id}`}
                                        title="Done"
                                        status={IssueStatus.Done}
                                        issues={getColumnIssues(IssueStatus.Done, epicTasks)}
                                        colorClass="bg-green-500/5 text-green-600"
                                        usersMap={usersMap}
                                        onIssueClick={setSelectedIssue}
                                    />
                                </div>
                            )}
                        </div>
                    );
                })}

                {epics.length === 0 && orphanTasks.length === 0 && (
                    <div className="text-center py-12 text-muted bg-muted/5 rounded-xl border border-dashed border-muted/20">
                        <Layers className="w-12 h-12 mx-auto mb-4 text-muted/50" />
                        <p>No issues found. Create one to get started.</p>
                        <button
                            onClick={() => setCreateOpen(true)}
                            className="mt-4 px-4 py-2 bg-primary text-white rounded-lg text-sm font-medium hover:bg-primary/90"
                        >
                            Create First Issue
                        </button>
                    </div>
                )}
            </div>
        );
    };

    return (
        <div className="space-y-4 h-full flex flex-col">
            <div className="flex items-center justify-between shrink-0">
                <div className="flex items-center gap-4">
                    <h2 className="text-lg font-semibold text-text">Board</h2>
                    <div className="flex bg-muted/10 p-1 rounded-lg border border-muted/10">
                        <button
                            onClick={() => setViewMode('kanban')}
                            className={`p-1.5 rounded-md transition-all flex items-center gap-2 px-3 text-xs font-medium ${viewMode === 'kanban' ? 'bg-surface shadow-sm text-primary' : 'text-muted hover:text-text'}`}
                            title="Kanban View"
                        >
                            <Layout className="w-4 h-4" />
                            Kanban
                        </button>
                        <button
                            onClick={() => setViewMode('swimlanes')}
                            className={`p-1.5 rounded-md transition-all flex items-center gap-2 px-3 text-xs font-medium ${viewMode === 'swimlanes' ? 'bg-surface shadow-sm text-primary' : 'text-muted hover:text-text'}`}
                            title="Swimlanes View (Group by Epic)"
                        >
                            <Layers className="w-4 h-4" />
                            Swimlanes
                        </button>
                    </div>
                </div>
                <button
                    onClick={() => setCreateOpen(true)}
                    className="flex items-center gap-2 px-4 py-2 bg-primary hover:bg-primary/90 text-white rounded-lg text-sm font-medium transition-colors shadow-sm shadow-primary/20"
                >
                    <Plus className="w-4 h-4" />
                    Create Issue
                </button>
            </div>

            {loading ? (
                <div className="flex-1 flex items-center justify-center">
                    <Loader2 className="w-8 h-8 text-primary animate-spin" />
                </div>
            ) : (
                <DndContext
                    sensors={sensors}
                    collisionDetection={closestCorners}
                    onDragStart={handleDragStart}
                    onDragEnd={handleDragEnd}
                >
                    {viewMode === 'kanban' ? renderKanbanBoard() : renderSwimlanes()}

                    <DragOverlay>
                        {activeIssue ? <IssueCard issue={activeIssue} assigneeName={activeIssue.assigneeId ? usersMap[activeIssue.assigneeId]?.fullName : undefined} /> : null}
                    </DragOverlay>
                </DndContext>
            )}

            <CreateIssueModal
                isOpen={isCreateOpen}
                onClose={() => setCreateOpen(false)}
                onSuccess={handleCreateSuccess}
                projectKey={key!}
            />

            <IssueDetailModal
                isOpen={!!selectedIssue}
                onClose={() => setSelectedIssue(null)}
                issue={selectedIssue}
                permissions={permissions}
                usersMap={usersMap}
                projectMembers={project.members}
                onDeleteSuccess={fetchIssues}
                onAssignSuccess={() => {
                    setSelectedIssue(null); // Close modal
                    fetchIssues(); // Refresh issue list
                }}
            />
        </div>
    );
}

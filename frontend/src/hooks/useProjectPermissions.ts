import type { ProjectDto } from '../services/api';

export interface ProjectPermissions {
    canDeleteProject: boolean;
    canManageMembers: boolean;
    canEditProject: boolean;
    canCreateIssue: boolean;
    canEditIssue: boolean;
    canDeleteIssue: boolean;
    canAssignIssue: boolean;
}

export const useProjectPermissions = (project: ProjectDto | null): ProjectPermissions => {
    if (!project) {
        return {
            canDeleteProject: false,
            canManageMembers: false,
            canEditProject: false,
            canCreateIssue: false,
            canEditIssue: false,
            canDeleteIssue: false,
            canAssignIssue: false,
        };
    }

    const role = project.currentUserRole;
    const isOwner = role === 'Owner';
    const isAdmin = role === 'Admin';
    // const isMember = role === 'Member'; // Unused
    const isViewer = role === 'Viewer';

    // System Admin override logic can be added here if we had that flag in ProjectDto,
    // but for now we rely on ProjectDto.currentUserRole which is calculated in backend.

    return {
        canDeleteProject: isOwner,
        canManageMembers: isOwner || isAdmin,
        canEditProject: isOwner || isAdmin,
        canCreateIssue: !isViewer,
        canEditIssue: !isViewer, // Simplified for UI: Viewers can't edit. Members/Admins/Owners can try.
        canDeleteIssue: isOwner || isAdmin,
        canAssignIssue: !isViewer,
    };
};

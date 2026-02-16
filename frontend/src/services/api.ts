import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '../store/authStore';

// Create axios instance
export interface CreateProjectRequest {
    key: string;
    name: string;
    description: string;
    techStack: string[];
    repositoryUrl?: string;
    projectType: number;
}

const api = axios.create({
    baseURL: import.meta.env.VITE_API_URL || 'http://localhost:8090',
    headers: {
        'Content-Type': 'application/json',
    },
    withCredentials: true,
});

export const createProject = async (data: CreateProjectRequest) => {
    return api.post('/api/projects', data);
};

export interface UpdateProjectRequest {
    name: string;
    description?: string;
    repositoryUrl?: string;
    techStack?: string[];
    projectType: number;
}

export const getProject = async (key: string) => {
    return api.get(`/api/projects/${key}`);
};

export const updateProject = async (key: string, data: UpdateProjectRequest) => {
    return api.put(`/api/projects/${key}`, data);
};

export const deleteProject = async (key: string) => {
    return api.delete(`/api/projects/${key}`);
};

// --- Issues ---

// --- Issues ---

export const IssueStatus = {
    Open: 0,
    InProgress: 1,
    InReview: 2,
    Done: 3,
    Closed: 4
} as const;
export type IssueStatus = typeof IssueStatus[keyof typeof IssueStatus];

export const IssueStatusLabels: Record<number, string> = {
    [IssueStatus.Open]: 'Open',
    [IssueStatus.InProgress]: 'In Progress',
    [IssueStatus.InReview]: 'In Review',
    [IssueStatus.Done]: 'Done',
    [IssueStatus.Closed]: 'Closed'
};

export const IssuePriority = {
    Low: 0,
    Medium: 1,
    High: 2,
    Critical: 3
} as const;
export type IssuePriority = typeof IssuePriority[keyof typeof IssuePriority];

export const IssuePriorityLabels: Record<number, string> = {
    [IssuePriority.Low]: 'Low',
    [IssuePriority.Medium]: 'Medium',
    [IssuePriority.High]: 'High',
    [IssuePriority.Critical]: 'Critical'
};

export const IssueType = {
    Bug: 0,
    Feature: 1,
    Task: 2,
    Story: 3,
    Epic: 4
} as const;
export type IssueType = typeof IssueType[keyof typeof IssueType];

export const IssueTypeLabels: Record<number, string> = {
    [IssueType.Bug]: 'Bug',
    [IssueType.Feature]: 'Feature',
    [IssueType.Task]: 'Task',
    [IssueType.Story]: 'Story',
    [IssueType.Epic]: 'Epic'
};

export interface Issue {
    id: string;
    key: string;
    title: string;
    description?: string;
    status: IssueStatus;
    priority: IssuePriority;
    type: IssueType;
    projectId: string;
    assigneeId?: string;
    parentIssueId?: string;
    createdAtUtc: string;
    startedAtUtc?: string;
    branchName?: string;
}

export interface CreateIssueRequest {
    projectKey: string;
    title: string;
    description?: string;
    type: IssueType;
    priority: IssuePriority;
    assigneeId?: string;
    dueDate?: string;
    estimatedHours?: number;
}

export const getIssues = async (projectKey: string, parentIssueId?: string) => {
    const url = parentIssueId
        ? `/api/issues?projectKey=${projectKey}&parentIssueId=${parentIssueId}&pageSize=100`
        : `/api/issues?projectKey=${projectKey}&pageSize=100`;
    return api.get(url);
};

export const createIssue = async (data: CreateIssueRequest) => {
    return api.post('/api/issues', data);
};

export const updateIssueStatus = async (key: string, status: IssueStatus) => {
    return api.post(`/api/issues/${key}/status`, { status });
};

export const deleteIssue = async (key: string) => {
    return api.delete(`/api/issues/${key}`);
};

export interface UpdateIssueRequest {
    title?: string;
    description?: string;
    priority?: IssuePriority;
    assigneeId?: string | null;
}

export const updateIssue = async (key: string, data: UpdateIssueRequest) => {
    return api.put(`/api/issues/${key}`, data);
};

export interface GenerateAiPlanRequest {
    planName: string;
    description: string;
    bundleType?: string;
}

export const generateAiPlan = async (issueKey: string) => {
    return api.post(`/api/issues/${issueKey}/generate`);
};

export const generateProjectAiPlan = async (projectKey: string, data: GenerateAiPlanRequest) => {
    return api.post(`/api/projects/${projectKey}/generate-plan`, data);
};

// Issue assignment
export const assignIssue = async (issueKey: string, assigneeId: string | null, createBranch: boolean = true) => {
    return api.post(`/api/issues/${issueKey}/assign`, { assigneeId, createBranch });
};

// --- AI Code Reviews ---

export interface CodeReviewDto {
    artifactId: string;
    revisionNo: number;
    contentJson: string;
    correlationId?: string;
    metadata?: string;
    createdAtUtc: string;
}

export const getCodeReviews = async (issueId: string, projectId: string) => {
    return api.get<CodeReviewDto[]>(`/api/artifacts/reviews`, {
        params: { issueId, projectId }
    });
};

// Flag to prevent multiple refresh attempts
let isRefreshing = false;
let failedQueue: Array<{
    resolve: (token: string) => void;
    reject: (error: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null = null) => {
    failedQueue.forEach((prom) => {
        if (error) {
            prom.reject(error);
        } else {
            prom.resolve(token!);
        }
    });
    failedQueue = [];
};

// Request Interceptor: Add Authorization header
api.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
        const token = useAuthStore.getState().accessToken;
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error) => Promise.reject(error)
);

// Response Interceptor: Handle 401 and silent refresh
api.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
        const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

        // If 401 and not a refresh request itself
        if (error.response?.status === 401 && !originalRequest._retry) {
            if (isRefreshing) {
                // Queue this request until refresh completes
                return new Promise((resolve, reject) => {
                    failedQueue.push({ resolve, reject });
                })
                    .then((token) => {
                        originalRequest.headers.Authorization = `Bearer ${token}`;
                        return api(originalRequest);
                    })
                    .catch((err) => Promise.reject(err));
            }

            originalRequest._retry = true;
            isRefreshing = true;

            const refreshToken = useAuthStore.getState().refreshToken;

            if (!refreshToken) {
                // No refresh token, logout
                useAuthStore.getState().logout();
                window.location.href = '/login';
                return Promise.reject(error);
            }

            try {
                const response = await axios.post(
                    `${import.meta.env.VITE_API_URL || 'http://localhost:8090'}/api/auth/refresh`,
                    { refreshToken }
                );

                const { accessToken, refreshToken: newRefreshToken } = response.data;

                useAuthStore.getState().setTokens(accessToken, newRefreshToken);
                processQueue(null, accessToken);

                originalRequest.headers.Authorization = `Bearer ${accessToken}`;
                return api(originalRequest);
            } catch (refreshError) {
                processQueue(refreshError, null);
                useAuthStore.getState().logout();
                window.location.href = '/login';
                return Promise.reject(refreshError);
            } finally {
                isRefreshing = false;
            }
        }

        return Promise.reject(error);
    }
);

export interface ProjectMember {
    userId: string;
    role: string;
    joinedAtUtc: string;
}

export interface ProjectDto {
    id: string;
    key: string;
    name: string;
    description?: string;
    repositoryUrl?: string;
    repositoryProvider?: number;
    defaultBranch: string;
    techStack: string[];
    projectType: number;
    creatorId: string;
    issueCount: number;
    createdAtUtc: string;
    updatedAtUtc: string;
    members: ProjectMember[];
    currentUserRole?: string;
}

export interface UserDto {
    id: string;
    userName: string;
    email: string;
    fullName: string;
    isSystemAdmin?: boolean;
    isActive?: boolean;
    createdAtUtc?: string;
}

export interface SearchUsersResponse {
    items: UserDto[];
    totalCount: number;
}

export interface AdminStats {
    totalUsers: number;
    activeUsers: number;
    bannedUsers: number;
}

export const searchUsers = async (term: string, page = 1, pageSize = 10) => {
    return api.get<SearchUsersResponse>(`/api/users?term=${term}&page=${page}&pageSize=${pageSize}`);
};

export const getUsersBatch = async (userIds: string[]) => {
    return api.post<UserDto[]>('/api/users/batch', userIds);
};

export const addProjectMember = async (projectKey: string, userId: string, role = 'Member') => {
    return api.post(`/api/projects/${projectKey}/members`, { userId, role });
};

export const updateProjectMemberRole = async (projectKey: string, userId: string, role: string) => {
    return api.put(`/api/projects/${projectKey}/members/${userId}`, { role });
};

export const removeProjectMember = async (projectKey: string, userId: string) => {
    return api.delete(`/api/projects/${projectKey}/members/${userId}`);
};

// --- Admin ---

export const getAdminStats = async () => {
    return api.get<AdminStats>('/api/admin/stats');
};

export const getAllUsers = async (page = 1, pageSize = 20, search = '') => {
    return api.get<{ items: UserDto[], totalCount: number, page: number, pageSize: number }>(`/api/admin/users?page=${page}&pageSize=${pageSize}&search=${search}`);
};

export const toggleUserBan = async (userId: string) => {
    return api.put<{ message: string, isActive: boolean }>(`/api/admin/users/${userId}/ban`);
};

// --- GitHub Integration ---

export interface GitHubInstallation {
    id: string;
    installationId: number;
    accountLogin: string;
    accountType: string;
    installedAt: string;
    repositoryCount: number;
}

export interface GitHubRepository {
    id: number;
    name: string;
    fullName: string;
    private: boolean;
    htmlUrl: string;
    defaultBranch: string;
}

export interface LinkProjectToRepositoryRequest {
    projectId: string;
    installationId: number;
    repositoryFullName: string;
    defaultBranch?: string;
    repositoryId?: number;
    accountLogin?: string;
    accountType?: string;
}

export const listGitHubInstallations = async () => {
    return api.get<GitHubInstallation[]>('/api/installations');
};

export const listGitHubRepositories = async (installationId: number) => {
    return api.get<GitHubRepository[]>(`/api/installations/${installationId}/repositories`);
};

export const linkProjectToRepository = async (data: LinkProjectToRepositoryRequest) => {
    return api.post('/api/installations/link', data);
};

export const getProjectRepositoryConnection = async (projectId: string) => {
    return api.get(`/api/installations/project/${projectId}`);
};

export const unlinkProjectRepository = async (projectId: string) => {
    return api.delete(`/api/installations/project/${projectId}`);
};

export default api;

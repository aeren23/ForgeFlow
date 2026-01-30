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
    createdAtUtc: string;
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

export const getIssues = async (projectKey: string) => {
    return api.get(`/api/issues?projectKey=${projectKey}&pageSize=100`);
};

export const createIssue = async (data: CreateIssueRequest) => {
    return api.post('/api/issues', data);
};

export const updateIssueStatus = async (key: string, status: IssueStatus) => {
    return api.post(`/api/issues/${key}/status`, { status });
};

export const generateAiPlan = async (issueKey: string) => {
    return api.post(`/api/issues/${issueKey}/generate`);
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

export default api;

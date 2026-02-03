import { useState } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ToastContainer } from './components/ui/ToastContainer';
import { LiveLog } from './components/ui/LiveLog';
import { AuthProvider } from './components/AuthProvider';
import { useNotificationStore } from './store/notificationStore';
import { ProtectedRoute } from './components/ProtectedRoute';
import { AdminRoute } from './components/AdminRoute';
import { LoginPage } from './features/auth/LoginPage';
import { RegisterPage } from './features/auth/RegisterPage';
import { ProfilePage } from './features/profile/ProfilePage';
import { DashboardLayout } from './layouts/DashboardLayout';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { ProjectLayout } from './layouts/ProjectLayout';
import { ProjectDetailPage } from './features/projects/ProjectDetailPage';
import { ProjectSettingsPage } from './features/projects/ProjectSettingsPage';
import { AdminLayout } from './layouts/AdminLayout';
import { AdminDashboard } from './pages/admin/AdminDashboard';
import { UserManagement } from './pages/admin/UserManagement';
import { AdminProjectsPage } from './pages/admin/AdminProjectsPage';

function App() {
  const { isLiveLogOpen, setLiveLogOpen } = useNotificationStore();
  const [isLiveLogMinimized, setLiveLogMinimized] = useState(false);

  return (
    <BrowserRouter>
      <AuthProvider>
        {/* Global Toast Container */}
        <ToastContainer />

        {/* Global Live Log Console */}
        <LiveLog
          isOpen={isLiveLogOpen}
          onClose={() => setLiveLogOpen(false)}
          isMinimized={isLiveLogMinimized}
          onToggleMinimize={() => setLiveLogMinimized(!isLiveLogMinimized)}
        />

        <Routes>
          {/* Public Routes */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />

          {/* Protected Routes */}
          <Route element={<ProtectedRoute />}>
            <Route element={<DashboardLayout />}>
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/profile" element={<ProfilePage />} />
            </Route>

            <Route path="/project/:key" element={<ProjectLayout />}>
              <Route index element={<Navigate to="board" replace />} />
              <Route path="board" element={<ProjectDetailPage />} />
              <Route path="settings" element={<ProjectSettingsPage />} />
            </Route>

            {/* Admin Routes - Protected + Admin Only */}
            <Route element={<AdminRoute />}>
              <Route path="/admin" element={<AdminLayout />}>
                <Route index element={<AdminDashboard />} />
                <Route path="projects" element={<AdminProjectsPage />} />
                <Route path="users" element={<UserManagement />} />
              </Route>
            </Route>
          </Route>

          {/* Default redirect */}
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;

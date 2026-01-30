import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ToastContainer } from './components/ui/ToastContainer';
import { ProtectedRoute } from './components/ProtectedRoute';
import { LoginPage } from './features/auth/LoginPage';
import { RegisterPage } from './features/auth/RegisterPage';
import { ProfilePage } from './features/profile/ProfilePage';
import { DashboardLayout } from './layouts/DashboardLayout';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { ProjectLayout } from './layouts/ProjectLayout';
import { ProjectDetailPage } from './features/projects/ProjectDetailPage';
import { ProjectSettingsPage } from './features/projects/ProjectSettingsPage';

function App() {
  return (
    <BrowserRouter>
      {/* Global Toast Container */}
      <ToastContainer />

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
        </Route>

        {/* Default redirect */}
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;

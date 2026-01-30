import { Outlet } from 'react-router-dom';
import { TopHeader } from '../components/layout/TopHeader';

export function DashboardLayout() {
    return (
        <div className="min-h-screen bg-background flex flex-col">
            <TopHeader />
            <main className="flex-1">
                <Outlet />
            </main>
        </div>
    );
}

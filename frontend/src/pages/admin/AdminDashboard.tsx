import { useEffect, useState } from 'react';
import { Users, UserCheck, UserX, Loader2 } from 'lucide-react';
import { getAdminStats, type AdminStats } from '../../services/api';

export function AdminDashboard() {
    const [stats, setStats] = useState<AdminStats | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const loadStats = async () => {
            try {
                const response = await getAdminStats();
                setStats(response.data);
            } catch (error) {
                console.error('Failed to load stats', error);
            } finally {
                setLoading(false);
            }
        };

        loadStats();
    }, []);

    if (loading) {
        return (
            <div className="flex h-full items-center justify-center">
                <Loader2 className="w-8 h-8 animate-spin text-primary" />
            </div>
        );
    }

    if (!stats) return <div>Stats unavailable.</div>;

    const cards = [
        {
            title: 'Total Users',
            value: stats.totalUsers,
            icon: Users,
            color: 'text-blue-500',
            bg: 'bg-blue-500/10',
            border: 'border-blue-500/20'
        },
        {
            title: 'Active Users',
            value: stats.activeUsers,
            icon: UserCheck,
            color: 'text-green-500',
            bg: 'bg-green-500/10',
            border: 'border-green-500/20'
        },
        {
            title: 'Banned Users',
            value: stats.bannedUsers,
            icon: UserX,
            color: 'text-red-500',
            bg: 'bg-red-500/10',
            border: 'border-red-500/20'
        }
    ];

    return (
        <div className="space-y-8">
            <div className="flex items-center justify-between">
                <h1 className="text-3xl font-bold text-text">System Overview</h1>
                <p className="text-muted text-sm">Real-time platform statistics</p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                {cards.map((card, idx) => (
                    <div key={idx} className={`p-6 rounded-2xl border ${card.border} bg-surface shadow-sm`}>
                        <div className="flex items-center justify-between mb-4">
                            <div className={`p-3 rounded-xl ${card.bg} ${card.color}`}>
                                <card.icon className="w-6 h-6" />
                            </div>
                            <span className={`text-2xl font-bold ${card.color}`}>{card.value}</span>
                        </div>
                        <h3 className="text-muted font-medium">{card.title}</h3>
                    </div>
                ))}
            </div>
        </div>
    );
}

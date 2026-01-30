import { useEffect, useState } from 'react';
import { User, Mail, Shield, Calendar, ArrowLeft, Loader2 } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import api from '../../services/api';
import { toast } from '../../store/uiStore';

interface UserProfile {
    id: string;
    email: string;
    fullName: string;
    roles: string[];
    createdAt: string;
}

export function ProfilePage() {
    const navigate = useNavigate();
    const [profile, setProfile] = useState<UserProfile | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        fetchProfile();
    }, []);

    const fetchProfile = async () => {
        try {
            const response = await api.get('/api/auth/profile');
            setProfile(response.data);
        } catch (error) {
            toast.error('Failed to load profile information.');
            navigate('/dashboard');
        } finally {
            setLoading(false);
        }
    };

    if (loading) {
        return (
            <div className="min-h-screen bg-background flex items-center justify-center">
                <Loader2 className="w-8 h-8 text-primary animate-spin" />
            </div>
        );
    }

    if (!profile) return null;

    return (
        <div className="min-h-screen bg-background p-4 md:p-8">
            <div className="max-w-3xl mx-auto">
                {/* Back Button */}
                <button
                    onClick={() => navigate('/dashboard')}
                    className="flex items-center gap-2 text-muted hover:text-text transition-colors mb-8"
                >
                    <ArrowLeft className="w-4 h-4" />
                    Back to Dashboard
                </button>

                {/* Profile Card */}
                <div className="bg-surface border border-muted/20 rounded-2xl overflow-hidden shadow-xl">
                    {/* Header Banner */}
                    <div className="h-32 bg-gradient-to-r from-primary/20 to-secondary/20 border-b border-primary/10"></div>

                    <div className="px-8 pb-8">
                        <div className="relative flex justify-between items-end -mt-12 mb-6">
                            {/* Avatar */}
                            <div className="w-24 h-24 rounded-2xl bg-surface border-4 border-surface shadow-lg flex items-center justify-center">
                                <span className="text-3xl font-bold text-primary">
                                    {profile.fullName?.charAt(0).toUpperCase() || profile.email.charAt(0).toUpperCase()}
                                </span>
                            </div>

                            {/* Role Badge */}
                            <div className="flex gap-2">
                                {profile.roles.map(role => (
                                    <span key={role} className="px-3 py-1 rounded-full bg-primary/10 text-primary border border-primary/20 text-sm font-medium flex items-center gap-1.5">
                                        <Shield className="w-3 h-3" />
                                        {role}
                                    </span>
                                ))}
                            </div>
                        </div>

                        {/* User Info */}
                        <div className="space-y-6">
                            <div>
                                <h1 className="text-2xl font-bold text-text">{profile.fullName || 'User'}</h1>
                                <p className="text-muted">ForgeFlow Member</p>
                            </div>

                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                {/* Email */}
                                <div className="p-4 rounded-xl bg-background border border-muted/30 flex items-center gap-3">
                                    <div className="w-10 h-10 rounded-lg bg-blue-500/10 flex items-center justify-center text-blue-500">
                                        <Mail className="w-5 h-5" />
                                    </div>
                                    <div>
                                        <p className="text-xs text-muted uppercase tracking-wider font-semibold">Email</p>
                                        <p className="text-text font-medium">{profile.email}</p>
                                    </div>
                                </div>

                                {/* Join Date */}
                                <div className="p-4 rounded-xl bg-background border border-muted/30 flex items-center gap-3">
                                    <div className="w-10 h-10 rounded-lg bg-green-500/10 flex items-center justify-center text-green-500">
                                        <Calendar className="w-5 h-5" />
                                    </div>
                                    <div>
                                        <p className="text-xs text-muted uppercase tracking-wider font-semibold">Member Since</p>
                                        <p className="text-text font-medium">
                                            {new Date(profile.createdAt).toLocaleDateString('en-US', {
                                                year: 'numeric',
                                                month: 'long',
                                                day: 'numeric'
                                            })}
                                        </p>
                                    </div>
                                </div>

                                {/* User ID (System Info) */}
                                <div className="p-4 rounded-xl bg-background border border-muted/30 flex items-center gap-3 md:col-span-2">
                                    <div className="w-10 h-10 rounded-lg bg-purple-500/10 flex items-center justify-center text-purple-500">
                                        <User className="w-5 h-5" />
                                    </div>
                                    <div className="flex-1">
                                        <p className="text-xs text-muted uppercase tracking-wider font-semibold">User ID</p>
                                        <p className="text-text font-mono text-sm">{profile.id}</p>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}

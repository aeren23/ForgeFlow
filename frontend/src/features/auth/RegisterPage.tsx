import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { Flame, Mail, Lock, User, Loader2 } from 'lucide-react';
import api from '../../services/api';
import { toast } from '../../store/uiStore';

export function RegisterPage() {
    const navigate = useNavigate();

    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [fullName, setFullName] = useState('');
    const [loading, setLoading] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setLoading(true);

        try {
            await api.post('/api/auth/register', { email, password, fullName });
            toast.success('Registration successful! You can now sign in.');
            navigate('/login');
        } catch (error: any) {
            const errors = error.response?.data?.errors;
            if (errors && Array.isArray(errors)) {
                errors.forEach((err: string) => toast.error(err));
            } else {
                toast.error('Registration failed. Please check your information.');
            }
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="min-h-screen bg-background flex items-center justify-center p-4">
            <div className="w-full max-w-md">
                {/* Logo */}
                <div className="text-center mb-8">
                    <div className="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-primary/20 mb-4">
                        <Flame className="w-8 h-8 text-primary" />
                    </div>
                    <h1 className="text-3xl font-bold text-text">ForgeFlow</h1>
                    <p className="text-muted mt-2">Create a new account</p>
                </div>

                {/* Form */}
                <form onSubmit={handleSubmit} className="bg-surface rounded-2xl p-8 shadow-xl border border-surface/50">
                    {/* Full Name */}
                    <div className="mb-4">
                        <label className="block text-sm font-medium text-muted mb-2">Full Name</label>
                        <div className="relative">
                            <User className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted" />
                            <input
                                type="text"
                                value={fullName}
                                onChange={(e) => setFullName(e.target.value)}
                                placeholder="John Doe"
                                className="w-full bg-background border border-muted/30 rounded-lg py-3 pl-11 pr-4 text-text placeholder:text-muted/50 focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary transition-all"
                            />
                        </div>
                    </div>

                    {/* Email */}
                    <div className="mb-4">
                        <label className="block text-sm font-medium text-muted mb-2">Email</label>
                        <div className="relative">
                            <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted" />
                            <input
                                type="email"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                                placeholder="example@email.com"
                                required
                                className="w-full bg-background border border-muted/30 rounded-lg py-3 pl-11 pr-4 text-text placeholder:text-muted/50 focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary transition-all"
                            />
                        </div>
                    </div>

                    {/* Password */}
                    <div className="mb-6">
                        <label className="block text-sm font-medium text-muted mb-2">Password</label>
                        <div className="relative">
                            <Lock className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted" />
                            <input
                                type="password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                placeholder="••••••••"
                                required
                                minLength={6}
                                className="w-full bg-background border border-muted/30 rounded-lg py-3 pl-11 pr-4 text-text placeholder:text-muted/50 focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary transition-all"
                            />
                        </div>
                        <p className="text-xs text-muted mt-1">At least 6 characters</p>
                    </div>

                    {/* Submit */}
                    <button
                        type="submit"
                        disabled={loading}
                        className="w-full bg-primary hover:bg-primary/90 text-white font-semibold py-3 rounded-lg transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
                    >
                        {loading ? (
                            <>
                                <Loader2 className="w-5 h-5 animate-spin" />
                                Creating account...
                            </>
                        ) : (
                            'Sign Up'
                        )}
                    </button>

                    {/* Login link */}
                    <p className="text-center text-muted mt-6">
                        Already have an account?{' '}
                        <Link to="/login" className="text-primary hover:underline font-medium">
                            Sign in
                        </Link>
                    </p>
                </form>
            </div>
        </div>
    );
}

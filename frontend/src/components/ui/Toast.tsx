import { X, CheckCircle, AlertCircle, Info, AlertTriangle } from 'lucide-react';
import { type Toast as ToastType, useUIStore } from '../../store/uiStore';

const iconMap = {
    success: CheckCircle,
    error: AlertCircle,
    info: Info,
    warning: AlertTriangle,
};

const colorMap = {
    success: 'bg-success/20 border-success text-success',
    error: 'bg-error/20 border-error text-error',
    info: 'bg-info/20 border-info text-info',
    warning: 'bg-warning/20 border-warning text-warning',
};

export function Toast({ toast }: { toast: ToastType }) {
    const removeToast = useUIStore((state) => state.removeToast);
    const Icon = iconMap[toast.type];

    return (
        <div
            className={`flex items-center gap-3 px-4 py-3 rounded-lg border shadow-lg backdrop-blur-sm animate-in slide-in-from-right duration-300 ${colorMap[toast.type]}`}
        >
            <Icon className="w-5 h-5 shrink-0" />
            <span className="text-sm text-text flex-1">{toast.message}</span>
            <button
                onClick={() => removeToast(toast.id)}
                className="text-muted hover:text-text transition-colors"
            >
                <X className="w-4 h-4" />
            </button>
        </div>
    );
}

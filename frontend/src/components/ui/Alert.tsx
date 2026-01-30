import { X, CheckCircle, AlertCircle, Info, AlertTriangle } from 'lucide-react';
import { type Alert as AlertType, useUIStore } from '../../store/uiStore';

const iconMap = {
    success: CheckCircle,
    error: AlertCircle,
    info: Info,
    warning: AlertTriangle,
};

const colorMap = {
    success: 'bg-success/10 border-success/50 text-success',
    error: 'bg-error/10 border-error/50 text-error',
    info: 'bg-info/10 border-info/50 text-info',
    warning: 'bg-warning/10 border-warning/50 text-warning',
};

export function Alert({ alert }: { alert: AlertType }) {
    const removeAlert = useUIStore((state) => state.removeAlert);
    const Icon = iconMap[alert.type];

    return (
        <div
            className={`flex items-start gap-3 p-4 rounded-lg border ${colorMap[alert.type]}`}
        >
            <Icon className="w-5 h-5 shrink-0 mt-0.5" />
            <div className="flex-1">
                <h4 className="font-semibold text-text">{alert.title}</h4>
                <p className="text-sm text-muted mt-1">{alert.message}</p>
            </div>
            <button
                onClick={() => removeAlert(alert.id)}
                className="text-muted hover:text-text transition-colors"
            >
                <X className="w-4 h-4" />
            </button>
        </div>
    );
}

export function AlertContainer() {
    const alerts = useUIStore((state) => state.alerts);

    if (alerts.length === 0) return null;

    return (
        <div className="flex flex-col gap-2 mb-4">
            {alerts.map((alert) => (
                <Alert key={alert.id} alert={alert} />
            ))}
        </div>
    );
}

import { useUIStore } from '../../store/uiStore';
import { Toast } from './Toast';

export function ToastContainer() {
    const toasts = useUIStore((state) => state.toasts);

    return (
        <div className="fixed top-4 right-4 z-50 flex flex-col gap-2 max-w-sm w-full">
            {toasts.map((toast) => (
                <Toast key={toast.id} toast={toast} />
            ))}
        </div>
    );
}

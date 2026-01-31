import Swal from 'sweetalert2';
import withReactContent from 'sweetalert2-react-content';

const MySwal = withReactContent(Swal);

interface ConfirmOptions {
    title?: string;
    text?: string;
    confirmButtonText?: string;
    cancelButtonText?: string;
    icon?: 'warning' | 'error' | 'success' | 'info' | 'question';
}

export const confirmAction = async ({
    title = 'Are you sure?',
    text = "You won't be able to revert this!",
    confirmButtonText = 'Yes, delete it!',
    cancelButtonText = 'Cancel',
    icon = 'warning'
}: ConfirmOptions): Promise<boolean> => {
    const result = await MySwal.fire({
        title,
        text,
        icon,
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText,
        cancelButtonText,
        background: '#1e293b', // Dark theme background (slate-800)
        color: '#f8fafc',      // Dark theme text (slate-50)
    });

    return result.isConfirmed;
};

export const showSuccess = (title: string, text?: string) => {
    return MySwal.fire({
        title,
        text,
        icon: 'success',
        timer: 1500,
        showConfirmButton: false,
        background: '#1e293b',
        color: '#f8fafc'
    });
};

export const showError = (title: string, text?: string) => {
    return MySwal.fire({
        title,
        text,
        icon: 'error',
        background: '#1e293b',
        color: '#f8fafc'
    });
};

// Specific for Owner Transfer as requested
export const listConfirmOwnerTransfer = async (userName: string): Promise<boolean> => {
    const result = await MySwal.fire({
        title: 'Transfer Ownership?',
        text: `Are you sure you want to transfer project ownership to ${userName}? You will lose your Owner privileges and become an Admin.`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33', // Red for danger
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Yes, Transfer Ownership',
        cancelButtonText: 'Cancel',
        background: '#1e293b',
        color: '#f8fafc'
    });

    return result.isConfirmed;
};

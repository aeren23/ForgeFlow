interface AuthProviderProps {
    children: React.ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
    // Auth initialization is synchronous in the store (reading from localStorage + JWT decode)
    // So we don't need any async hydration logic here!

    return <>{children}</>;
}

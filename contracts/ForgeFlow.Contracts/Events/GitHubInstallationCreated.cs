namespace ForgeFlow.Contracts.Events;

public record GitHubInstallationCreated(
    long InstallationId,
    string AccountLogin,
    string AccountType,
    DateTime Timestamp
);

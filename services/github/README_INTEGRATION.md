# GitHub Integration Documentation

This document describes the GitHub integration in ForgeFlow, specifically the flow from Issue Assignment to Branch Creation.

## Overview

When an issue is assigned to a user in ForgeFlow, the system automatically creates a feature branch in the linked GitHub repository.

### Workflow

1. **User action**: Issue is assigned in the UI.
2. **Work Service**:
   - Updates the issue status.
   - Publishes `IssueAssigned` event (MassTransit).
3. **GitHub Service**:
   - `IssueAssignedConsumer` receives the event.
   - Verifies if the project is linked to a GitHub repository (`RepositoryConnections` table).
   - If linked, it authenticates with GitHub using a JWT (App ID + Private Key).
   - Determines the base branch (checks `develop`, `master`, `main` or repository default).
   - Creates a new branch: `feature/{issueKey}-{slug}`.
   - Publishes `BranchCreated` event.
4. **Notification Service**:
   - Receives `BranchCreated` event.
   - Sends a SignalR notification to the user.

## Configuration (.env)

The following environment variables are critical for the GitHub Service:

```env
# GitHub App ID (from GitHub App settings -> General)
GITHUB_APP_ID=123456

# GitHub Private Key (Base64 encoded PEM)
# Encode your .pem file content to base64 to avoid newline issues in .env
GITHUB_PRIVATE_KEY_BASE64=LS0tLS1CRUdJTiBSU0EgUFJJVkFURSBLRVktLS0tLQ...

# GitHub Webhook Secret (for incoming webhooks)
GITHUB_WEBHOOK_SECRET=your_secret
```

### Private Key Format
The system uses a custom RSA implementation to generate JWTs. It supports Base64 encoded PEM keys. The key is automatically sanitized (whitespace removed) before decoding.

## Troubleshooting

### 401 Unauthorized (Bad Credentials)
- **Clock Skew**: The system automatically adjusts `iat` (Issued At) by -60 seconds to handle container time drift.
- **App ID**: Ensure `GITHUB_APP_ID` is correct.
- **Private Key**: Ensure the Base64 string is valid and corresponds to the App ID.

### 404 Not Found (Branch Creation)
- **Repository Link**: Ensure the project is linked to a valid repository.
- **Base Branch**: The system tries `develop` -> `master` -> `main`. If none exist, it fails. Ensure the repository has at least one valid branch.

### Logs
Check logs for `forgeflow-github` container:
```bash
docker logs forgeflow-github
```
Success log: `Created branch 'feature/...' from '...'`

## Development
- **Local Testing**: You can manually link a project using the `POST /api/installations/link` endpoint.
- **RabbitMQ**: Ensure RabbitMQ is running and `issue-assigned-github` queue is bound to the exchange.

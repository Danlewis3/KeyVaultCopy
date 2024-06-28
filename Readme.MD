# KeyVault Backup and Restore Azure Function

This Azure Function App provides the capability to back up secrets from an Azure Key Vault to Azure Blob Storage and restore them to another Key Vault. It uses managed identities to securely access both Key Vaults and the storage account.

## Prerequisites

- Azure Subscription
- Azure Storage Account
- Two Azure Key Vaults (one for backup and one for restore)
- Azure Function App with managed identity enabled
- Azure CLI or Azure Portal for setup

## Setup

### 1. Configure Azure Function App

1. **Enable Managed Identity for Azure Function App**:
   - Navigate to your Function App in the Azure Portal.
   - Under **Settings**, select **Identity**.
   - Turn the **Status** to **On** for the System-assigned identity.
   - Click **Save**.

2. **Assign Permissions to Managed Identity**:

   - **Key Vault Permissions**:
     - Navigate to your Key Vault.
     - Under **Settings**, select **Access policies**.
     - Click **+ Add Access Policy**.
     - Configure the access policy:
       - **Secret Permissions**: Get, List, Backup, and Restore.
       - **Principal**: Select your Function App's managed identity.
     - Click **Add** to save the access policy.

   - **Blob Storage Permissions**:
     - Navigate to your Storage Account.
     - Under **Security + networking**, select **Access control (IAM)**.
     - Click **+ Add > Add role assignment**.
     - Configure the role assignment:
       - **Role**: Storage Blob Data Contributor (or a more restrictive role if appropriate).
       - **Principal**: Select your Function App's managed identity.
     - Click **Save** to apply the role assignment.

### 2. Configure Environment Variables

1. Navigate to your Function App in the Azure Portal.
2. Under **Settings**, select **Configuration**.
3. Add the following application settings:
   - **Name**: `STORAGE_CONNECTION_STRING`
     - **Value**: `<your-storage-account-connection-string>`
   - **Name**: `STORAGE_CONTAINER_NAME`
     - **Value**: `<your-blob-container-name>`

## Usage 
**Backup Secrets**
1. To back up secrets from the Key Vault to Azure Blob Storage, send a POST request to the Azure Function endpoint with the following JSON payload:

> POST "https://<your-function-app-name>.azurewebsites.net/api/BackupRestoreFunction?code=<your-function-key>" \
>   -H "Content-Type: application/json" \
>  -d '{
>       "action": "backup",
>      "vaultName": "<sourceVaultName>"
>   }'

**Restore Secrets**
1.To restore secrets from Azure Blob Storage to another Key Vault, send a POST request to the Azure Function endpoint with the following JSON payload:

> curl -X POST "https://<your-function-app-name>.azurewebsites.net/api/BackupRestoreFunction?code=<your-function-key>" \
>     -H "Content-Type: application/json" \
>     -d '{
>           "action": "restore",
>           "destinationVaultName": "<destinationVaultName>"
>        }'

using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace KeyVaultBackupRestore
{
    public class BackupRestoreFunction
    {
        private readonly ILogger<BackupRestoreFunction> _logger;
        private readonly IConfiguration _configuration;

        public BackupRestoreFunction(ILogger<BackupRestoreFunction> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [Function("BackupRestoreFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("BackupRestoreFunction triggered.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            string action = data?.action;
            string vaultName = data?.vaultName;
            string destinationVaultName = data?.destinationVaultName;

            if (string.IsNullOrEmpty(action))
            {
                return new BadRequestObjectResult("Please provide 'action'.");
            }

            if (action.ToLower() == "backup" && string.IsNullOrEmpty(vaultName))
            {
                return new BadRequestObjectResult("Please provide 'vaultName' for backup.");
            }

            if (action.ToLower() == "restore" && string.IsNullOrEmpty(destinationVaultName))
            {
                return new BadRequestObjectResult("Please provide 'Target Key Vault Name' for restore.");
            }

            string storageConnectionString = _configuration["STORAGE_CONNECTION_STRING"];
            string containerName = _configuration["STORAGE_CONTAINER_NAME"];
            var blobServiceClient = new BlobServiceClient(storageConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var credential = new DefaultAzureCredential();

            try
            {
                if (action.ToLower() == "backup")
                {
                    var client = new SecretClient(new Uri($"https://{vaultName}.vault.azure.net/"), credential);

                    await foreach (var secretProperties in client.GetPropertiesOfSecretsAsync())
                    {
                        var secretName = secretProperties.Name;
                        var secretValue = await client.GetSecretAsync(secretName);
                        var blobClient = containerClient.GetBlobClient($"{secretName}.backup");
                        await blobClient.UploadAsync(new BinaryData(secretValue.Value.Value), true);
                    }

                    return new OkObjectResult("Backup of all secrets completed successfully.");
                }
                else if (action.ToLower() == "restore")
                {
                    var restoreClient = new SecretClient(new Uri($"https://{destinationVaultName}.vault.azure.net/"), credential);

                    await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
                    {
                        var blobClient = containerClient.GetBlobClient(blobItem.Name);
                        var downloadResponse = await blobClient.DownloadContentAsync();
                        var secretValue = downloadResponse.Value.Content.ToString();
                        var secretName = Path.GetFileNameWithoutExtension(blobItem.Name);
                        await restoreClient.SetSecretAsync(secretName, secretValue);
                    }

                    return new OkObjectResult("Restore from backup to vault completed successfully.");
                }
                else
                {
                    return new BadRequestObjectResult("Invalid action. Please use 'backup' or 'restore'.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred");
                return new ObjectResult("An error occurred") { StatusCode = 500 };
            }
        }
    }
}
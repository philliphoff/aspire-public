using Aspire.Hosting.Azure;
using Azure.Provisioning.AppContainers;

namespace Aspire.Hosting;

internal static class Extensions
{
    private sealed record SecretMapping(string OriginalName, IAzureKeyVaultSecretReference Reference);

    public static IResourceBuilder<T> PublishWithContainerAppSecrets<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<AzureKeyVaultResource>? keyVault = null,
        string[]? hostKeyNames = null,
        string[]? systemKeyExtensionNames = null)
        where T : AzureFunctionsProjectResource
    {
        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        keyVault ??= builder.ApplicationBuilder.AddAzureKeyVault("functions-keys");

        var hostKeysToAdd = (hostKeyNames ?? []).Append("default").Select(k => $"host-function-{k}");
        var systemKeysToAdd = systemKeyExtensionNames?.Select(k => $"host-systemKey-{k}_extension") ?? [];
        var secrets = hostKeysToAdd.Union(systemKeysToAdd)
            .Select(secretName => new SecretMapping(
                secretName,
                CreateSecretIfNotExists(builder.ApplicationBuilder, keyVault, secretName.Replace("_", "-"))
            )).ToList();

        return builder
            .WithReference(keyVault)
            .WithEnvironment("AzureWebJobsSecretStorageType", "ContainerApps")
            .PublishAsAzureContainerApp((infra, app) => ConfigureFunctionsContainerApp(infra, app, builder.Resource, secrets));
    }

    private static void ConfigureFunctionsContainerApp(
        AzureResourceInfrastructure infrastructure, 
        ContainerApp containerApp, 
        IResource resource, 
        List<SecretMapping> secrets)
    {
        const string volumeName = "functions-keys";
        const string mountPath = "/run/secrets/functions-keys";

        var appIdentityAnnotation = resource.Annotations.OfType<AppIdentityAnnotation>().Last();
        var containerAppIdentityId = appIdentityAnnotation.IdentityResource.Id.AsProvisioningParameter(infrastructure);

        var containerAppSecretsVolume = new ContainerAppVolume
        {
            Name = volumeName,
            StorageType = ContainerAppStorageType.Secret
        };

        foreach (var mapping in secrets)
        {
            var secret = mapping.Reference.AsKeyVaultSecret(infrastructure);

            containerApp.Configuration.Secrets.Add(new ContainerAppWritableSecret()
            {
                Name = mapping.Reference.SecretName.ToLowerInvariant(),
                KeyVaultUri = secret.Properties.SecretUri,
                Identity = containerAppIdentityId
            });

            containerAppSecretsVolume.Secrets.Add(new SecretVolumeItem
            {
                Path = mapping.OriginalName.Replace("-", "."),
                SecretRef = mapping.Reference.SecretName.ToLowerInvariant()
            });
        }

        containerApp.Template.Containers[0].Value!.VolumeMounts.Add(new ContainerAppVolumeMount
        {
            VolumeName = volumeName,
            MountPath = mountPath
        });
        containerApp.Template.Volumes.Add(containerAppSecretsVolume);
    }

    public static IAzureKeyVaultSecretReference CreateSecretIfNotExists(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<AzureKeyVaultResource> keyVault,
        string secretName)
    {
        var secretParameter = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"param-{secretName}", special: false);
        builder.AddBicepTemplateString($"key-vault-key-{secretName}", """
                param location string = resourceGroup().location
                param keyVaultName string
                param secretName string
                @secure()
                param secretValue string    

                // Reference the existing Key Vault
                resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
                  name: keyVaultName
                }

                // Deploy the secret only if it does not already exist
                @onlyIfNotExists()
                resource newSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
                  parent: keyVault
                  name: secretName
                  properties: {
                      value: secretValue
                  }
                }
                """)
            .WithParameter("keyVaultName", keyVault.GetOutput("name"))
            .WithParameter("secretName", secretName)
            .WithParameter("secretValue", secretParameter);

        return keyVault.GetSecret(secretName);
    }
}
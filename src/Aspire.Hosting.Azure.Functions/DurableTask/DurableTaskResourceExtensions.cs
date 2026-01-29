// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.DurableTask;
using Azure.Provisioning;
using Azure.Provisioning.DurableTask;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding and configuring Durable Task resources within a distributed application.
/// </summary>
public static class DurableTaskResourceExtensions
{
    /// <summary>
    /// Adds a Durable Task scheduler resource to the distributed application.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The logical name of the scheduler resource.</param>
    /// <returns>An <see cref="IResourceBuilder{TResource}"/> for the scheduler resource.</returns>
    /// <example>
    /// Add a Durable Task scheduler resource:
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// var scheduler = builder.AddDurableTaskScheduler("scheduler");
    /// </code>
    /// </example>
    public static IResourceBuilder<DurableTaskSchedulerResource> AddDurableTaskScheduler(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        builder.AddAzureProvisioning();

        var configureInfrastructure = static (AzureResourceInfrastructure infrastructure) =>
        {
            var aspireResource = (DurableTaskSchedulerResource)infrastructure.AspireResource;

            // Create the Durable Task Scheduler resource using the custom provisioning resource
            var scheduler = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(
                infrastructure,
                (identifier, name) =>
                {
                    var resource = DurableTaskSchedulerProvisioningResource.FromExisting(identifier);
                    resource.Name = name;
                    return resource;
                },
                (infra) =>
                {
                    var skuParameter = new ProvisioningParameter("sku", typeof(string))
                    {
                        Value = "Consumption"
                    };
                    infra.Add(skuParameter);

                    var resource = new DurableTaskSchedulerProvisioningResource(infra.AspireResource.GetBicepIdentifier())
                    {
                        Name = infra.AspireResource.Name,
                        Location = new ProvisioningParameter(AzureBicepResource.KnownParameters.Location, typeof(string)),
                        SkuName = skuParameter,
                        IpAllowlist = ["0.0.0.0/0"]
                    };
                    return resource;
                });

            // Output the scheduler endpoint for connection string construction
            infrastructure.Add(new ProvisioningOutput("schedulerEndpoint", typeof(string))
            {
                Value = scheduler.Endpoint
            });

            // Output the name for role assignments
            infrastructure.Add(new ProvisioningOutput("name", typeof(string)) { Value = scheduler.Name });

            // Create TaskHub sub-resources
            foreach (var hub in aspireResource.Hubs)
            {
                var taskHub = hub.ToProvisioningEntity();
                taskHub.Parent = scheduler;
                infrastructure.Add(taskHub);
            }
        };

        var scheduler = new DurableTaskSchedulerResource(name, configureInfrastructure);

        // Note: Role assignments are NOT added to the Scheduler by default.
        // Instead, they should be added to TaskHubs (principle of least privilege).
        // Use WithRoleAssignments(scheduler, ...) to explicitly grant roles on the Scheduler if needed.
        return builder.AddResource(scheduler);
    }

    /// <summary>
    /// Configures the Durable Task scheduler to use an existing scheduler instance referenced by the provided connection string.
    /// No new scheduler resource is provisioned.
    /// </summary>
    /// <param name="builder">The scheduler resource builder.</param>
    /// <param name="connectionString">The connection string referencing the existing Durable Task scheduler instance.</param>
    /// <returns>The same <see cref="IResourceBuilder{DurableTaskSchedulerResource}"/> instance for fluent chaining.</returns>
    /// <remarks>
    /// The existing resource annotation is only applied when the execution context is not in publish mode.
    /// </remarks>
    /// <example>
    /// Use an existing scheduler instead of provisioning a new one:
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// var scheduler = builder.AddDurableTaskScheduler("scheduler")
    ///     .RunAsExisting("Endpoint=https://example;...;");
    /// </code>
    /// </example>
    public static IResourceBuilder<DurableTaskSchedulerResource> RunAsExisting(this IResourceBuilder<DurableTaskSchedulerResource> builder, string connectionString)
    {
        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            builder.WithAnnotation(new DurableTaskSchedulerConnectionStringAnnotation(connectionString));
        }

        return builder;
    }

    /// <summary>
    /// Configures the Durable Task scheduler to use an existing scheduler instance referenced by the provided connection string.
    /// No new scheduler resource is provisioned.
    /// </summary>
    /// <param name="builder">The scheduler resource builder.</param>
    /// <param name="connectionString">The connection string parameter referencing the existing Durable Task scheduler instance.</param>
    /// <returns>The same <see cref="IResourceBuilder{DurableTaskSchedulerResource}"/> instance for fluent chaining.</returns>
    /// <remarks>
    /// The existing resource annotation is only applied when the execution context is not in publish mode.
    /// </remarks>
    /// <example>
    /// Use an existing scheduler where the connection string is supplied via a parameter:
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// var schedulerConnectionString = builder.AddParameter("schedulerConnectionString");
    ///
    /// var scheduler = builder.AddDurableTaskScheduler("scheduler")
    ///     .RunAsExisting(schedulerConnectionString);
    /// </code>
    /// </example>
    public static IResourceBuilder<DurableTaskSchedulerResource> RunAsExisting(this IResourceBuilder<DurableTaskSchedulerResource> builder, IResourceBuilder<ParameterResource> connectionString)
    {
        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            builder.WithAnnotation(new DurableTaskSchedulerConnectionStringAnnotation(connectionString.Resource));
        }

        return builder;
    }

    /// <summary>
    /// Configures the Durable Task scheduler to run using the local emulator (only in non-publish modes).
    /// </summary>
    /// <param name="builder">The resource builder for the scheduler.</param>
    /// <param name="configureContainer">Callback that exposes underlying container used for emulation to allow for customization.</param>
    /// <returns>The same <see cref="IResourceBuilder{DurableTaskSchedulerResource}"/> instance for chaining.</returns>
    /// <example>
    /// Run the scheduler locally using the emulator:
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// var scheduler = builder.AddDurableTaskScheduler("scheduler")
    ///     .RunAsEmulator();
    /// </code>
    /// </example>
    public static IResourceBuilder<DurableTaskSchedulerResource> RunAsEmulator(this IResourceBuilder<DurableTaskSchedulerResource> builder, Action<IResourceBuilder<DurableTaskSchedulerEmulatorResource>>? configureContainer = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        // Mark this resource as an emulator for consistent resource identification and tooling support
        builder.WithAnnotation(new EmulatorResourceAnnotation());

        builder.WithEndpoint(name: "grpc", targetPort: 8080)
               .WithHttpEndpoint(name: "http", targetPort: 8081)
               .WithHttpEndpoint(name: "dashboard", targetPort: 8082)
               .WithUrlForEndpoint("dashboard", c => c.DisplayText = "Scheduler Dashboard")
               .WithAnnotation(new ContainerImageAnnotation
               {
                   Registry = DurableTaskSchedulerEmulatorContainerImageTags.Registry,
                   Image = DurableTaskSchedulerEmulatorContainerImageTags.Image,
                   Tag = DurableTaskSchedulerEmulatorContainerImageTags.Tag
               });

        var emulatorResource = new DurableTaskSchedulerEmulatorResource(builder.Resource);

        var surrogateBuilder =
            builder
                .ApplicationBuilder
                .CreateResourceBuilder(emulatorResource)
                .WithEnvironment(
                    context =>
                    {
                        ReferenceExpressionBuilder namesBuilder = new();

                        var durableTaskHubNames =
                            builder
                                .ApplicationBuilder
                                .Resources
                                .OfType<DurableTaskHubResource>()
                                .Where(th => th.Parent == builder.Resource)
                                .Select(th => th.TaskHubName)
                                .ToList();

                        for (int i = 0; i < durableTaskHubNames.Count; i++)
                        {
                            if (i > 0)
                            {
                                namesBuilder.AppendLiteral(", ");
                            }

                            namesBuilder.AppendFormatted(durableTaskHubNames[i]);
                        }

                        context.EnvironmentVariables["DTS_TASK_HUB_NAMES"] = namesBuilder.Build();
                    });

        configureContainer?.Invoke(surrogateBuilder);

        return builder;
    }

    /// <summary>
    /// Adds a Durable Task hub resource associated with the specified scheduler.
    /// </summary>
    /// <param name="builder">The scheduler resource builder.</param>
    /// <param name="name">The logical name of the task hub resource.</param>
    /// <returns>An <see cref="IResourceBuilder{TResource}"/> for the task hub resource.</returns>
    /// <remarks>
    /// By default, resources that reference the TaskHub will be assigned the following role:
    /// <list type="bullet">
    /// <item><see cref="DurableTaskSchedulerBuiltInRole.DurableTaskDataContributor"/></item>
    /// </list>
    /// This follows the principle of least privilege by granting access to the specific TaskHub
    /// rather than the parent Scheduler. Use <see cref="WithRoleAssignments{T}(IResourceBuilder{T}, IResourceBuilder{DurableTaskHubResource}, DurableTaskSchedulerBuiltInRole[])"/>
    /// to customize the roles assigned.
    /// </remarks>
    /// <example>
    /// Add a task hub under a scheduler:
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// var scheduler = builder.AddDurableTaskScheduler("scheduler").RunAsEmulator();
    ///
    /// var hub = scheduler.AddTaskHub("hub")
    ///     .WithTaskHubName("MyTaskHub");
    /// </code>
    /// </example>
    public static IResourceBuilder<DurableTaskHubResource> AddTaskHub(this IResourceBuilder<DurableTaskSchedulerResource> builder, [ResourceName] string name)
    {
        var hub = new DurableTaskHubResource(name, builder.Resource);

        builder.Resource.Hubs.Add(hub);

        var hubBuilder = builder.ApplicationBuilder.AddResource(hub)
            .WithDefaultRoleAssignments(DurableTaskSchedulerBuiltInRole.GetBuiltInRoleName,
                DurableTaskSchedulerBuiltInRole.DurableTaskDataContributor);

        hubBuilder.OnResourceReady(
            async (r, e, ct) =>
            {
                var notifications = e.Services.GetRequiredService<ResourceNotificationService>();

                string? url = null;
                if (builder.Resource.IsEmulator)
                {
                    var dashboardUrl = await r.Parent.GetEmulatorDashboardUrl().GetValueAsync(ct).ConfigureAwait(false);
                    var taskHubName = await r.TaskHubName.GetValueAsync(ct).ConfigureAwait(false);
                    url = $"{dashboardUrl}/subscriptions/default/schedulers/default/taskhubs/{taskHubName}";
                }

                await notifications.PublishUpdateAsync(r, snapshot => snapshot with
                {
                    State = KnownResourceStates.Running,
                    Urls = url is not null
                        ? [new("dashboard", url, false) { DisplayProperties = new() { DisplayName = "Task Hub Dashboard" } }]
                        : []
                }).ConfigureAwait(false);
            });

        return hubBuilder;
    }

    /// <summary>
    /// Sets the name of the Durable Task hub.
    /// </summary>
    /// <param name="builder">The task hub resource builder.</param>
    /// <param name="taskHubName">The name of the Task Hub.</param>
    /// <returns>The same <see cref="IResourceBuilder{DurableTaskHubResource}"/> instance for fluent chaining.</returns>
    /// <example>
    /// Set the task hub name:
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// var scheduler = builder.AddDurableTaskScheduler("scheduler").RunAsEmulator();
    /// var hub = scheduler.AddTaskHub("hub").WithTaskHubName("MyTaskHub");
    /// </code>
    /// </example>
    public static IResourceBuilder<DurableTaskHubResource> WithTaskHubName(this IResourceBuilder<DurableTaskHubResource> builder, string taskHubName)
    {
        return builder.WithAnnotation(new DurableTaskHubNameAnnotation(taskHubName));
    }

    /// <summary>
    /// Sets the name of the Durable Task hub using a parameter resource.
    /// </summary>
    /// <param name="builder">The task hub resource builder.</param>
    /// <param name="taskHubName">A parameter resource that resolves to the Task Hub name.</param>
    /// <returns>The same <see cref="IResourceBuilder{DurableTaskHubResource}"/> instance for fluent chaining.</returns>
    /// <example>
    /// Set the task hub name from a parameter:
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// var taskHubName = builder.AddParameter("taskHubName");
    ///
    /// var scheduler = builder.AddDurableTaskScheduler("scheduler").RunAsEmulator();
    /// var hub = scheduler.AddTaskHub("hub").WithTaskHubName(taskHubName);
    /// </code>
    /// </example>
    public static IResourceBuilder<DurableTaskHubResource> WithTaskHubName(this IResourceBuilder<DurableTaskHubResource> builder, IResourceBuilder<ParameterResource> taskHubName)
    {
        return builder.WithAnnotation(new DurableTaskHubNameAnnotation(taskHubName.Resource));
    }

    /// <summary>
    /// Assigns the specified roles to the given resource, granting it the necessary permissions
    /// on the target Durable Task scheduler. This replaces the default role assignments for the resource.
    /// </summary>
    /// <param name="builder">The resource to which the specified roles will be assigned.</param>
    /// <param name="target">The target Durable Task scheduler.</param>
    /// <param name="roles">The built-in Durable Task Scheduler roles to be assigned.</param>
    /// <returns>The updated <see cref="IResourceBuilder{T}"/> with the applied role assignments.</returns>
    /// <remarks>
    /// Use this method when you need to grant permissions at the Scheduler level rather than
    /// the TaskHub level. For most use cases, prefer using <see cref="WithRoleAssignments{T}(IResourceBuilder{T}, IResourceBuilder{DurableTaskHubResource}, DurableTaskSchedulerBuiltInRole[])"/>
    /// to grant permissions on specific TaskHubs instead (principle of least privilege).
    /// <example>
    /// Assigns the DurableTaskDataOwner role to the 'Projects.Api' project on the scheduler.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var scheduler = builder.AddDurableTaskScheduler("scheduler");
    ///
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithRoleAssignments(scheduler, DurableTaskSchedulerBuiltInRole.DurableTaskDataOwner);
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<T> WithRoleAssignments<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<DurableTaskSchedulerResource> target,
        params DurableTaskSchedulerBuiltInRole[] roles)
        where T : IResource
    {
        return builder.WithRoleAssignments(target, DurableTaskSchedulerBuiltInRole.GetBuiltInRoleName, roles);
    }

    /// <summary>
    /// Assigns the specified roles to the given resource, granting it the necessary permissions
    /// on the target Durable Task hub. This replaces the default role assignments for the resource.
    /// </summary>
    /// <param name="builder">The resource to which the specified roles will be assigned.</param>
    /// <param name="target">The target Durable Task hub.</param>
    /// <param name="roles">The built-in Durable Task Scheduler roles to be assigned.</param>
    /// <returns>The updated <see cref="IResourceBuilder{T}"/> with the applied role assignments.</returns>
    /// <remarks>
    /// <example>
    /// Assigns the DurableTaskDataContributor role to the 'Projects.Api' project on a specific TaskHub.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var scheduler = builder.AddDurableTaskScheduler("scheduler");
    /// var hub = scheduler.AddTaskHub("hub");
    ///
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithRoleAssignments(hub, DurableTaskSchedulerBuiltInRole.DurableTaskDataContributor)
    ///   .WithReference(hub);
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<T> WithRoleAssignments<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<DurableTaskHubResource> target,
        params DurableTaskSchedulerBuiltInRole[] roles)
        where T : IResource
    {
        return builder.WithRoleAssignments(target, DurableTaskSchedulerBuiltInRole.GetBuiltInRoleName, roles);
    }
}

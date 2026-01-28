// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning.Primitives;

namespace Azure.Provisioning.DurableTask;

/// <summary>
/// A custom provisioning resource for Azure Durable Task Scheduler.
/// This is used until an official Azure.Provisioning.DurableTask package is available.
/// </summary>
internal sealed class DurableTaskSchedulerProvisioningResource : ProvisionableResource
{
    /// <summary>
    /// Creates a new instance of <see cref="DurableTaskSchedulerProvisioningResource"/>.
    /// </summary>
    /// <param name="bicepIdentifier">The Bicep identifier for this resource.</param>
    /// <param name="resourceVersion">The API version for the resource.</param>
    public DurableTaskSchedulerProvisioningResource(string bicepIdentifier, string? resourceVersion = "2025-04-01-preview")
        : base(bicepIdentifier, new Azure.Core.ResourceType("Microsoft.DurableTask/schedulers"), resourceVersion)
    {
    }

    /// <summary>
    /// Gets or sets the name of the Durable Task Scheduler.
    /// </summary>
    public BicepValue<string> Name
    {
        get { Initialize(); return _name!; }
        set { Initialize(); _name!.Assign(value); }
    }
    private BicepValue<string>? _name;

    /// <summary>
    /// Gets or sets the location of the Durable Task Scheduler.
    /// </summary>
    public BicepValue<string> Location
    {
        get { Initialize(); return _location!; }
        set { Initialize(); _location!.Assign(value); }
    }
    private BicepValue<string>? _location;

    /// <summary>
    /// Gets or sets the SKU of the Durable Task Scheduler.
    /// </summary>
    public BicepValue<string> SkuName
    {
        get { Initialize(); return _skuName!; }
        set { Initialize(); _skuName!.Assign(value); }
    }
    private BicepValue<string>? _skuName;

    /// <summary>
    /// Gets or sets the SKU capacity of the Durable Task Scheduler.
    /// </summary>
    public BicepValue<int> SkuCapacity
    {
        get { Initialize(); return _skuCapacity!; }
        set { Initialize(); _skuCapacity!.Assign(value); }
    }
    private BicepValue<int>? _skuCapacity;

    /// <summary>
    /// Gets the endpoint of the Durable Task Scheduler (output).
    /// </summary>
    public BicepValue<string> Endpoint
    {
        get { Initialize(); return _endpoint!; }
    }
    private BicepValue<string>? _endpoint;

    /// <inheritdoc/>
    protected override void DefineProvisionableProperties()
    {
        _name = DefineProperty<string>(nameof(Name), ["name"], isOutput: false, isRequired: true);
        _location = DefineProperty<string>(nameof(Location), ["location"], isOutput: false, isRequired: true);
        _skuName = DefineProperty<string>(nameof(SkuName), ["sku", "name"], isOutput: false, isRequired: false);
        _skuCapacity = DefineProperty<int>(nameof(SkuCapacity), ["sku", "capacity"], isOutput: false, isRequired: false);
        _endpoint = DefineProperty<string>(nameof(Endpoint), ["properties", "endpoint"], isOutput: true, isRequired: false);
    }

    /// <summary>
    /// Creates a reference to an existing Durable Task Scheduler resource.
    /// </summary>
    /// <param name="bicepIdentifier">The Bicep identifier for this resource.</param>
    /// <param name="resourceVersion">The API version for the resource.</param>
    /// <returns>A new instance configured as an existing resource reference.</returns>
    public static DurableTaskSchedulerProvisioningResource FromExisting(string bicepIdentifier, string? resourceVersion = "2025-04-01-preview")
    {
        return new DurableTaskSchedulerProvisioningResource(bicepIdentifier, resourceVersion)
        {
            IsExistingResource = true
        };
    }
}

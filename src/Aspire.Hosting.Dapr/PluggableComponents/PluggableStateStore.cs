// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Dapr.PluggableComponents.Components;
using Dapr.PluggableComponents.Components.StateStore;

namespace Aspire.Hosting.Dapr.PluggableComponents;

sealed class PluggableStateStore : IStateStore
{
    public Task InitAsync(MetadataRequest request, CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.CompletedTask;
    }

    public Task DeleteAsync(StateStoreDeleteRequest request, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public Task<StateStoreGetResponse?> GetAsync(StateStoreGetRequest request, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public Task SetAsync(StateStoreSetRequest request, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }
}

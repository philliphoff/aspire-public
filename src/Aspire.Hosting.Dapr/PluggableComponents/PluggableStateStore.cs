// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Dapr.PluggableComponents.Components;
using Dapr.PluggableComponents.Components.StateStore;

namespace Aspire.Hosting.Dapr.PluggableComponents;

sealed class PluggableStateStore : IStateStore
{
    private readonly ConcurrentDictionary<string, ReadOnlyMemory<byte>> _store = new();

    public Task InitAsync(MetadataRequest request, CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.CompletedTask;
    }

    public Task DeleteAsync(StateStoreDeleteRequest request, CancellationToken cancellationToken = new CancellationToken())
    {
        _store.TryRemove(request.Key, out _);

        return Task.CompletedTask;
    }

    public Task<StateStoreGetResponse?> GetAsync(StateStoreGetRequest request, CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.FromResult(
            _store.TryGetValue(request.Key, out var value)
                ? new StateStoreGetResponse { Data = value.ToArray() }
                : null);
    }

    public Task SetAsync(StateStoreSetRequest request, CancellationToken cancellationToken = new CancellationToken())
    {
        _store.AddOrUpdate(request.Key, request.Value, (_, _) => request.Value);

        return Task.CompletedTask;
    }
}

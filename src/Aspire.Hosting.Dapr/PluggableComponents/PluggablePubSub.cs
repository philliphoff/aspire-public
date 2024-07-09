// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Dapr.PluggableComponents.Components;
using Dapr.PluggableComponents.Components.PubSub;

namespace Aspire.Hosting.Dapr.PluggableComponents;

sealed class PluggablePubSub : IPubSub
{
    public Task InitAsync(MetadataRequest request, CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.CompletedTask;
    }

    public Task PublishAsync(PubSubPublishRequest request, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public Task PullMessagesAsync(PubSubPullMessagesTopic topic, MessageDeliveryHandler<string?, PubSubPullMessagesResponse> deliveryHandler, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }
}

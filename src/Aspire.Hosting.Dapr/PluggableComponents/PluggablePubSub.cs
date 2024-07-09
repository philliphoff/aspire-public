// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Threading.Channels;
using Dapr.PluggableComponents.Components;
using Dapr.PluggableComponents.Components.PubSub;

namespace Aspire.Hosting.Dapr.PluggableComponents;

sealed class PluggablePubSub : IPubSub
{
    private readonly ConcurrentDictionary<string, Channel<ReadOnlyMemory<byte>>> _topics = new();

    public Task InitAsync(MetadataRequest request, CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.CompletedTask;
    }

    public async Task PublishAsync(PubSubPublishRequest request, CancellationToken cancellationToken = new CancellationToken())
    {
        var channel = _topics.AddOrUpdate(request.Topic, _ => Channel.CreateUnbounded<ReadOnlyMemory<byte>>(), (_, channel) => channel);

        await channel.Writer.WriteAsync(request.Data, cancellationToken).ConfigureAwait(false);
    }

    public async Task PullMessagesAsync(PubSubPullMessagesTopic topic, MessageDeliveryHandler<string?, PubSubPullMessagesResponse> deliveryHandler, CancellationToken cancellationToken = new CancellationToken())
    {
        var channel = _topics.AddOrUpdate(topic.Name, _ => Channel.CreateUnbounded<ReadOnlyMemory<byte>>(), (_, channel) => channel);

        while (!cancellationToken.IsCancellationRequested)
        {
            await channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false);

            while (channel.Reader.TryRead(out var message))
            {
                await deliveryHandler.Invoke(
                    new PubSubPullMessagesResponse(topic.Name) { Data = message.ToArray() },
                    async errorMessage =>
                    {
                        if (errorMessage is not null)
                        {
                            // TODO: Defer adding the message back to the channel for some timeout period.
                            // TODO: Optionally, add a retry count to the message and discard it after a certain number of retries.
                            await channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
                        }
                    }).ConfigureAwait(false);
            }
        }
    }
}

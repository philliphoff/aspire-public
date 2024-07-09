// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Dapr.PluggableComponents;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using BindingFlags = System.Reflection.BindingFlags;

namespace Aspire.Hosting.Dapr.PluggableComponents;

sealed class PluggableComponentsManager(ILogger<PluggableComponentsManager> logger): IAsyncDisposable
{
    private DaprPluggableComponentsApplication? _app;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var builder = WebApplication.CreateEmptyBuilder(
            new()
            {
            });

        builder.Logging.AddConsole();

        builder.WebHost.UseKestrelCore();

        var constructor = typeof(DaprPluggableComponentsApplication).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, [typeof(WebApplicationBuilder)]);

        if (constructor is null)
        {
            throw new InvalidOperationException("Unable to find constructor.");
        }

        _app = (DaprPluggableComponentsApplication)constructor.Invoke([builder]);

        logger.LogInformation("Starting Dapr pluggable components...");

        _app.RegisterService(
            new DaprPluggableComponentsServiceOptions("aspirestore")
            {
                // TODO: Adjust the socket folder for the platform.
                SocketFolder = "/tmp/dapr-components-sockets"
            },
            serviceBuilder =>
            {
                serviceBuilder.RegisterPubSub<PluggablePubSub>();
                serviceBuilder.RegisterStateStore<PluggableStateStore>();
            });

        await _app.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            logger.LogInformation("Shutting down Dapr pluggable components...");

            await _app.DisposeAsync().ConfigureAwait(false);
        }
    }
}

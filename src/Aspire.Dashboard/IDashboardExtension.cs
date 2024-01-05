// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Aspire.Dashboard;

public interface IDashboardExtension
{
    void ConfigureRoutes(IEndpointRouteBuilder builder);
}

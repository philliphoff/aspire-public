var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator();

var scheduler = builder.AddDurableTaskScheduler("scheduler").RunAsEmulator();

var taskHub = scheduler.AddTaskHub("taskhub");

builder.AddAzureContainerAppEnvironment("cae");

builder.AddAzureFunctionsProject<Projects.AzureFunctionsWithDts_Functions>("funcapp")
    .WithExternalHttpEndpoints()
    .WithHostStorage(storage)
    .WithReference(taskHub)
    .PublishWithContainerAppSecrets(systemKeyExtensionNames: ["durabletask"]);

builder.Build().Run();

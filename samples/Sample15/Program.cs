﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.MongoDb.Extensions;
using Elsa.Runtime;
using Elsa.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample14;

namespace Sample15
{
    /// <summary>
    /// A simple demonstration of using the MongoDB persistence providers.
    /// If you don't have MongoDB installed but you do have Docker, run `docker-compose up` to run a container with MongoDB (see the 'docker-compose.yaml' file). 
    /// </summary>
    class Program
    {
        static async Task Main()
        {
            var services = BuildServices();

            // Create a workflow definition.
            var registry = services.GetService<IWorkflowRegistry>();
            var workflowDefinition = registry.RegisterWorkflow<HelloWorldWorkflow>();

            // Mark this definition as the "latest" version.
            workflowDefinition.IsLatest = true;
            workflowDefinition.Version = 1;

            using (var scope = services.CreateScope())
            {
                // Persist the workflow definition.
                var definitionStore = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
                await definitionStore.SaveAsync(workflowDefinition);

                // Load the workflow definition.
                workflowDefinition = await definitionStore.GetByIdAsync(workflowDefinition.Id, VersionOptions.Latest);

                // Execute the workflow.
                var invoker = scope.ServiceProvider.GetRequiredService<IWorkflowInvoker>();
                var executionContext = await invoker.InvokeAsync(workflowDefinition);

                // Persist the workflow instance.
                var instanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
                var workflowInstance = executionContext.Workflow.ToInstance();
                await instanceStore.SaveAsync(workflowInstance);
            }
        }

        private static IServiceProvider BuildServices()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                    new Dictionary<string, string>
                    {
                        ["ConnectionStrings:MongoDb"] = "mongodb://localhost"
                    }
                )
                .Build();

            return new ServiceCollection()
                .AddWorkflowsCore()
                .AddStartupRunner()
                .AddConsoleActivities()
                .AddMongoDb(configuration, "Elsa", "MongoDb")
                .AddMongoDbWorkflowDefinitionStore()
                .AddMongoDbWorkflowInstanceStore()
                .BuildServiceProvider();
        }
    }
}
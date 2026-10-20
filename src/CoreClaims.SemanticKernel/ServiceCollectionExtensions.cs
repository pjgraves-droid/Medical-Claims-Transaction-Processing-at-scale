using Azure.AI.OpenAI;
using Azure.Core;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CoreClaims.SemanticKernel
{
    public static class ServiceCollectionExtensions
    {
        public const string ConfigSectionName = "RulesEngine";

        public static IServiceCollection AddRulesEngine(this IServiceCollection services, IConfiguration configuration, TokenCredential credential)
        {
            services.Configure<RulesEngineSettings>(configuration.GetSection(ConfigSectionName));

            services.AddSingleton<IChatClient>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<RulesEngineSettings>>().Value;
                return new AzureOpenAIClient(new Uri(settings.OpenAIEndpoint), credential)
                    .GetChatClient(settings.OpenAICompletionsDeployment)
                    .AsIChatClient();
            });

            services.AddSingleton<IRulesEngine, RulesEngine>();

            return services;
        }
    }
}

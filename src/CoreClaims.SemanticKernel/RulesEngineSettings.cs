namespace CoreClaims.SemanticKernel
{
    public record RulesEngineSettings
    {
        public required string OpenAIEndpoint { get; set; }
        public required string OpenAICompletionsDeployment { get; set; }
    }
}

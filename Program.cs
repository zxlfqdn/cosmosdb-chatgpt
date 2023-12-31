using Cosmos.Chat.GPT.Options;
using Cosmos.Chat.GPT.Services;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.RegisterConfiguration();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();
builder.Services.RegisterServices();
// builder.Services.AddAuthentication();
// builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

await app.RunAsync();

static class ProgramExtensions
{
    public static void RegisterConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<CosmosDb>()
            .Bind(builder.Configuration.GetSection(nameof(CosmosDb)));

        builder.Services.AddOptions<OpenAi>()
            .Bind(builder.Configuration.GetSection(nameof(OpenAi)));
    }

    public static void RegisterServices(this IServiceCollection services)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);

        services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(options => configuration.Bind("AzureAd", options));

        services.AddScoped<CosmosDbService, CosmosDbService>((provider) =>
        {
            var cosmosDbOptions = provider.GetRequiredService<IOptions<CosmosDb>>();
            if (cosmosDbOptions is null)
            {
                throw new ArgumentException($"{nameof(IOptions<CosmosDb>)} was not resolved through dependency injection.");
            }
            else
            {
                return new CosmosDbService(
                    endpoint: cosmosDbOptions.Value?.Endpoint ?? String.Empty,
                    key: cosmosDbOptions.Value?.Key ?? String.Empty,
                    databaseName: cosmosDbOptions.Value?.Database ?? String.Empty,
                    containerName: cosmosDbOptions.Value?.Container ?? String.Empty
                );
            }
        });
        services.AddScoped<OpenAiService, OpenAiService>((provider) =>
        {
            var openAiOptions = provider.GetRequiredService<IOptions<OpenAi>>();
            if (openAiOptions is null)
            {
                throw new ArgumentException($"{nameof(IOptions<OpenAi>)} was not resolved through dependency injection.");
            }
            else
            {
                return new OpenAiService(
                    endpoint: openAiOptions.Value?.Endpoint ?? String.Empty,
                    key: openAiOptions.Value?.Key ?? String.Empty,
                    modelName: openAiOptions.Value?.ModelName ?? String.Empty
                );
            }
        });
        services.AddScoped<ChatService>((provider) =>
        {
            var openAiOptions = provider.GetRequiredService<IOptions<OpenAi>>();
            if (openAiOptions is null)
            {
                throw new ArgumentException($"{nameof(IOptions<OpenAi>)} was not resolved through dependency injection.");
            }
            else
            {
                var cosmosDbService = provider.GetRequiredService<CosmosDbService>();
                var openAiService = provider.GetRequiredService<OpenAiService>();

                return new ChatService(
                    openAiService: openAiService,
                    cosmosDbService: cosmosDbService,
                    maxConversationTokens: openAiOptions.Value?.MaxConversationTokens ?? String.Empty
                );
            }
        });
    }
}

﻿using Azure;
using Azure.AI.OpenAI;
using Cosmos.Chat.GPT.Models;

namespace Cosmos.Chat.GPT.Services;

/// <summary>
/// Service to access Azure OpenAI.
/// </summary>
public class OpenAiService
{
    private readonly string _modelName = String.Empty;
    private readonly OpenAIClient _client;

    /// <summary>
    /// System prompt to send with user prompts to instruct the model for chat session
    /// </summary>
    private readonly string _systemPrompt = @"
        あなたは質問を解決する為のAIアシスタントです。必ず日本語で丁寧、正確と簡潔な回答してください。" + Environment.NewLine;
    
    /// <summary>    
    /// System prompt to send with user prompts to instruct the model for summarization
    /// </summary>
    private readonly string _summarizePrompt = @"
        请用1个或2个词总结对话的内容以用作这段对话的标题。必ず日本語で" + Environment.NewLine;

    /// <summary>
    /// Creates a new instance of the service.
    /// </summary>
    /// <param name="endpoint">Endpoint URI.</param>
    /// <param name="key">Account key.</param>
    /// <param name="modelName">Name of the deployed Azure OpenAI model.</param>
    /// <exception cref="ArgumentNullException">Thrown when endpoint, key, or modelName is either null or empty.</exception>
    /// <remarks>
    /// This constructor will validate credentials and create a HTTP client instance.
    /// </remarks>
    public OpenAiService(string endpoint, string key, string modelName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(modelName);
        ArgumentNullException.ThrowIfNullOrEmpty(endpoint);
        ArgumentNullException.ThrowIfNullOrEmpty(key);

        _modelName = modelName;

        _client = new(new Uri(endpoint), new AzureKeyCredential(key));
    }

    /// <summary>
    /// Sends a prompt to the deployed OpenAI LLM model and returns the response.
    /// </summary>
    /// <param name="sessionId">Chat session identifier for the current conversation.</param>
    /// <param name="prompt">Prompt message to send to the deployment.</param>
    /// <returns>Response from the OpenAI model along with tokens for the prompt and response.</returns>
    public async Task<(string response, int promptTokens, int responseTokens)> GetChatCompletionAsync(string sessionId, string userPrompt)
    {
        try{
            ChatMessage systemMessage = new(ChatRole.System, _systemPrompt);
            ChatMessage userMessage = new(ChatRole.User, userPrompt);
            
            ChatCompletionsOptions options = new()
            {
                Messages =
                {
                    systemMessage,
                    userMessage
                },
                User = sessionId,
                MaxTokens = 3000,
                Temperature = 0.3f,
                NucleusSamplingFactor = 0.5f,
                FrequencyPenalty = 0,
                PresencePenalty = 0
            };

            Response<ChatCompletions> completionsResponse = await _client.GetChatCompletionsAsync(_modelName, options);

            ChatCompletions completions = completionsResponse.Value;

            return (
                response: completions.Choices[0].Message.Content,
                promptTokens: completions.Usage.PromptTokens,
                responseTokens: completions.Usage.CompletionTokens
            );
        }
        catch (Exception ex)
        {
            // Return an error response with all values set to 0 and the error message
            return (
                response: "Error: " + ex.Message,
                promptTokens: 0,
                responseTokens: 0
            );
        }
            
    }

    /// <summary>
    /// Sends the existing conversation to the OpenAI model and returns a two word summary.
    /// </summary>
    /// <param name="sessionId">Chat session identifier for the current conversation.</param>
    /// <param name="conversation">Prompt conversation to send to the deployment.</param>
    /// <returns>Summarization response from the OpenAI model deployment.</returns>
    public async Task<string> SummarizeAsync(string sessionId, string userPrompt)
    {
        
        ChatMessage systemMessage = new(ChatRole.System, _summarizePrompt);
        ChatMessage userMessage = new(ChatRole.User, userPrompt);
        
        ChatCompletionsOptions options = new()
        {
            Messages = { 
                systemMessage,
                userMessage
            },
            User = sessionId,
            MaxTokens = 20,
            Temperature = 0.0f,
            NucleusSamplingFactor = 0.0f,
            FrequencyPenalty = 0,
            PresencePenalty = 0
        };

        Response<ChatCompletions> completionsResponse = await _client.GetChatCompletionsAsync(_modelName, options);

        ChatCompletions completions = completionsResponse.Value;

        string summary =  completions.Choices[0].Message.Content;

        return summary;
    }
}
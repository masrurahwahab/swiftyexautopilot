using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

using SwiftyAutopilot.Services.Interfaces;

namespace SwiftyAutopilot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController(
    IAiService aiService,
    IRuleService ruleService) : ControllerBase
{
   
    [HttpPost("suggest")]
    public async Task<IActionResult> SuggestRules(
        [FromBody] AiSuggestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Goal))
            return BadRequest(new
            {
                message = "Goal cannot be empty."
            });

        try
        {
            var suggestions = await aiService
                .SuggestRulesAsync(request.Goal);

            if (!suggestions.Any())
                return Ok(new
                {
                    message = "No suggestions generated. " +
                              "Try rephrasing your goal.",
                    suggestions = suggestions
                });

            return Ok(new
            {
                message = $"Generated {suggestions.Count} " +
                          $"rule(s) for your goal.",
                suggestions
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "AI service error.",
                detail  = ex.Message
            });
        }
    }

  
    [HttpPost("activate")]
    public async Task<IActionResult> ActivateSuggestions(
        [FromBody] ActivateSuggestionsRequest request)
    {
        if (request.Suggestions is null
            || !request.Suggestions.Any())
            return BadRequest(new
            {
                message = "No suggestions provided."
            });

        var telegramId = GetTelegramId();
        var created    = new List<object>();

        foreach (var suggestion in request.Suggestions)
        {
            try
            {
                
                var ruleRequest = MapToRuleRequest(suggestion);

                var rule = await ruleService
                    .CreateRuleAsync(telegramId, ruleRequest);

                created.Add(new
                {
                    id          = rule.Id,
                    name        = rule.Name,
                    description = suggestion.Description
                });
            }
            catch (ArgumentException ex)
            {
               
                created.Add(new
                {
                    name    = suggestion.Name,
                    error   = ex.Message,
                    skipped = true
                });
            }
        }

        return Ok(new
        {
            message = $"{created.Count} rule(s) activated.",
            rules   = created
        });
    }

   
    private long GetTelegramId()
    {
        return (long)HttpContext.Items["TelegramId"]!;
    }

    private static CreateRuleRequest MapToRuleRequest(
        AiRuleSuggestion suggestion)
    {
       
        var triggerType = Enum.Parse<Models.Enums.TriggerType>(
            suggestion.TriggerType, ignoreCase: true);

        var actionType = Enum.Parse<Models.Enums.ActionType>(
            suggestion.ActionType, ignoreCase: true);

        return new CreateRuleRequest(
            Name:              suggestion.Name,
            TriggerType:       triggerType,
            ActionType:        actionType,
            TargetAsset:       suggestion.TargetAsset,
            TargetPrice:       suggestion.TargetPrice,
            PriceDropPercent:  suggestion.PriceDropPercent,
            BalanceThreshold:  suggestion.BalanceThreshold,
            CronExpression:    suggestion.CronExpression,
            ActionAsset:       suggestion.ActionAsset,
            ActionAmount:      suggestion.ActionAmount,
            ActionCurrency:    suggestion.ActionCurrency,
            BankAccount:       null,
            BillType:          suggestion.BillType);
    }
}

public record AiSuggestRequest(
    [property: JsonPropertyName("goal")]
    string Goal);

public record ActivateSuggestionsRequest(
    [property: JsonPropertyName("suggestions")]
    List<AiRuleSuggestion> Suggestions);
    
    

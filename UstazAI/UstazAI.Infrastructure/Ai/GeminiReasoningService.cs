using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UstazAI.Application.Ai;
using UstazAI.Application.Common.Interfaces;
using UstazAI.Domain.Entities;
using UstazAI.Domain.Enums;

namespace UstazAI.Infrastructure.Ai;

public sealed class GeminiReasoningService(
    HttpClient http, IOptions<GeminiOptions> optionsAccessor, IAppDbContext db, ICurrentUser currentUser, ILogger<GeminiReasoningService> logger)
    : IAiReasoningService
{
    private readonly GeminiOptions _options = optionsAccessor.Value;

    private Locale L(Locale profileLocale) => currentUser.UiLocale ?? profileLocale;

    private readonly SemaphoreSlim _dbLock = new(1, 1);

    public async Task<DiagnosticsAiOutput> GenerateDiagnosticsAsync(DiagnosticsAiInput input, CancellationToken ct)
    {
        var schema = new JsonObject
        {
            ["type"] = "OBJECT",
            ["properties"] = new JsonObject
            {
                ["strengths"] = new JsonObject { ["type"] = "ARRAY", ["items"] = new JsonObject { ["type"] = "STRING" } },
                ["constraintsFound"] = new JsonObject { ["type"] = "ARRAY", ["items"] = new JsonObject { ["type"] = "STRING" } },
                ["inferredGoal"] = new JsonObject { ["type"] = "STRING" },
                ["confidenceScore"] = new JsonObject { ["type"] = "NUMBER" }
            },
            ["required"] = new JsonArray("strengths", "constraintsFound", "inferredGoal", "confidenceScore")
        };

        var userContent = "Student profile (JSON): " + JsonSerializer.Serialize(new
        {
            input.FullName,
            input.Grade,
            Gpa = input.Gpa,
            input.Interests,
            input.TargetCountries,
            BudgetBand = input.BudgetBand.ToString(),
            input.Constraints
        });

        var json = await CallAsync(_options.FlashModel, PromptLibrary.DiagnosticsSystemInstruction(L(input.Locale)),
            userContent, schema, "Diagnostics", ct);

        return new DiagnosticsAiOutput(
            [.. json["strengths"]!.AsArray().Select(n => n!.GetValue<string>())],
            [.. json["constraintsFound"]!.AsArray().Select(n => n!.GetValue<string>())],
            json["inferredGoal"]!.GetValue<string>(),
            json["confidenceScore"]!.GetValue<double>(),
            false);
    }

    public async Task<RecommendationNarrationOutput> NarrateRecommendationAsync(RecommendationNarrationInput input, CancellationToken ct)
    {
        var schema = new JsonObject
        {
            ["type"] = "OBJECT",
            ["properties"] = new JsonObject
            {
                ["narrativeSummary"] = new JsonObject { ["type"] = "STRING" },
                ["academicExplanation"] = new JsonObject { ["type"] = "STRING" },
                ["financialExplanation"] = new JsonObject { ["type"] = "STRING" },
                ["careerExplanation"] = new JsonObject { ["type"] = "STRING" },
                ["timelineExplanation"] = new JsonObject { ["type"] = "STRING" }
            },
            ["required"] = new JsonArray("narrativeSummary", "academicExplanation", "financialExplanation", "careerExplanation", "timelineExplanation")
        };

        var userContent = "Program and precomputed scores (JSON, do not alter the numbers): " + JsonSerializer.Serialize(new
        {
            input.ProgramName,
            input.FieldOfStudy,
            input.University,
            input.Country,
            input.AcademicFitScore,
            input.FinancialFitScore,
            input.CareerFitScore,
            input.TimelineFitScore,
            input.AcademicNote,
            input.FinancialNote,
            input.CareerNote,
            input.TimelineNote,
            input.AdmissionProbabilityPoint
        });

        var json = await CallAsync(_options.FlashModel, PromptLibrary.RecommendationNarrationSystemInstruction(L(input.Locale)),
            userContent, schema, "RecommendationNarration", ct);

        return new RecommendationNarrationOutput(
            json["narrativeSummary"]!.GetValue<string>(),
            json["academicExplanation"]!.GetValue<string>(),
            json["financialExplanation"]!.GetValue<string>(),
            json["careerExplanation"]!.GetValue<string>(),
            json["timelineExplanation"]!.GetValue<string>(),
            false);
    }

    public async Task<EssayReviewAiOutput> ReviewEssayAsync(EssayReviewAiInput input, CancellationToken ct)
    {
        var schema = new JsonObject
        {
            ["type"] = "OBJECT",
            ["properties"] = new JsonObject
            {
                ["strengths"] = new JsonObject { ["type"] = "ARRAY", ["items"] = new JsonObject { ["type"] = "STRING" } },
                ["suggestedImprovements"] = new JsonObject { ["type"] = "ARRAY", ["items"] = new JsonObject { ["type"] = "STRING" } },
                ["clarityFeedback"] = new JsonObject { ["type"] = "STRING" },
                ["structureFeedback"] = new JsonObject { ["type"] = "STRING" }
            },
            ["required"] = new JsonArray("strengths", "suggestedImprovements", "clarityFeedback", "structureFeedback")
        };

        var userContent = "Essay prompt: " + (input.Prompt ?? "(not specified)") + "\n\nEssay text:\n" + input.EssayText;

        var json = await CallAsync(_options.FlashModel, PromptLibrary.EssayReviewSystemInstruction(L(input.Locale)),
            userContent, schema, "EssayReview", ct);

        return new EssayReviewAiOutput(
            [.. json["strengths"]!.AsArray().Select(n => n!.GetValue<string>())],
            [.. json["suggestedImprovements"]!.AsArray().Select(n => n!.GetValue<string>())],
            json["clarityFeedback"]!.GetValue<string>(),
            json["structureFeedback"]!.GetValue<string>(),
            false);
    }

    public async Task<ChatAiOutput> ChatAsync(ChatAiInput input, CancellationToken ct)
    {
        var schema = new JsonObject
        {
            ["type"] = "OBJECT",
            ["properties"] = new JsonObject { ["reply"] = new JsonObject { ["type"] = "STRING" } },
            ["required"] = new JsonArray("reply")
        };

        var userContent = "Student's own eligibility/diagnostics context (JSON, read-only — cite only what's here): "
            + input.ScopedContextJson
            + "\n\nConversation so far:\n"
            + string.Join("\n", input.History.Select(h => $"{h.Role}: {h.Content}"))
            + $"\n\nNew student message:\n{input.UserMessage}";

        var json = await CallAsync(_options.FlashModel, PromptLibrary.ChatSystemInstruction(L(input.Locale)),
            userContent, schema, "Chat", ct);

        return new ChatAiOutput(json["reply"]!.GetValue<string>(), false);
    }

    public async Task<StudyGuideAiOutput> GenerateStudyGuideAsync(StudyGuideAiInput input, CancellationToken ct)
    {
        var schema = new JsonObject
        {
            ["type"] = "OBJECT",
            ["properties"] = new JsonObject
            {
                ["steps"] = new JsonObject
                {
                    ["type"] = "ARRAY",
                    ["items"] = new JsonObject
                    {
                        ["type"] = "OBJECT",
                        ["properties"] = new JsonObject
                        {
                            ["title"] = new JsonObject { ["type"] = "STRING" },
                            ["description"] = new JsonObject { ["type"] = "STRING" },
                            ["estimatedMinutes"] = new JsonObject { ["type"] = "INTEGER" },
                            ["videoSearchQuery"] = new JsonObject { ["type"] = "STRING" }
                        },
                        ["required"] = new JsonArray("title", "description", "estimatedMinutes", "videoSearchQuery")
                    }
                }
            },
            ["required"] = new JsonArray("steps")
        };

        var userContent = "Roadmap task to build a how-to guide for (JSON): " + JsonSerializer.Serialize(new
        {
            Topic = input.Subject,
            input.TaskCategory,
            input.TaskDescription,
            input.CurrentScore,
            input.MaxScore,
            input.GapHeadroomPoints
        });

        var json = await CallAsync(_options.FlashModel, PromptLibrary.StudyGuideSystemInstruction(L(input.Locale)),
            userContent, schema, "StudyGuide", ct);

        var steps = json["steps"]!.AsArray().Select(s => new StudyGuideStepAiOutput(
            s!["title"]!.GetValue<string>(),
            s["description"]!.GetValue<string>(),
            s["estimatedMinutes"]!.GetValue<int>(),
            s["videoSearchQuery"]!.GetValue<string>())).ToList();

        return new StudyGuideAiOutput(steps, false);
    }

    public async Task<PersonaClassificationOutput> ClassifyPersonaAsync(string freeText, Locale locale, CancellationToken ct)
    {
        var schema = new JsonObject
        {
            ["type"] = "OBJECT",
            ["properties"] = new JsonObject
            {
                ["tone"] = new JsonObject { ["type"] = "STRING", ["enum"] = new JsonArray("Neutral", "Reassuring", "Direct", "Energetic") }
            },
            ["required"] = new JsonArray("tone")
        };

        var json = await CallAsync(_options.FlashModel, PromptLibrary.PersonaClassificationSystemInstruction(L(locale)),
            $"Student free text: {freeText}", schema, "PersonaClassification", ct);

        var tone = Enum.TryParse<PersonaTone>(json["tone"]!.GetValue<string>(), out var parsed) ? parsed : PersonaTone.Neutral;
        return new PersonaClassificationOutput(tone, false);
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> ExhaustedUntil = new();

    private async Task<JsonNode> CallAsync(
        string model, string systemInstruction, string userContent, JsonNode schema, string module, CancellationToken ct)
    {
        var candidates = new[] { model }.Concat(_options.FallbackModels).Distinct().ToList();
        Exception? last = null;
        foreach (var candidate in candidates)
        {
            if (ExhaustedUntil.TryGetValue(candidate, out var until) && until > DateTime.UtcNow && candidate != candidates[^1]) continue;
            try
            {
                return await CallOnceAsync(candidate, systemInstruction, userContent, schema.DeepClone(), module, ct);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.TooManyRequests or System.Net.HttpStatusCode.ServiceUnavailable or System.Net.HttpStatusCode.GatewayTimeout)
            {
                ExhaustedUntil[candidate] = DateTime.UtcNow.AddMinutes(ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests ? 10 : 1);
                logger.LogWarning("Model {Model} unavailable ({Status}); trying next model for {Module}", candidate, (int?)ex.StatusCode, module);
                last = ex;
            }
        }
        throw last ?? new InvalidOperationException("No Gemini model available.");
    }

    private async Task<JsonNode> CallOnceAsync(
        string model, string systemInstruction, string userContent, JsonNode schema, string module, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var inputTokens = 0;
        var outputTokens = 0;
        var success = false;
        string? errorMessage = null;

        try
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new InvalidOperationException("Gemini API key is not configured (set Gemini:ApiKey via user-secrets or environment variable).");

            var request = new JsonObject
            {
                ["systemInstruction"] = new JsonObject { ["parts"] = new JsonArray(new JsonObject { ["text"] = systemInstruction }) },
                ["contents"] = new JsonArray(new JsonObject { ["role"] = "user", ["parts"] = new JsonArray(new JsonObject { ["text"] = userContent }) }),
                ["generationConfig"] = new JsonObject { ["responseMimeType"] = "application/json", ["responseSchema"] = schema }
            };

            var url = $"{_options.BaseUrl}/models/{model}:generateContent";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("x-goog-api-key", _options.ApiKey);
            using var httpResponse = await http.SendAsync(httpRequest, ct);
            httpResponse.EnsureSuccessStatusCode();

            var responseText = await httpResponse.Content.ReadAsStringAsync(ct);
            var responseJson = JsonNode.Parse(responseText)
                ?? throw new InvalidOperationException("Gemini returned an empty response.");

            var candidateText = responseJson["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>()
                ?? throw new InvalidOperationException("Gemini response had no candidate text.");

            inputTokens = responseJson["usageMetadata"]?["promptTokenCount"]?.GetValue<int>() ?? 0;
            outputTokens = responseJson["usageMetadata"]?["candidatesTokenCount"]?.GetValue<int>() ?? 0;

            var parsed = JsonNode.Parse(candidateText) ?? throw new InvalidOperationException("Gemini structured output was not valid JSON.");
            success = true;
            return parsed;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            logger.LogWarning(ex, "Gemini call failed for module {Module} using model {Model}", module, model);
            throw;
        }
        finally
        {
            sw.Stop();
            await _dbLock.WaitAsync(CancellationToken.None);
            try
            {
                db.AiUsageLogs.Add(new AiUsageLog
                {
                    Module = module,
                    Model = model,
                    InputTokens = inputTokens,
                    OutputTokens = outputTokens,
                    LatencyMs = sw.ElapsedMilliseconds,
                    Success = success,
                    FallbackUsed = !success,
                    ErrorMessage = errorMessage
                });
                await db.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception logEx) { logger.LogWarning(logEx, "Failed to persist AiUsageLog entry"); }
            finally { _dbLock.Release(); }
        }
    }

    public async Task<ProgramResearchOutput> ResearchProgramAsync(ProgramResearchInput input, CancellationToken ct)
    {
        var (text, sources) = await GroundedTextAsync(
            "Convert currencies to USD per academic year for an international student. Program: " +
            $"{input.ProgramName} ({input.DegreeLevel}), {input.UniversityName}, {input.Country}.",
            ["TUITION_USD_PER_YEAR: <number or unknown>", "LIVING_USD_PER_YEAR: <number or unknown>",
             "DEADLINE: <YYYY-MM-DD or unknown>", "SCHOLARSHIP_AVAILABLE: <yes|no|unknown>"],
            "ProgramWebResearch", ct);

        return new ProgramResearchOutput(
            Number(Field(text, "TUITION_USD_PER_YEAR")), Number(Field(text, "LIVING_USD_PER_YEAR")),
            DateOnly.TryParse(Field(text, "DEADLINE"), out var dl) ? dl : null,
            Field(text, "SCHOLARSHIP_AVAILABLE")?.Trim().ToLowerInvariant() switch { "yes" => true, "no" => false, _ => null },
            null, sources);
    }

    public async Task<ThresholdResearchOutput> ResearchThresholdsAsync(ThresholdResearchInput input, CancellationToken ct)
    {
        var (text, sources) = await GroundedTextAsync(
            $"Kazakhstan university admission scoring (ENT / UNT) for the admission track '{input.TrackLabel}'. " +
            $"Program: {input.ProgramName}, {input.UniversityName}. STATE_THRESHOLD is the national minimum score to compete for a state " +
            "grant on this track; UNIVERSITY_THRESHOLD is this program's own minimum; GRANT_CUTOFF_* are the passing scores that actually " +
            "won grants in recent years (min/max/median across the years you found).",
            ["STATE_THRESHOLD: <number or unknown>", "UNIVERSITY_THRESHOLD: <number or unknown>", "GRANT_CUTOFF_MIN: <number or unknown>",
             "GRANT_CUTOFF_MAX: <number or unknown>", "GRANT_CUTOFF_MEDIAN: <number or unknown>", "CUTOFF_YEARS_COUNT: <integer or unknown>"],
            "ThresholdWebResearch", ct);

        return new ThresholdResearchOutput(
            Number(Field(text, "STATE_THRESHOLD")), Number(Field(text, "UNIVERSITY_THRESHOLD")), Number(Field(text, "GRANT_CUTOFF_MIN")),
            Number(Field(text, "GRANT_CUTOFF_MAX")), Number(Field(text, "GRANT_CUTOFF_MEDIAN")),
            (int?)Number(Field(text, "CUTOFF_YEARS_COUNT")), sources);
    }

    private static string? Field(string text, string key)
    {
        var m = System.Text.RegularExpressions.Regex.Match(text, "^" + key + @":\s*(.+?)\s*$", System.Text.RegularExpressions.RegexOptions.Multiline);
        return m.Success && !m.Groups[1].Value.StartsWith("unknown", StringComparison.OrdinalIgnoreCase) ? m.Groups[1].Value : null;
    }

    private static decimal? Number(string? raw)
    {
        if (raw is null) return null;
        var nums = System.Text.RegularExpressions.Regex.Matches(raw.Replace(",", ""), @"\d+(\.\d+)?")
            .Select(x => decimal.Parse(x.Value, System.Globalization.CultureInfo.InvariantCulture)).ToList();
        return nums.Count == 0 ? null : nums.Average();
    }

    private async Task<(string Text, List<ProgramResearchSource> Sources)> GroundedTextAsync(
        string context, string[] lines, string module, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var success = false;
        var inputTokens = 0;
        var outputTokens = 0;
        string? errorMessage = null;
        var usedModel = _options.ResearchModel;

        try
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new InvalidOperationException("Gemini API key is not configured.");

            var prompt =
                "You MUST run Google searches (official university / ministry pages first). Reply in plain text: exactly these lines, " +
                "then one sentence of notes saying where each figure came from.\n" + string.Join("\n", lines) + "\n" +
                "Never guess — write unknown if not confirmed on a page you found. " + context;

            var request = new JsonObject
            {
                ["contents"] = new JsonArray(new JsonObject { ["role"] = "user", ["parts"] = new JsonArray(new JsonObject { ["text"] = prompt }) }),
                ["tools"] = new JsonArray(new JsonObject { ["google_search"] = new JsonObject() })
            };

            HttpResponseMessage? httpResponse = null;
            var keys = new[] { _options.ApiKey }.Concat(_options.ExtraApiKeys).Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().ToList();
            foreach (var (key, m) in keys.SelectMany(k => new[] { _options.ResearchModel }.Concat(_options.FallbackModels).Append(_options.FlashModel).Distinct().Select(m => (k, m))))
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/models/{m}:generateContent")
                {
                    Content = new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json")
                };
                httpRequest.Headers.Add("x-goog-api-key", key);
                httpResponse?.Dispose();
                httpResponse = await http.SendAsync(httpRequest, ct);
                usedModel = m;
                if (httpResponse.StatusCode != System.Net.HttpStatusCode.TooManyRequests) break;
            }
            using var _ = httpResponse;
            httpResponse!.EnsureSuccessStatusCode();

            var root = JsonNode.Parse(await httpResponse.Content.ReadAsStringAsync(ct))
                ?? throw new InvalidOperationException("Gemini returned an empty response.");
            inputTokens = root["usageMetadata"]?["promptTokenCount"]?.GetValue<int>() ?? 0;
            outputTokens = root["usageMetadata"]?["candidatesTokenCount"]?.GetValue<int>() ?? 0;

            var candidate = root["candidates"]?[0] ?? throw new InvalidOperationException("No candidate in response.");
            var text = candidate["content"]?["parts"]?[0]?["text"]?.GetValue<string>() ?? throw new InvalidOperationException("No text in response.");

            var sources = (candidate["groundingMetadata"]?["groundingChunks"]?.AsArray() ?? [])
                .Select(c => c?["web"])
                .Where(w => w?["uri"] is not null)
                .Select(w => new ProgramResearchSource(w!["title"]?.GetValue<string>() ?? "", w["uri"]!.GetValue<string>()))
                .DistinctBy(x => x.Url).Take(6).ToList();

            success = true;
            return (text, sources);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            logger.LogWarning(ex, "Gemini grounded research failed ({Module})", module);
            throw;
        }
        finally
        {
            sw.Stop();
            await _dbLock.WaitAsync(CancellationToken.None);
            try
            {
                db.AiUsageLogs.Add(new AiUsageLog
                {
                    Module = module, Model = usedModel, InputTokens = inputTokens, OutputTokens = outputTokens,
                    LatencyMs = sw.ElapsedMilliseconds, Success = success, FallbackUsed = !success, ErrorMessage = errorMessage
                });
                await db.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception logEx) { logger.LogWarning(logEx, "Failed to persist AiUsageLog entry"); }
            finally { _dbLock.Release(); }
        }
    }
}

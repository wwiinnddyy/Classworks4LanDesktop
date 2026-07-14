using ClassworksPlugin.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

namespace ClassworksPlugin.Services;

/// <summary>
/// Classworks 使用厚浪云 KV 服务同步每日作业，键名为 classworks-data-YYYYMMDD。
/// </summary>
public sealed class ClassworksService : IDisposable
{
    public const string DefaultKvBaseUrl = "https://kv-service.houlang.cloud";
    public const string LegacyKvBaseUrl = "https://kv-service.wuyuan.dev";
    public const string DefaultAppId = "d158067f53627d2b98babe8bffd2fd7d";

    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    public string Token { get; set; } = string.Empty;

    public async Task<string?> AuthenticateAsync(
        string namespaceId,
        string password,
        string appId,
        string? kvBaseUrl = null,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = NormalizeBaseUrl(kvBaseUrl);
        var url = $"{baseUrl}/apps/auth/token";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonConvert.SerializeObject(new
            {
                @namespace = namespaceId,
                password,
                appId
            }), System.Text.Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var json = JObject.Parse(body);
        Token = json["token"]?.ToString()
                ?? json["data"]?["token"]?.ToString()
                ?? string.Empty;
        return string.IsNullOrWhiteSpace(Token) ? null : Token;
    }

    public async Task<IReadOnlyList<Assignment>> GetAssignmentsAsync(
        DateTime date,
        string? kvBaseUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(Token))
        {
            return Array.Empty<Assignment>();
        }

        var baseUrl = NormalizeBaseUrl(kvBaseUrl);
        var key = $"classworks-data-{date:yyyyMMdd}";
        var url = $"{baseUrl}/kv/{Uri.EscapeDataString(key)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddAppTokenHeader(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return Array.Empty<Assignment>();
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseHomework(body, date);
    }

    public async Task SaveAssignmentsAsync(
        DateTime date,
        IReadOnlyCollection<Assignment> allAssignments,
        string? kvBaseUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(Token))
        {
            throw new InvalidOperationException("Token is not set.");
        }

        var baseUrl = NormalizeBaseUrl(kvBaseUrl);
        var key = $"classworks-data-{date:yyyyMMdd}";
        var url = $"{baseUrl}/kv/{Uri.EscapeDataString(key)}";

        JObject? currentJson = null;
        using var getRequest = new HttpRequestMessage(HttpMethod.Get, url);
        AddAppTokenHeader(getRequest);
        using (var getResp = await _httpClient.SendAsync(getRequest, cancellationToken))
        {
            if (getResp.IsSuccessStatusCode)
            {
                var body = await getResp.Content.ReadAsStringAsync(cancellationToken);
                currentJson = ParseBoardDocument(body);
            }
            else if (getResp.StatusCode != HttpStatusCode.NotFound)
            {
                getResp.EnsureSuccessStatusCode();
            }
        }

        var finalJson = MergeAssignmentsIntoBoard(currentJson, allAssignments);

        using var postRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(finalJson.ToString(Formatting.None), System.Text.Encoding.UTF8, "application/json")
        };
        AddAppTokenHeader(postRequest);
        using var postResp = await _httpClient.SendAsync(postRequest, cancellationToken);
        postResp.EnsureSuccessStatusCode();
    }

    internal static IReadOnlyList<Assignment> ParseHomework(string jsonBody, DateTime date)
    {
        if (string.IsNullOrWhiteSpace(jsonBody))
        {
            return Array.Empty<Assignment>();
        }

        var root = ParseBoardDocument(jsonBody);
        if (root["homework"] is not JObject homework)
        {
            return Array.Empty<Assignment>();
        }

        var assignments = new List<Assignment>();
        foreach (var property in homework.Properties())
        {
            var content = property.Value?["content"]?.ToString() ?? string.Empty;
            assignments.Add(new Assignment
            {
                Title = property.Name,
                Description = content,
                DueDate = date
            });
        }

        return assignments;
    }

    internal static JObject MergeAssignmentsIntoBoard(
        JObject? currentJson,
        IReadOnlyCollection<Assignment> allAssignments)
    {
        ArgumentNullException.ThrowIfNull(allAssignments);

        var finalJson = currentJson is null
            ? new JObject()
            : (JObject)currentJson.DeepClone();
        JObject homework;
        if (finalJson["homework"] is JObject existingHomework)
        {
            homework = existingHomework;
        }
        else
        {
            homework = new JObject();
            finalJson["homework"] = homework;
        }

        foreach (var item in allAssignments)
        {
            if (string.IsNullOrWhiteSpace(item.Title))
            {
                continue;
            }

            var card = homework[item.Title] as JObject;
            if (card is null)
            {
                card = new JObject();
                homework[item.Title] = card;
            }

            card["content"] = item.Description ?? string.Empty;
        }

        return finalJson;
    }

    private static string NormalizeBaseUrl(string? kvBaseUrl)
    {
        var value = string.IsNullOrWhiteSpace(kvBaseUrl) ? DefaultKvBaseUrl : kvBaseUrl.Trim();
        return value.TrimEnd('/');
    }

    private void AddAppTokenHeader(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation("x-app-token", Token);
    }

    private static JObject ParseBoardDocument(string jsonBody)
    {
        var root = JObject.Parse(jsonBody);
        if (root["value"] is JObject objectValue)
        {
            return objectValue;
        }

        if (root["value"]?.Type == JTokenType.String)
        {
            var value = root["value"]?.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return JObject.Parse(value);
            }
        }

        return root;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

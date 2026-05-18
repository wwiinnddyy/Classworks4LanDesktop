using System.Net.Http.Headers;
using ClassworksPlugin.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClassworksPlugin.Services;

/// <summary>
/// Classworks 使用厚浪云 KV 服务同步每日作业，键名为 classworks-data-YYYYMMDD。
/// </summary>
public sealed class ClassworksService
{
    public const string DefaultKvBaseUrl = "https://kv-service.wuyuan.dev";

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
        var request = new HttpRequestMessage(HttpMethod.Post, url)
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
        return json["token"]?.ToString();
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
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return Array.Empty<Assignment>();
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseHomework(body, date);
    }

    public async Task AddAssignmentAsync(
        Assignment assignment,
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
        try
        {
            var getRequest = new HttpRequestMessage(HttpMethod.Get, url);
            getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            using var getResp = await _httpClient.SendAsync(getRequest, cancellationToken);
            if (getResp.IsSuccessStatusCode)
            {
                var body = await getResp.Content.ReadAsStringAsync(cancellationToken);
                currentJson = JObject.Parse(body);
            }
        }
        catch
        {
        }

        var homework = new JObject();
        foreach (var item in allAssignments)
        {
            if (string.IsNullOrWhiteSpace(item.Title))
            {
                continue;
            }

            homework[item.Title] = new JObject
            {
                ["content"] = item.Description ?? string.Empty
            };
        }

        var finalJson = new JObject
        {
            ["homework"] = homework
        };

        if (currentJson?["attendance"] is JToken attendance)
        {
            finalJson["attendance"] = attendance.DeepClone();
        }

        var postRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(finalJson.ToString(Formatting.None), System.Text.Encoding.UTF8, "application/json")
        };
        postRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        using var postResp = await _httpClient.SendAsync(postRequest, cancellationToken);
        postResp.EnsureSuccessStatusCode();
    }

    internal static IReadOnlyList<Assignment> ParseHomework(string jsonBody, DateTime date)
    {
        if (string.IsNullOrWhiteSpace(jsonBody))
        {
            return Array.Empty<Assignment>();
        }

        var root = JObject.Parse(jsonBody);
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

    private static string NormalizeBaseUrl(string? kvBaseUrl)
    {
        var value = string.IsNullOrWhiteSpace(kvBaseUrl) ? DefaultKvBaseUrl : kvBaseUrl.Trim();
        return value.TrimEnd('/');
    }
}

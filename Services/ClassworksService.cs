using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ClassworksPlugin
{
    /// <summary>
    /// Handles communication with the Classworks KV API.  Classworks 提供了
    /// 永久免费的 KV 数据存储服务，可用于同步作业、学生信息、考试看板等【250439475987970†L42-L88】【706689548766089†L85-L116】。
    /// 开发者需要在 ZeroCat 社区创建应用获取 appId，并为每个设备设置命名空间和访问密码。
    /// 通过 `POST /apps/auth/token` 获得访问令牌【250439475987970†L53-L80】后，可在请求头带上 `Authorization: Bearer <token>`
    /// 调用 `GET /kv/<key>` 等接口读写键值数据【250439475987970†L88-L125】。
    /// </summary>
    public sealed class ClassworksService
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassworksService"/> class.
        /// </summary>
        public ClassworksService()
        {
            _httpClient = new HttpClient();
            // Set a reasonable timeout for network operations.
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// 请求访问令牌。根据 Classworks KV 文档，客户端需要提供命名空间、密码和 appId
        /// 来交换访问令牌【250439475987970†L53-L80】。成功后返回的 token 可用于之后的
        /// KV 请求。请将 namespace 和 password 存储在用户的插件设置中。
        /// </summary>
        /// <param name="namespaceId">设备的唯一命名空间。</param>
        /// <param name="password">设备管理员设置的访问密码。</param>
        /// <param name="appId">在 ZeroCat 社区创建的应用 ID。</param>
        /// <returns>访问令牌；如果失败则返回 <c>null</c>。</returns>
        public async Task<string?> AuthenticateAsync(string namespaceId, string password, string appId)
        {
            var url = "https://kv-service.wuyuan.dev/apps/auth/token";
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new
                {
                    namespace = namespaceId,
                    password,
                    appId
                }), System.Text.Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            var body = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<dynamic>(body);
            return (string?)json?.token;
        }

        /// <summary>
        /// <summary>
        /// 根据指定日期获取当天的作业列表。Classworks 将每天的作业存储在键
        /// `classworks-data-YYYYMMDD` 中【706689548766089†L85-L116】。键值的
        /// `homework` 字段包含科目名称与作业内容。此方法会解析这些信息并
        /// 转换为 Assignment 对象列表。调用前请先设置 Token 属性。
        /// </summary>
        /// <param name="date">需要查询的日期。</param>
        /// <returns>解析后的作业列表。</returns>
        public async Task<List<Assignment>> GetAssignmentsAsync(DateTime date)
        {
            if (string.IsNullOrEmpty(Token))
                return new List<Assignment>();

            string key = $"classworks-data-{date:yyyyMMdd}";
            var url = $"https://kv-service.wuyuan.dev/kv/{Uri.EscapeDataString(key)}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new List<Assignment>();
            }
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<dynamic>(body);
            if (json == null || json.homework == null)
                return new List<Assignment>();

            var assignments = new List<Assignment>();
            foreach (var subject in json.homework)
            {
                string subjectName = subject.Name;
                string content = subject.First.content != null ? (string)subject.First.content : string.Empty;
                assignments.Add(new Assignment
                {
                    Title = subjectName,
                    Description = content,
                    DueDate = date
                });
            }
            return assignments;
        }

        /// <summary>
        /// Adds or updates an assignment for the specified date and synchronises
        /// the entire assignments collection to Classworks KV.  This method
        /// reads the existing JSON for <c>classworks-data-YYYYMMDD</c>, updates
        /// the <c>homework</c> section using the provided list of assignments,
        /// and writes the new JSON back to the service via <c>POST /kv/{key}</c>【250439475987970†L88-L125】.  The
        /// attendance section (if present) is preserved.  Requires that
        /// <see cref="Token"/> has been set.
        /// </summary>
        /// <param name="assignment">The assignment that was added.</param>
        /// <param name="date">The date for which the assignment applies.</param>
        /// <param name="allAssignments">The complete list of assignments for that date.</param>
        public async Task AddAssignmentAsync(Assignment assignment, DateTime date, IReadOnlyCollection<Assignment> allAssignments)
        {
            if (string.IsNullOrEmpty(Token))
                throw new InvalidOperationException("Token is not set");
            string key = $"classworks-data-{date:yyyyMMdd}";
            var url = $"https://kv-service.wuyuan.dev/kv/{Uri.EscapeDataString(key)}";

            // Attempt to fetch existing data so we can preserve attendance
            dynamic? currentJson = null;
            try
            {
                var getRequest = new HttpRequestMessage(HttpMethod.Get, url);
                getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                using var getResp = await _httpClient.SendAsync(getRequest);
                if (getResp.IsSuccessStatusCode)
                {
                    var body = await getResp.Content.ReadAsStringAsync();
                    currentJson = JsonConvert.DeserializeObject<dynamic>(body);
                }
            }
            catch
            {
                // ignore fetch error; we'll build new object
            }

            // Build new homework dictionary from all assignments
            var homeworkDict = new Dictionary<string, object>();
            foreach (var a in allAssignments)
            {
                var subj = a.Title;
                if (string.IsNullOrEmpty(subj))
                    continue;
                homeworkDict[subj] = new { content = a.Description ?? string.Empty };
            }
            // Compose final json
            dynamic finalJson = new System.Dynamic.ExpandoObject();
            finalJson.homework = homeworkDict;
            if (currentJson != null && currentJson.attendance != null)
            {
                finalJson.attendance = currentJson.attendance;
            }
            var payload = JsonConvert.SerializeObject(finalJson);

            var postRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
            };
            postRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            using var postResp = await _httpClient.SendAsync(postRequest);
            postResp.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// 持有当前会话的访问令牌。设置此属性后，所有 KV 请求都会在
        /// Authorization 头中携带该 token【250439475987970†L88-L100】。
        /// </summary>
        public string Token { get; set; } = string.Empty;
    }
}
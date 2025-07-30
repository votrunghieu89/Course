using E_learning.Model.cloudeDB;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace E_learning.Services.Cloude
{
    public class GoogleService
    {
        private readonly HttpClient _httpClient;
        private readonly GoogleModel _googleConfig;

        public GoogleService(IHttpClientFactory httpClientFactory, IOptions<GoogleModel> googleOptions)
        {
            _httpClient = httpClientFactory.CreateClient();
            _googleConfig = googleOptions.Value;
        }

        public async Task<string> GetAccessTokenAsync(string code)
        {
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("client_id", _googleConfig.ClientId),
                new KeyValuePair<string, string>("client_secret", _googleConfig.ClientSecret),
                new KeyValuePair<string, string>("redirect_uri", _googleConfig.RedirectUri),
                new KeyValuePair<string, string>("grant_type", "authorization_code")
            });

            var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", requestContent);
            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);
            return json["access_token"]?.ToString();
        }

        public async Task<JObject> GetGoogleUserInfoAsync(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return JObject.Parse(content);
        }
    }
}

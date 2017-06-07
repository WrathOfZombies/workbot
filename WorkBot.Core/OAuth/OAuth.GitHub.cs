using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace WorkBot.Core.OAuth
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Microsoft.Bot.Connector;
    using System.Net;
    using System.Net.Http.Headers;

    public class GitHubAccessToken
    {
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }

        [JsonProperty(PropertyName = "token_type")]
        public string TokenType { get; set; }

        [JsonProperty(PropertyName = "expires_in")]
        public long ExpiresIn { get; set; }
    }

    public class GitHubProfile
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "login")]
        public string Handle { get; set; }

        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; }

        [JsonProperty(PropertyName = "bio")]
        public string Bio { get; set; }

        [JsonProperty(PropertyName = "avatar")]
        public string Avatar { get; set; }
    }

    /// <summary>
    /// Helpers implementing Facebook API calls.
    /// </summary>
    public static class GitHubHelpers
    {
        // The Facebook App Id
        public static readonly string GitHubAppId = "297b5d9c743eb89cba7f";

        // The Facebook App Secret
        public static readonly string GitHubAppSecret = "67db20e5a1da4ac7d877250cb37f892df81453fa";

        public async static Task<GitHubAccessToken> ExchangeCodeForAccessToken(ConversationReference conversationReference, string code, string githubOAuthCallback)
        {
            var redirectUri = GetOAuthCallBack(conversationReference, githubOAuthCallback);
            var uri = GetUri("https://github.com/login/oauth/access_token",
                Tuple.Create("client_id", GitHubAppId),
                Tuple.Create("redirect_uri", redirectUri),
                Tuple.Create("client_secret", GitHubAppSecret),
                Tuple.Create("code", code)
                );

            return await GitHubRequest<GitHubAccessToken>(uri);
        }

        public static async Task<GitHubProfile> GetGitHubProfile(GitHubAccessToken token)
        {
            var uri = GetUri("https://api.github.com/user");

            var res = await GitHubRequest<GitHubProfile>(uri, token);
            return res;
        }

        private static string GetOAuthCallBack(ConversationReference conversationReference, string facebookOauthCallback)
        {
            var uri = GetUri(facebookOauthCallback,
                Tuple.Create("userId", TokenEncoder(conversationReference.User.Id)),
                Tuple.Create("botId", TokenEncoder(conversationReference.Bot.Id)),
                Tuple.Create("conversationId", TokenEncoder(conversationReference.Conversation.Id)),
                Tuple.Create("serviceUrl", TokenEncoder(conversationReference.ServiceUrl)),
                Tuple.Create("channelId", conversationReference.ChannelId)
                );
            return uri.ToString();
        }

        // because of a limitation on the characters in Facebook redirect_uri, we don't use the serialization of the cookie.
        // http://stackoverflow.com/questions/4386691/facebook-error-error-validating-verification-code
        public static string TokenEncoder(string token)
        {
            return WebUtility.UrlEncode(token);
        }

        public static string TokenDecoder(string token)
        {
            return WebUtility.UrlDecode(token);
        }

        public static string GetGitHubLoginUrl(ConversationReference conversationReference, string facebookOauthCallback)
        {
            var redirectUri = GetOAuthCallBack(conversationReference, facebookOauthCallback);
            var uri = GetUri("https://github.com/login/oauth/authorize",
                Tuple.Create("client_id", GitHubAppId),
                Tuple.Create("redirect_uri", redirectUri),
                Tuple.Create("response_type", "code"),
                Tuple.Create("scope", "gist"),
                Tuple.Create("state", Convert.ToString(new Random().Next(9999)))
                );

            return uri.ToString();
        }

        private static async Task<T> GitHubRequest<T>(Uri uri, GitHubAccessToken token = null)
        {
            string json;
            using (HttpClient client = new HttpClient())
            {
                if (token != null)
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
                }
                json = await client.GetStringAsync(uri).ConfigureAwait(false);
            }

            try
            {
                var result = JsonConvert.DeserializeObject<T>(json);
                return result;
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Unable to deserialize the Facebook response.", ex);
            }
        }

        private static Uri GetUri(string endPoint, params Tuple<string, string>[] queryParams)
        {
            var queryString = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(string.Empty);
            queryParams.ToList().ForEach(queryParam => { queryString.Add(queryParam.Item1, queryParam.Item2); });
            var builder = new UriBuilder(endPoint)
            {
                Query = queryString.ToString()
            };
            return builder.Uri;
        }
    }

}

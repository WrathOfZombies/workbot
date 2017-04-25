using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace WorkBot.Controllers
{
    [Route("api/[controller]")]
    public class EchoController : Controller
    {
        /// <summary>
        /// memoryCache
        /// </summary>
        private IMemoryCache _memoryCache;

        /// <summary>
        /// Bot Credentials
        /// </summary>
        private BotCredentials _botCredentials;

        public EchoController(IMemoryCache memoryCache, IOptions<BotCredentials> botCredentials)
        {
            this._memoryCache = memoryCache;
            this._botCredentials = botCredentials.Value;
        }

        /// <summary>
        /// GET api/echo
        /// This method is to ensure that the bot is running.
        /// </summary>
        [HttpGet]
        public string Get()
        {
            return $"Echo bot is ready with {this._botCredentials.ClientId}";
        }

        /// <summary>
        /// POST api/echo
        /// This method will be called every time the bot receives an activity. This is the messaging endpoint
        /// </summary>
        /// <param name="activity">The activity sent to the bot. I'm using dynamic here to simplify the code for the post</param>
        /// <returns>201 Created</returns>
        [HttpPost]
        public virtual async Task<IActionResult> Post([FromBody] dynamic activity)
        {
            // Get the conversation id so the bot answers.
            var conversationId = activity.from.id.ToString();

            // Get a valid token
            string token = await this._getBotApiToken();

            // send the message back
            using (var client = new HttpClient())
            {
                // I'm using dynamic here to make the code simpler
                dynamic message = new ExpandoObject();
                message.type = "message/text";
                message.text = activity.text;

                // Set the token in the authorization header.
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Post the message
                await client.PostAsJsonAsync<ExpandoObject>(
                    $"https://api.skype.net/v3/conversations/{conversationId}/activities",
                    message as ExpandoObject);
            }

            return Created(Url.Content("~/"), string.Empty);
        }

        /// <summary>
        /// Gets and caches a valid token so the bot can send messages.
        /// </summary>
        /// <returns>The token</returns>
        private async Task<string> _getBotApiToken()
        {
            // Check to see if we already have a valid token
            string token = this._memoryCache.Get("token")?.ToString();
            if (string.IsNullOrEmpty(token))
            {
                // we need to get a token.
                using (var client = new HttpClient())
                {
                    // Create the encoded content needed to get a token
                    var parameters = new Dictionary<string, string>
                    {
                        { "client_id", this._botCredentials.ClientId },
                        { "client_secret", this._botCredentials.ClientSecret },
                        { "scope", "https://graph.microsoft.com/.default" },
                        { "grant_type", "client_credentials" }
                    };
                    var content = new FormUrlEncodedContent(parameters);

                    // Post
                    var response = await client.PostAsync("https://login.microsoftonline.com/common/oauth2/v2.0/token", content);

                    // Get the token response
                    dynamic tokenResponse = await response.Content.ReadAsStringAsync();
                    token = tokenResponse["access_token"];

                    // Cache the token for 15 minutes.
                    this._memoryCache.Set(
                        "token",
                        token,
                        new DateTimeOffset(DateTime.Now.AddMinutes(15)));
                }
            }

            return token;
        }
    }
}

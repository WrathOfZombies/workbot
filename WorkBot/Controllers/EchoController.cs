using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Bot.Connector;
using Microsoft.AspNetCore.Authorization;

namespace WorkBot.Controllers
{
    [Route("api/[controller]")]
    public class EchoController : Controller
    {
        private readonly IMemoryCache _memoryCache;
        private readonly MicrosoftAppCredentials _microsoftAppCredentials;

        public EchoController(IMemoryCache memoryCache, IOptions<BotCredentials> botCredentials)
        {
            this._memoryCache = memoryCache;
            this._microsoftAppCredentials = new MicrosoftAppCredentials(botCredentials.Value.ClientId, botCredentials.Value.ClientSecret);
        }

        [HttpGet]
        [Route("")]
        public OkObjectResult Get()
        {
            return Ok($"Echo bot running successfully via {this._microsoftAppCredentials.MicrosoftAppId}");
        }

        [HttpPost]
        [Route("")]
        public virtual async Task<OkResult> Post([FromBody]Activity activity)
        {
            var client = new ConnectorClient(new Uri(activity.ServiceUrl), this._microsoftAppCredentials);
            var reply = activity.CreateReply();
            if (activity.Type == ActivityTypes.Message)
            {
                reply.Text = $"echo: {activity.Text}";
            }
            else
            {
                reply.Text = $"activity type: {activity.Type}";
            }
            await client.Conversations.ReplyToActivityAsync(reply);
            return Ok();
        }
    }
}

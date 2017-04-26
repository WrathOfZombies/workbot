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
using Microsoft.Extensions.Configuration;

namespace WorkBot.Controllers
{
    [Route("api/[controller]")]
    public class EchoController : Controller
    {
        private readonly MicrosoftAppCredentials _credentials;

        public EchoController(MicrosoftAppCredentials credentials)
        {
            this._credentials = credentials;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok($"Echo bot running successfully via {this._credentials.MicrosoftAppId}");
        }

        [Authorize(Roles = "Bot")]
        [HttpPost]
        public virtual async Task<IActionResult> Post([FromBody]Activity activity)
        {
            try
            {
                var client = new ConnectorClient(new Uri(activity.ServiceUrl), this._credentials);
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
            catch(Exception exception)
            {
                return BadRequest(exception);
            }
        }
    }
}

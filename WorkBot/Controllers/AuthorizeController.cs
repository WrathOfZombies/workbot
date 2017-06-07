using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WorkBot.Core;
using Microsoft.Bot.Builder.Dialogs;
using WorkBot.Core.OAuth;
using Microsoft.Bot.Builder.ConnectorEx;
using System.Net.Http;
using System.Threading;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;

namespace WorkBot.Controllers
{
    [Route("api/[controller]")]
    public class AuthorizeController : Controller
    {
        private Conversation _converstaion;

        AuthorizeController(Conversation conversation)
        {
            this._converstaion = conversation;
        }

        [HttpGet]
        public async Task<ActionResult> Get([Bind] string userId, [Bind] string botId, [Bind] string conversationId, [Bind] string channelId, [Bind] string serviceUrl, [Bind] string code, [Bind] string state, CancellationToken token)
        {
            return Ok("You are now logged in! Continue talking to the bot.");
        }
    }
}

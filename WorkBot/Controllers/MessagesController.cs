using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WorkBot.Core;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using WorkBot.Core.OAuth;

namespace WorkBot.Controllers
{
    [Route("api/[controller]")]
    public class MessagesController : Controller
    {
        private readonly BotActivityHandler _handler;
        private readonly Conversation _conversation;

        public MessagesController(BotActivityHandler handler, Conversation conversation)
        {
            this._handler = handler;
            this._conversation = conversation;
        }

        [HttpGet]
        public IActionResult Get()
        {
#if DEBUG            
            return Ok($"{this._handler.config.Name} running successfully...");
#else
            return Ok($"{this._handler.config.Name} running successfully...");
#endif
        }

        [Authorize(Roles = "Bot")]
        [HttpPost]
        public virtual async Task<IActionResult> Post([FromBody]Microsoft.Bot.Connector.Activity activity)
        {
            // Check if activity is of type message
            if (activity != null && activity.GetActivityType() == ActivityTypes.Message)
            {
                await this._conversation.SendAsync(activity, () => SimpleGitHubAuthDialog.dialog);
            }
            else
            {
                await this._handler.QueueActivity(activity);
            }
            return Ok();
        }
    }
}

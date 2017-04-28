using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WorkBot.Core;

namespace WorkBot.Controllers
{
    [Route("api/[controller]")]
    public class MessagesController : Controller
    {
        private readonly BotActivityHandler _handler;

        public MessagesController(BotActivityHandler handler)
        {
            this._handler = handler;
        }

        [HttpGet]
        public IActionResult Get()
        {
#if DEBUG            
            return Ok(this._handler.config);
#else
            return Ok($"{this._handler.config.Name} running successfully...");
#endif
        }

        [Authorize(Roles = "Bot")]
        [HttpPost]
        public virtual async Task<IActionResult> Post([FromBody]Microsoft.Bot.Connector.Activity activity)
        {
            await this._handler.QueueActivity(activity);
            return Ok();
        }
    }
}

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

namespace WorkBot.Controllers
{
    [Route("api/[controller]")]
    public class AuthorizeController : Controller
    {
        [HttpGet]
        public async Task<HttpResponseMessage> Get([Bind] string userId, [Bind] string botId, [Bind] string conversationId, [Bind] string channelId, [Bind] string serviceUrl, [Bind] string code, [Bind] string state, CancellationToken token)
        {
            // Get the resumption cookie
            var address = new Address
                (
                    // purposefully using named arguments because these all have the same type
                    botId: GitHubHelpers.TokenDecoder(botId),
                    channelId: channelId,
                    userId: GitHubHelpers.TokenDecoder(userId),
                    conversationId: GitHubHelpers.TokenDecoder(conversationId),
                    serviceUrl: GitHubHelpers.TokenDecoder(serviceUrl)
                );
            var conversationReference = address.ToConversationReference();

            // Exchange the Facebook Auth code with Access token
            var accessToken = await GitHubHelpers.ExchangeCodeForAccessToken(conversationReference, code, SimpleGitHubAuthDialog.GitHubOauthCallback.ToString());

            // Create the message that is send to conversation to resume the login flow
            var msg = conversationReference.GetPostToBotMessage();
            msg.Text = $"token:{accessToken.AccessToken}";

            // Resume the conversation to SimpleFacebookAuthDialog
            await Conversation.ResumeAsync(conversationReference, msg);

            using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, msg))
            {
                var dataBag = scope.Resolve<IBotData>();
                await dataBag.LoadAsync(token);
                ConversationReference pending;
                if (dataBag.PrivateConversationData.TryGetValue("persistedCookie", out pending))
                {
                    // remove persisted cookie
                    dataBag.PrivateConversationData.RemoveValue("persistedCookie");
                    await dataBag.FlushAsync(token);
                    return Request.CreateResponse("You are now logged in! Continue talking to the bot.");
                }
                else
                {
                    // Callback is called with no pending message as a result the login flow cannot be resumed.
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, new InvalidOperationException("Cannot resume!"));
                }
            }
        }
    }
}

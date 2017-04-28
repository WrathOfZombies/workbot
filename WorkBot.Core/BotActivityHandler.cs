using Microsoft.Bot.Connector;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WorkBot.Core.Models;

namespace WorkBot.Core
{
    public class BotActivityHandler : IDisposable
    {
        public readonly BotConfiguration config;

        private readonly ILogger<BotActivityHandler> _logger;
        private readonly MicrosoftAppCredentials _credentials;

        public BotActivityHandler(IOptions<BotConfiguration> config, MicrosoftAppCredentials credentials, ILogger<BotActivityHandler> logger)
        {
            this.config = config.Value;
            if (this.config == null)
            {
                throw new ArgumentNullException("Bot Configuration cannot be null");
            }
            this._credentials = credentials;
            this._logger = logger;
        }

        private async Task<ResourceResponse> SendReply(Activity response)
        {
            using (var client = new ConnectorClient(new Uri(response.ServiceUrl), this._credentials))
            {
                return await client.Conversations.ReplyToActivityAsync(response);
            }
        }

        private Activity HandleMessage(Activity activity)
        {
            var reply = activity.CreateReply();
            if (!string.IsNullOrEmpty(activity.Text))
            {
                reply.Text = activity.Text.Replace(this.config.Name, "").Trim();
            }
            return reply;
        }

        private Activity HandleGenericType(Activity activity)
        {
            throw new NotImplementedException(activity.Type);
        }

        public async Task<ResourceResponse> QueueActivity(Activity request)
        {
            Activity response;
            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException("Invalid message received");
                }
                switch (request.Type)
                {
                    case ActivityTypes.Message:
                        response = this.HandleMessage(request);
                        break;

                    default:
                        response = this.HandleGenericType(request);
                        break;
                }
            }
            catch (Exception exception)
            {
                string message = "Umm... I cannot help you with that.";
#if DEBUG
                message += $"{System.Environment.NewLine}{exception.Message}";
#endif
                response = request.CreateReply(message);
            }
            return await this.SendReply(response);
        }

        public void Dispose()
        {
        }
    }
}

using Microsoft.Bot.Connector;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WorkBot.Core
{
    public class BotActivityHandler : IDisposable
    {
        public readonly BotConfiguration config;

        private readonly MicrosoftAppCredentials _credentials;

        public BotActivityHandler(IOptions<BotConfiguration> config)
        {
            this.config = config.Value;
            if (this.config == null)
            {
                throw new ArgumentNullException("Bot Configuration cannot be null");
            }
            this._credentials = new MicrosoftAppCredentials(this.config.AppId, this.config.AppSecret);
        }

        private Task<ResourceResponse> CreateClient(string serviceUrl, Activity reply)
        {
            var client = new ConnectorClient(new Uri(serviceUrl), this._credentials);
            return client.Conversations.ReplyToActivityAsync(reply);
        }

        private Activity HandleMessage(Activity activity)
        {
            var reply = activity.CreateReply();
            if (activity.Attachments.Count > 0)
            {

            }

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

        public async Task<ResourceResponse> QueueActivity(Activity activity)
        {
            Activity reply;
            try
            {
                switch (activity.Type)
                {
                    case ActivityTypes.Message:
                        reply = this.HandleMessage(activity);
                        break;

                    default:
                        reply = this.HandleGenericType(activity);
                        break;
                }
            }
            catch (Exception exception)
            {
                reply = activity.CreateReply($"Umm... I cannot help you with that.{System.Environment.NewLine}{exception.Message}");
            }
            return await this.CreateClient(activity.ServiceUrl, reply);
        }

        public void Dispose()
        {
        }
    }
}

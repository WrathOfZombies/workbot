using Microsoft.Bot.Builder.ConnectorEx;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WorkBot.Core.Models;

namespace WorkBot.Core.Generic
{
    /// <summary>
    /// This Dialog implements a generic OAuth 2.0 Login flow.     
    /// Inspired by https://github.com/Microsoft/BotBuilder/blob/master/CSharp/Samples/SimpleFacebookAuthBot/SimpleFacebookAuthDialog.cs
    /// </summary>
    public abstract class OAuthDialog<T> : IDialog<string> where T : OAuthToken
    {
        #region Private Properties
        /// <summary>
        /// OAuth callback registered for the application.        
        /// </summary>        
        private readonly string clientId;

        /// <summary>
        /// OAuth callback registered for the application.        
        /// </summary>        
        private readonly string clientSecret;

        /// <summary>
        /// OAuth callback registered for the application.        
        /// </summary>        
        private readonly string redirectUrl;

        /// <summary>
        /// OAuth scope for the application.        
        /// </summary>        
        private readonly string scope;
        #endregion

        #region Abstract Properties
        /// <summary>
        /// A unique name for the provider.
        /// </summary>
        public abstract string Provider { get; }

        /// <summary>
        /// AccessToken obtained for the application.
        /// </summary>
        public abstract T Token { get; }

        /// <summary>
        /// Authorize url where a code can be obtained for the application.
        /// </summary>
        public abstract string AuthorizeUrl { get; }

        /// <summary>
        /// TokenUrl where an access token can be obtained for the application.
        /// </summary>
        public abstract string TokenUrl { get; }

        /// <summary>
        /// ValidationUrl where an access token can be validated for the application.
        /// </summary>
        public abstract string ValidationUrl { get; }
        #endregion

        public OAuthDialog(string clientId, string clientSecret, string redirectUrl, string scope)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.redirectUrl = redirectUrl;
            this.scope = scope;
        }

        public async Task StartAsync(IDialogContext context)
        {
            await LogInAsync(context);
        }

        #region Bot Authentication Helpers
        /// <summary>
        /// Login the user.
        /// </summary>
        /// <param name="context">The Dialog context.</param>
        /// <returns> A task that represents the login action.</returns>
        private async Task LogInAsync(IDialogContext context)
        {
            if (!context.PrivateConversationData.TryGetValue(Token.AccessToken, out string token))
            {
                var conversationReference = context.Activity.ToConversationReference();
                context.PrivateConversationData.SetValue($"{Provider}_token", conversationReference);

                var loginUrl = GetLoginUrl(conversationReference);
                var attachment = SigninCard.Create("You need to authorize me to access your profile", $"Login to {Provider}!", loginUrl).ToAttachment();

                var reply = context.MakeMessage();
                reply.Text = $"Please login into {Provider}";
                reply.Attachments.Add(attachment);
                await context.PostAsync(reply);

                context.Wait(MessageReceivedAsync);
            }
            else
            {
                context.Done(token);
            }
        }

        /// <summary>
        /// Callback when the access token is received
        /// </summary>
        /// <param name="context">The Dialog context.</param>
        /// <param name="argument"></param>
        /// <returns>A task that represents the login action.</returns>
        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await (argument);
            if (msg.Text.StartsWith("access_token:"))
            {
                // Dialog is resumed by the OAuth callback and access token
                // is encoded in the message.Text
                var token = msg.Text.Remove(0, "access_token:".Length);
                context.PrivateConversationData.SetValue(Token.AccessToken, token);
                context.Done(token);
            }
            else
            {
                await LogInAsync(context);
            }
        }

        /// <summary>
        /// The chain of dialogs that implements the login/logout process for the bot
        /// </summary>
        public static IDialog<string> Authenticate(OAuthDialog<T> dialog)
        {
            return Chain
            .PostToChain()
            .Switch(
                new Case<IMessageActivity, IDialog<string>>(msg =>
                {
                    var regex = new Regex("^login", RegexOptions.IgnoreCase);
                    return regex.IsMatch(msg.Text);
                }, (ctx, msg) =>
                {
                    // User wants to login, send the message to Facebook Auth Dialog
                    return Chain.ContinueWith(dialog,
                                async (context, res) =>
                                {
                                    var token = await res;
                                    return Chain.Return($"Your are logged with: {token}");
                                });
                }),
                new Case<IMessageActivity, IDialog<string>>((msg) =>
                {
                    var regex = new Regex("^logout", RegexOptions.IgnoreCase);
                    return regex.IsMatch(msg.Text);
                }, (ctx, msg) =>
                {
                    return Chain.Return($"Your are logged out!");
                }),
                new DefaultCase<IMessageActivity, IDialog<string>>((ctx, msg) =>
                {
                    return Chain.Return("Say \"login\" when you want to login to Facebook!");
                })
            ).Unwrap().PostToUser();
        }
        #endregion

        #region OAuth Helpers
        /// <summary>
        /// Returns the login url for the application.
        /// </summary>
        private string GetLoginUrl(ConversationReference conversationReference)
        {
            var redirectUri = GetOAuthCallBack(conversationReference, this.redirectUrl);
            var uri = GetUri(AuthorizeUrl,
                Tuple.Create("client_id", this.clientId),
                Tuple.Create("redirect_uri", redirectUrl),
                Tuple.Create("response_type", "code"),
                Tuple.Create("scope", "public_profile,email"),
                Tuple.Create("state", Convert.ToString(new Random().Next(9999))));

            return uri.ToString();
        }

        private async Task<T> ExchangeCodeForAccessToken(ConversationReference conversationReference, string code, string oauthCallback)
        {
            var redirectUri = GetOAuthCallBack(conversationReference, oauthCallback);
            var uri = GetUri(TokenUrl,
                Tuple.Create("client_id", this.clientId),
                Tuple.Create("redirect_uri", redirectUrl),
                Tuple.Create("client_secret", this.clientSecret),
                Tuple.Create("code", code));

            return await Request(uri);
        }

        private async Task<bool> ValidateAccessToken(string accessToken, Predicate<dynamic> validate)
        {
            var uri = GetUri(ValidationUrl,
                Tuple.Create("input_token", accessToken),
                Tuple.Create("access_token", $"{this.clientId}|{this.clientSecret}"));

            var res = await Request(uri).ConfigureAwait(false);
            return validate(res);
        }

        private string GetOAuthCallBack(ConversationReference conversationReference, string oauthCallback)
        {
            var uri = GetUri(oauthCallback,
                Tuple.Create("userId", Encode(conversationReference.User.Id)),
                Tuple.Create("botId", Encode(conversationReference.Bot.Id)),
                Tuple.Create("conversationId", Encode(conversationReference.Conversation.Id)),
                Tuple.Create("serviceUrl", Encode(conversationReference.ServiceUrl)),
                Tuple.Create("channelId", conversationReference.ChannelId));
            return uri.ToString();
        }

        private string Encode(string token)
        {
            return System.Net.WebUtility.UrlEncode(token);
        }

        private string Decode(string token)
        {
            return System.Net.WebUtility.UrlDecode(token);
        }

        private async Task<T> Request(Uri uri)
        {
            string json;
            using (HttpClient client = new HttpClient())
            {
                json = await client.GetStringAsync(uri).ConfigureAwait(false);
            }
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Unable to deserialize the {Provider} response.", ex);
            }
        }

        private Uri GetUri(string endPoint, params Tuple<string, string>[] queryParams)
        {
            var builder = new UriBuilder(endPoint);
            var segments = queryParams
                .Select(param => $"{param.Item1}={Encode(param.Item2)}")
                .ToArray();

            builder.Query = String.Join("&", segments);
            return builder.Uri;
        }
        #endregion
    }
}

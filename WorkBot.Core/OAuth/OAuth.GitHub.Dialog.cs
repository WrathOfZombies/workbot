using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.ConnectorEx;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;

namespace WorkBot.Core.OAuth
{
    /// <summary>
    /// This Dialog implements the OAuth login flow for Facebook. 
    /// You can read more about Facebook's login flow here: https://developers.facebook.com/docs/facebook-login/manually-build-a-login-flow
    /// </summary>
    public class SimpleGitHubAuthDialog : IDialog<string>
    {

        /// <summary>
        /// OAuth callback registered for Facebook app.
        /// <see cref="Controllers.OAuthCallbackController"/> implementats the callback.
        /// </summary>
        /// <remarks>
        /// Make sure to replace this with the appropriate website url registered for your Facebook app.
        /// </remarks>
        public static readonly Uri GitHubOauthCallback = new Uri("https://localhost:44300/api/authorize");

        /// <summary>
        /// The key that is used to keep the AccessToken in <see cref="Microsoft.Bot.Builder.Dialogs.Internals.IBotData.PrivateConversationData"/>
        /// </summary>
        public static readonly string AuthTokenKey = "GitHubAuthToken";

        /// <summary>
        /// The chain of dialogs that implements the login/logout process for the bot
        /// </summary>
        public static readonly IDialog<string> dialog = Chain
            .PostToChain()
            .Switch(
                new Case<IMessageActivity, IDialog<string>>((msg) =>
                {
                    var regex = new Regex("^login", RegexOptions.IgnoreCase);
                    return regex.IsMatch(msg.Text);
                }, (ctx, msg) =>
                {
                    // User wants to login, send the message to Facebook Auth Dialog
                    return Chain.ContinueWith(new SimpleGitHubAuthDialog(),
                                async (context, res) =>
                                {
                                    // The Facebook Auth Dialog completed successfully and returend the access token in its results
                                    var token = await res;
                                    var profile = await GitHubHelpers.GetGitHubProfile(JsonConvert.DeserializeObject<GitHubAccessToken>(token));
                                    context.UserData.SetValue("name", profile.Name);
                                    return Chain.Return($"Your are logged in as: {profile.Name}");
                                });
                }),
                new Case<IMessageActivity, IDialog<string>>((msg) =>
                {
                    var regex = new Regex("^logout", RegexOptions.IgnoreCase);
                    return regex.IsMatch(msg.Text);
                }, (ctx, msg) =>
                {
                    // Clearing user related data upon logout
                    ctx.PrivateConversationData.RemoveValue(AuthTokenKey);
                    ctx.UserData.RemoveValue("name");
                    return Chain.Return($"Your are logged out!");
                }),
                new DefaultCase<IMessageActivity, IDialog<string>>((ctx, msg) =>
                {
                    string token;
                    string name = string.Empty;
                    if (ctx.PrivateConversationData.TryGetValue(AuthTokenKey, out token) && ctx.UserData.TryGetValue("name", out name))
                    {
                        return Chain.Return($"Your are logged in as: {name}");
                    }
                    else
                    {
                        return Chain.Return("Say \"login\" when you want to login to Facebook!");
                    }
                })
            ).Unwrap().PostToUser();


        public async Task StartAsync(IDialogContext context)
        {
            await LogIn(context);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var msg = await (argument);
            if (msg.Text.StartsWith("token:"))
            {
                // Dialog is resumed by the OAuth callback and access token
                // is encoded in the message.Text
                var token = msg.Text.Remove(0, "token:".Length);
                context.PrivateConversationData.SetValue(AuthTokenKey, token);
                context.Done(token);
            }
            else
            {
                await LogIn(context);
            }
        }

        /// <summary>
        /// Login the user.
        /// </summary>
        /// <param name="context"> The Dialog context.</param>
        /// <returns> A task that represents the login action.</returns>
        private async Task LogIn(IDialogContext context)
        {
            string token;
            if (!context.PrivateConversationData.TryGetValue(AuthTokenKey, out token))
            {
                var conversationReference = context.Activity.ToConversationReference();

                context.PrivateConversationData.SetValue("persistedCookie", conversationReference);

                // sending the sigin card with Facebook login url
                var reply = context.MakeMessage();
                var ghLoginUrl = GitHubHelpers.GetGitHubLoginUrl(conversationReference, GitHubOauthCallback.ToString());
                reply.Text = "Please login in using this card";
                reply.Attachments.Add(SigninCard.Create("You need to authorize me",
                                                        "Login to GitHub!",
                                                        ghLoginUrl
                                                        ).ToAttachment());
                await context.PostAsync(reply);
                context.Wait(MessageReceivedAsync);
            }
            else
            {
                context.Done(token);
            }
        }
    }
}
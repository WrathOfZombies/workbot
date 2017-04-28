namespace WorkBot.Core
{
    /// <summary>
    /// Contains details of the Bot and is extracted from the Environment config or appsettings.json
    /// These details are initially configured at https://dev.botframework.com/bots?id=<handle>
    /// </summary>
    public class BotConfiguration
    {
        /// <summary>
        /// Microsoft AppId registered with the BotFramework
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// Microsoft AppSecret registered with the BotFramework
        /// </summary>
        public string AppSecret { get; set; }

        /// <summary>
        /// Handle of the bot registered with the Bot Framework
        /// </summary>
        public string Handle { get; set; }

        /// <summary>
        /// Name of the bot registered with the Bot Framework
        /// </summary>
        public string Name { get; set; }        
    }
}
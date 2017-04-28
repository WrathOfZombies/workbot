using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace WorkBot.Core.Models
{
    public class OAuthToken
    {
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }

        [JsonProperty(PropertyName = "token_type")]
        public string TokenType { get; set; }

        [JsonProperty(PropertyName = "expires_in")]
        public long ExpiresIn { get; set; }
    }
}

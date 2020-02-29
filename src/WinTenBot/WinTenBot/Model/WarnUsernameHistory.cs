﻿using Newtonsoft.Json;

namespace WinTenBot.Model
{
    class WarnUsernameHistory
    {
        [JsonProperty("from_id")]
        public int FromId { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("step_count")]
        public long StepCount { get; set; }

        [JsonProperty("chat_id")]
        public long ChatId { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }
    }
}

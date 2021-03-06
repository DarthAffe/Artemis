﻿using System.ComponentModel;
using Newtonsoft.Json;

namespace Artemis.Settings
{
    public abstract class GameSettings : EffectSettings
    {
        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool Enabled { get; set; }

        [DefaultValue("Default")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string LastProfile { get; set; }
    }
}
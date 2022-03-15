﻿using Monaco;
using Monaco.Editor;
using Newtonsoft.Json;

namespace Monaco.Languages
{
    public sealed class TextEdit
    {
        [JsonProperty("range", NullValueHandling = NullValueHandling.Ignore)]
        public IRange Range { get; set; }

        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        [JsonProperty("eol", NullValueHandling = NullValueHandling.Ignore)]
        public EndOfLineSequence Eol { get; set; }
    }
}
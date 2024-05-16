using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Images_Classifier.Models
{
    public class ImageData
    {
        [JsonPropertyName("id")]
        public string Id { set; get; }

        [JsonPropertyName("format")]
        public string Format { set; get; }

        [JsonPropertyName("parent")]
        public string Parent { set; get; }

        [JsonPropertyName("author")]
        public string Author { set; get; }

        [JsonPropertyName("rating")]
        public int Rating { set; get; }

        [JsonPropertyName("text")]
        public Text Text { set; get; }

        [JsonPropertyName("tags")]
        public Tag Tags { set; get; }

        [JsonPropertyName("comment")]
        public string Comment { set; get; }

        [JsonPropertyName("title")]
        public string Title { set; get; }
    }

    public class Tag
    {
        [JsonPropertyName("parodies")]
        public string[] Parodies { set; get; }

        [JsonPropertyName("characters")]
        public Character Characters { set; get; }

        [JsonPropertyName("poses")]
        public string[] Poses { set; get; }

        [JsonPropertyName("clothes")]
        public string[] Clothes { set; get; }

        [JsonPropertyName("sexes")]
        public string[] Sexes { set; get; }

        [JsonPropertyName("others")]
        public string[] Others { set; get; }
    }

    public class Character
    {
        [JsonPropertyName("sexes")]
        public Dictionary<string, int> Sex { set; get; }

        [JsonPropertyName("names")]
        public string[] Names { set; get; }

        [JsonPropertyName("racial_attributes")]
        public string[] RacialAttributes { set; get; }

        [JsonPropertyName("attributes")]
        public string[] Attributes { set; get; }
    }

    public class Text
    {
        [JsonPropertyName("lang")]
        public string Language { set; get; }

        [JsonPropertyName("content")]
        public string[] Content { set; get; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace RDA.ContentHub.AzureFunctions.Models
{
    public class CHMetrics
    {
        public string ChannelAccount { get; set; }
        public string PostIdFromChannel { get; set; }
        public string Clicks { get; set; } = "0";
        public string Mentions { get; set; } = "0";
        public string Likes { get; set; } = "0";
        public string Comments { get; set; } = "0";
        public string Reach { get; set; } = "0";
        public string Favorites { get; set; } = "0";
        public string PlusOnes { get; set; } = "0";
        public string Replies { get; set; } = "0";
        public string Retweets { get; set; } = "0";
        public string Reshares { get; set; } = "0";
        public string Repins { get; set; } = "0";
        public string Connections { get; set; } = "0";
    }
}

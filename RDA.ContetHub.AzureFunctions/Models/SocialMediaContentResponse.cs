using System;
using System.Collections.Generic;
using System.Text;

namespace RDA.ContentHub.AzureFunctions.Models
{
    public class SocialMediaContentResponse
    {
        public string Message { get; set; }
        public string Tags { get; set; }
        public string[] Channels {get; set;}
        public string BlogUrl { get; set; }
        public string ImageUrl { get; set; }
    }
}

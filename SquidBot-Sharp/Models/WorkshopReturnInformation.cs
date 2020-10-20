using System;

namespace SquidBot_Sharp.Models
{
    public class WorkshopReturnInformation
    {
        public string Description { get; set; }
        public string Title { get; set; }
        public string FileURL { get; set; }
        public string PreviewURL { get; set; }
        public string PublishedFileID { get; set; }
        public string Filename { get; set; }
        public string CreatorID { get; set; }
        public Int64 TimeCreated { get; set; }
        public Int64 TimeUpdated { get; set; }
    }
}

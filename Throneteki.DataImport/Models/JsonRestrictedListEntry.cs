namespace CrimsonDev.Throneteki.DataImport.Models
{
    using System;
    using System.Collections.Generic;

    public class JsonRestrictedListEntry
    {
        public string Version { get; set; }
        public DateTime Date { get; set; }
        public List<string> JoustCards { get; set; }
        public List<string> MeleeCards { get; set; }
    }
}
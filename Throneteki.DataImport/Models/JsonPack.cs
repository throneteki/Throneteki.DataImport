namespace CrimsonDev.Throneteki.DataImport.Models
{
    using System;
    using System.Collections.Generic;

    public class JsonPack
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public int? CgdbId { get; set; }
        public DateTime? ReleaseDate { get; set; }

        public ICollection<JsonCard> Cards { get; set; }
    }
}
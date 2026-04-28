using System;
using System.Data;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace Find_Me_A
{
    // not sure where to put this atm, but this is the overhead needed to access the searches
    public class Title
    {
        public string TitleName { get; set; } = "";
        public string TitleType { get; set; } = "";
        public DateTime ReleaseDate { get; set; }
        public decimal AverageRating { get; set; }
        public int NumberOfRatings { get; set; }
        public int? EpisodeCount { get; set; } 
        public List<string> Genres { get; set; } = new List<string>();
        public List<string> Actors { get; set; } = new List<string>();
        public string? PosterPath { get; set; }
        public string? Overview { get; set; }
        public string? ImdbId { get; set; }
        public string? TrailerUrl { get; set; }
    }
    class Connection
    {
        }
}
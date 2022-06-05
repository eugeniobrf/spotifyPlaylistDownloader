namespace SpotifyPlaylistDownloader
{
    public class Metadata
    {
        public Metadata(string name, List<string> artists, string albumName, List<string> albumArtists, string[] albumImages, int durationMs, uint year, uint track)
        {
            Name = name;
            Artists = artists;
            AlbumName = albumName;
            AlbumArtists = albumArtists;
            AlbumImages = albumImages;
            DurationMs = durationMs;
            Year = year;
            Track = track;
        }

        public string Name { get; set; }
        public List<string> Artists { get; set; }
        public string AlbumName { get; set; }
        public List<string> AlbumArtists { get; set; }
        public string[] AlbumImages { get; set; }
        public int DurationMs { get; set; }
        public uint Year { get; set; }
        public uint Track { get; set; }

        public override string ToString()
        {
            return $"{string.Join(", ", Artists)} - {Name} - {AlbumName}";
        }
    }
}
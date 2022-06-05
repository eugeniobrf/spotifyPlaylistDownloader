using Microsoft.Extensions.Configuration;
using SpotifyPlaylistDownloader;
using SpotifyAPI.Web;
using TagLib;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Converter;

class Program
{
    private static int NumberSongsSpotifyPlaylist = 0;
    private static int NumberDownloadedSongs = 0;
    private static int NumberErros = 0;
    private static readonly List<Metadata> ErrorList = new();

    private static void PrintStatus()
    {
        Console.Clear();
        Console.WriteLine($"Foram encontradas {NumberSongsSpotifyPlaylist} músicas na playlist do spotify.");
        Console.WriteLine($"Musicas baixadas: {NumberDownloadedSongs}");
        Console.WriteLine($"Musicas com problema ao baixar: {NumberErros}");
        foreach (var metadata in ErrorList)
        {
            Console.WriteLine(metadata);
        }
    }

    private static async Task<List<Metadata>> GetMetadataFromPlaylistSpotify(string playlistId, string clientIdSpotfy, string clientSecretSpotify)
    {
        Console.WriteLine("Buscando dados da playlist...");
        var metaDataList = new List<Metadata>();
        IList<PlaylistTrack<IPlayableItem>> playlist;

        try
        {
            var spotify = new SpotifyClient(
                    SpotifyClientConfig.CreateDefault()
                        .WithAuthenticator(new ClientCredentialsAuthenticator(clientIdSpotfy, clientSecretSpotify))
                    );

            playlist = await spotify.PaginateAll(await spotify.Playlists.GetItems(playlistId));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Não foi possível realizar a conexão com o spotify");
            Console.Error.WriteLine($"Não foi possível realizar a conexão com o spotify pelo seguinte motivo: {ex.Message}");
            return metaDataList;
        }
 
        foreach(var item in playlist.ToList())
        {
            if(item.Track is FullTrack track)
            {
                metaDataList.Add(
                    new Metadata(
                        track.Name, 
                        track.Artists.Select(x => x.Name).ToList(), 
                        track.Album.Name,
                        track.Album.Artists.Select(x => x.Name).ToList(),
                        track.Album.Images.Select(x => x.Url).ToArray(), 
                        track.DurationMs, 
                        Convert.ToUInt32(track.Album.ReleaseDate[..4]),
                        Convert.ToUInt32(track.TrackNumber)
                        ));
            }
        }

        NumberSongsSpotifyPlaylist = metaDataList.Count;
        PrintStatus();

        return metaDataList;
    }

    private static async Task SearchMusicOnYoutubeAndDownload(List<Metadata> metaDataList)
    {
        var youtube = new YoutubeClient();
        System.IO.Directory.CreateDirectory("musics");

        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = 8
        };

        await Parallel.ForEachAsync(metaDataList, parallelOptions, new Func<Metadata, CancellationToken, ValueTask>(async (m, c) =>
        {
            try
            {
                var link = await GetMusicLinkOnYoutube(youtube, m);
                await DownloadMusic(youtube, m, link);
                NumberDownloadedSongs++;
                PrintStatus();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro no processo de descoberta de link e download de {m}: {ex}");
                ErrorList.Add(m);
                NumberErros++;
                PrintStatus();
            }

        }));
    }

    private static async Task<string> GetMusicLinkOnYoutube(YoutubeClient youtube, Metadata metadata, int diffTime = 5000)
    {
        var stringSearch = metadata.ToString();
        var diff = diffTime;

        if(diffTime > 10000)
        {
            stringSearch = $"{string.Join(", ", metadata.Artists)} - {metadata.Name}";
            diff = diffTime - 10000;
        }

        try
        {
            var link = (await youtube.Search
                .GetVideosAsync(stringSearch))
                .FirstOrDefault(x =>
                        x.Duration.HasValue
                        && x.Duration.Value.TotalMilliseconds >= (metadata.DurationMs - diff)
                        && x.Duration.Value.TotalMilliseconds <= (metadata.DurationMs + diff)
                        )?.Url;
            
            if (link == null)
            {
                return await GetMusicLinkOnYoutube(youtube, metadata, diffTime + 5000);
            }

            return link;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao achar o link da musica {metadata}: {ex}");
            return await GetMusicLinkOnYoutube(youtube, metadata, diffTime);
        }
    }

    private static string RemoveInvalidCharactersFromFileName(string fileName)
    {
        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        return new string(fileName.Where(m => !invalidChars.Contains(m)).ToArray<char>());
    }

    private async static Task UpdateMetaDataSong(string fileName, Metadata m)
    {
        var tfile = TagLib.File.Create(fileName);
        tfile.Tag.Title = m.Name;
        tfile.Tag.Performers = m.Artists.ToArray();
        tfile.Tag.Year = m.Year;
        tfile.Tag.Track = m.Track;
        tfile.Tag.Album = m.AlbumName;
        tfile.Tag.AlbumArtists = m.AlbumArtists.ToArray();
        
        var pic = new IPicture[m.AlbumImages.Length];
        for (var i = 0; i < m.AlbumImages.Length; i++)
        {
            var client = new HttpClient();
            var response = await client.GetAsync(new Uri(m.AlbumImages[i]));
            var data = await response.Content.ReadAsByteArrayAsync();
            pic[i] = new TagLib.Id3v2.AttachmentFrame
            {
                Type = TagLib.PictureType.FrontCover,
                Description = "Cover",
                MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg,
                Data = data,
                TextEncoding = TagLib.StringType.UTF16,
            };
        }
        tfile.Tag.Pictures = pic;
        
        tfile.Save();
    }

    private static async Task DownloadMusic(YoutubeClient youtube, Metadata m, string yotubeLink)
    {
        var fileName = "musics/" + RemoveInvalidCharactersFromFileName($"{m.Name} - {string.Join(", ", m.Artists)}.mp3");
        
        try
        {
            await youtube.Videos.DownloadAsync(yotubeLink, fileName, o => o
                    .SetContainer("mp3")
                    .SetPreset(ConversionPreset.VerySlow)
                    .SetFFmpegPath("ffmpeg.exe")
                );
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao baixar {m} do link {yotubeLink}: {ex}");
            await DownloadMusic(youtube, m, yotubeLink); 
        }

        try
        {
            await UpdateMetaDataSong(fileName, m);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao atualizar metadados de {m}: {ex}");
            m.AlbumImages = Array.Empty<string>();
            await UpdateMetaDataSong(fileName, m);
        }
    }

    static async Task Main()
    {
        Console.SetError(new StreamWriter("./erros.txt"));

        IConfigurationRoot config;
        try
        {
            var builder = new ConfigurationBuilder()
                      .SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("appsettings.json");
            config = builder.Build();
        }
        catch (Exception)
        {
            Console.WriteLine("Erro ao abrir o arquivo appsettings.json!");
            Console.Error.WriteLine("Erro ao abrir o arquivo appsettings.json!");
            return;
        }

        if (!System.IO.File.Exists("ffmpeg.exe"))
        {
            Console.WriteLine("Para realizar o download de músicas em mp3 é necessário que o arquivo ffmpeg.exe, disponível em 'https://ffbinaries.com/downloads' esteja na mesma pasta que o executável deste programa");
            Console.Error.WriteLine("Para realizar o download de músicas em mp3 é necessário que o arquivo ffmpeg.exe, disponível em 'https://ffbinaries.com/downloads' esteja na mesma pasta que o executável deste programa");
            return;
        }

        var clientId = config.GetSection("clientId").Value;
        var clientSecret = config.GetSection("clientSecret").Value;

        string? playlistId;
        do
        {
            Console.Write("Entre com o id da playlist a ser baixada: ");
            playlistId = Console.ReadLine();
        } while (string.IsNullOrEmpty(playlistId));

        var metaData = await GetMetadataFromPlaylistSpotify(playlistId, clientId, clientSecret);
        await SearchMusicOnYoutubeAndDownload(metaData);
    }
}
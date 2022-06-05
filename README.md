# Spotify Playlist Downloader

Este projeto foi desenvolvido em .NET 6.0 com o intuito de se baixar os arquivos mp3 de todas as músicas de uma playlist no spotify. 

Para isto, é feita uma conexão com a api do spotify, facilitada pela biblioteca  [SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET) buscando todos os metadados das músicas contidas na playlist.

A partir dos metadados obtidos na api do spotify, faz-se uma busca no youtube e o posterior download do vídeo encontrado em formato mp3. Estas operações são facilitadas pelas bibliotecas [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) e [YoutubeExplode.Converter](https://github.com/Tyrrrz/YoutubeExplode).

Por fim, são colocados os metadados das músicas nos mp3 baixados através da biblioteca [TagLibSharp](https://github.com/mono/taglib-sharp)

Ao fim do download, os arquivos de música poderam ser encontrados dentro da pasta musics, no local da execução do arquivo.

# Como utilizar
Para utilizar este projeto, primeiramente deve-se criar um app no [Spotify for Developers](https://developer.spotify.com/dashboard), copiando o clientId e o clientSecret gerado para o [appsettings.json](https://github.com/eugeniobrf/spotifyPlaylistDownloader/blob/master/SpotifyPlaylistDownloader/appsettings.json).

Posteriormente, deve-se realizar o donwload do [ffmpeg.exe](https://ffbinaries.com/downloads), e colá-lo dentro da pasta [SpotifyPlaylistDownloader](https://github.com/eugeniobrf/spotifyPlaylistDownloader/tree/master/SpotifyPlaylistDownloader).

Por fim, basta obter o id da playlist do spotify a qual se deseja baixar, o que pode ser obtido através do link da mesma.

using AnthemAPI.Common;

namespace AnthemAPI.Models;

public class SearchResult
{
    public object Data { get; set; }
    public  SearchResultType Type { get; }

    public SearchResult(Album album)
    {
        Data = album;
        Type = SearchResultType.Album;
    }

    public SearchResult(Artist artist)
    {
        Data = artist;
        Type = SearchResultType.Artist;
    }

    public SearchResult(Track track)
    {
        Data = track;
        Type = SearchResultType.Track;
    }
}

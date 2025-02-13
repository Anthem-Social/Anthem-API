namespace AnthemAPI.Common;

public enum ContentType
{
    Text,
    Track,
    Snippet,
    AlbumReview,
    TopFive
}

public enum Relationship
{
    A_Follows_B,
    B_Follows_A,
    Mutual,
    None,
    Self
}

public enum MusicProvider
{
    SpotifyFree,
    SpotifyPremium
}

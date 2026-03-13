using LmsApp.Models;
using System.Text.RegularExpressions;

namespace LmsApp.Services;

public static class VideoService
{
    private static readonly Regex YoutubeRegex = new(
        @"(?:youtube\.com\/(?:watch\?v=|embed\/|shorts\/)|youtu\.be\/)([a-zA-Z0-9_-]{11})",
        RegexOptions.Compiled);

    private static readonly Regex VimeoRegex = new(
        @"vimeo\.com\/(?:video\/)?(\d+)",
        RegexOptions.Compiled);

    public static (string? EmbedId, VideoProvider? Provider) Parse(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return (null, null);

        var ytMatch = YoutubeRegex.Match(url);
        if (ytMatch.Success)
            return (ytMatch.Groups[1].Value, VideoProvider.YouTube);

        var vimeoMatch = VimeoRegex.Match(url);
        if (vimeoMatch.Success)
            return (vimeoMatch.Groups[1].Value, VideoProvider.Vimeo);

        return (null, null);
    }

    public static string? GetEmbedUrl(string? embedId, VideoProvider? provider) => provider switch
    {
        VideoProvider.YouTube => $"https://www.youtube.com/embed/{embedId}?rel=0",
        VideoProvider.Vimeo => $"https://player.vimeo.com/video/{embedId}",
        _ => null
    };

    public static string? GetThumbnailUrl(string? embedId, VideoProvider? provider) => provider switch
    {
        VideoProvider.YouTube => $"https://img.youtube.com/vi/{embedId}/hqdefault.jpg",
        _ => null
    };
}

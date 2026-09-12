using System.Security.Cryptography;
using System.Text;

namespace CriticalListeningLab.Api.Data.Seed;

/// <summary>
/// Deterministicki Guid iz stabilnog kljuca. Ponovni seed ne stvara nove identitete.
/// </summary>
public static class SeedIds
{
    public static Guid For(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("cll.v1:" + key));
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash.AsSpan(0, 16));
    }

    public static Guid Module(string slug) => For($"module:{slug}");
    public static Guid Cohort(string name) => For($"cohort:{name}");
    public static Guid Source(string moduleSlug, string sourceSlug) => For($"source:{moduleSlug}:{sourceSlug}");
    public static Guid Asset(string moduleSlug, string sourceSlug, string variant) =>
        For($"asset:{moduleSlug}:{sourceSlug}:{variant}");
    public static Guid Segment(string moduleSlug, string key) => For($"segment:{moduleSlug}:{key}");
    public static Guid Level(string moduleSlug, string segmentKey, int number) =>
        For($"level:{moduleSlug}:{segmentKey}:{number}");
    public static Guid Unlock(string moduleSlug, string segmentKey, int number) =>
        For($"unlock:{moduleSlug}:{segmentKey}:{number}");
}

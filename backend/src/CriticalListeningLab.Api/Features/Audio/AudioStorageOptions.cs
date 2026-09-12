namespace CriticalListeningLab.Api.Features.Audio;

public class AudioStorageOptions
{
    public const string SectionName = "AudioStorage";

    public string Provider { get; set; } = "Local";

    public string LocalRoot { get; set; } = "wwwroot/audio";
}

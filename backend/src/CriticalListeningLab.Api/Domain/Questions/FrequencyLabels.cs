namespace CriticalListeningLab.Api.Domain.Questions;

public static class FrequencyLabels
{
    public static string Hz(int frequencyHz) => frequencyHz >= 1000
        ? $"{frequencyHz / 1000} kHz"
        : $"{frequencyHz} Hz";

    public static string Direction(string direction) => direction switch
    {
        "boost" => "pojačanje",
        "cut" => "smanjenje",
        _ => direction
    };

    public static string Combined(int frequencyHz, string direction) =>
        $"{Hz(frequencyHz)}, {Direction(direction)}";
}

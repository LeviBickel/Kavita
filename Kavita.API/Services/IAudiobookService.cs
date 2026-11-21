using Kavita.Models.Images;
using Kavita.Models.Parser;

namespace Kavita.API.Services;

public interface IAudiobookService
{
    /// <summary>
    /// Parses metadata from an audio file and returns a ParserInfo.
    /// Falls back to filename-based parsing if tag metadata is unavailable.
    /// </summary>
    ParserInfo? ParseInfo(string filePath);

    /// <summary>
    /// Returns the duration of the audio file in seconds.
    /// Stored in Chapter.Pages to reuse the existing progress tracking infrastructure.
    /// </summary>
    int GetDurationInSeconds(string filePath);

    /// <summary>
    /// Opens the audio file's cover image: embedded art if present, else cover/folder.jpg|jpeg|png in the file's
    /// directory or its parent. Null when neither is available
    /// </summary>
    OpenedCover? OpenCover(string filePath);
}

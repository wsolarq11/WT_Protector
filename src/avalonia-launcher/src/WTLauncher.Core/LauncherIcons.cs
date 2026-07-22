namespace WTLauncher.Core;

/// <summary>
/// Small inline icon set for common operations.
/// Each icon is defined as an SVG path data string for use with Avalonia's PathIcon.
/// </summary>
public static class LauncherIcons
{
    /// <summary>Play / launch arrow (triangle pointing right).</summary>
    public static string LaunchIcon => "M8,5 L18,12 L8,19 Z";

    /// <summary>Edit / pencil icon.</summary>
    public static string EditIcon => "M16.5,2.5 L18.5,4.5 L7,16 L4,16 L4,13 Z M20.5,2.5 C21.3,3.3 21.3,4.7 20.5,5.5 L19,7 L17,5 L18.5,3.5 C18.9,3.1 19.3,3 19.7,3 C20.1,3 20.5,3.1 20.5,2.5 Z";

    /// <summary>Delete / trash can icon.</summary>
    public static string DeleteIcon => "M6,5 L6,20 C6,21.1 6.9,22 8,22 L16,22 C17.1,22 18,21.1 18,20 L18,5 M10,2 L14,2 L15,3 L19,3 L19,5 L5,5 L5,3 L9,3 Z M9,9 L9,18 M12,9 L12,18 M15,9 L15,18";

    /// <summary>More / three vertical dots icon.</summary>
    public static string MoreIcon => "M12,8 C13.1,8 14,7.1 14,6 C14,4.9 13.1,4 12,4 C10.9,4 10,4.9 10,6 C10,7.1 10.9,8 12,8 Z M12,10 C10.9,10 10,10.9 10,12 C10,13.1 10.9,14 12,14 C13.1,14 14,13.1 14,12 C14,10.9 13.1,10 12,10 Z M12,16 C10.9,16 10,16.9 10,18 C10,19.1 10.9,20 12,20 C13.1,20 14,19.1 14,18 C14,16.9 13.1,16 12,16 Z";

    /// <summary>Search / magnifying glass icon.</summary>
    public static string SearchIcon => "M15.5,14 L20,18.5 L18.5,20 L14,15.5 C13,16.5 11.5,17 10,17 C6.5,17 3.5,14 3.5,10.5 C3.5,7 6.5,4 10,4 C13.5,4 16.5,7 16.5,10.5 C16.5,12 16,13.5 15.5,14 Z M10,5.5 C7.5,5.5 5.5,7.5 5.5,10 C5.5,12.5 7.5,14.5 10,14.5 C12.5,14.5 14.5,12.5 14.5,10 C14.5,7.5 12.5,5.5 10,5.5 Z";
}

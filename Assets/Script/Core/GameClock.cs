/// <summary>
/// The time shown in the game (laptop taskbar, phone status bar, café receipt), instead of the computer's clock.
/// Chapter 1 starts at 9:04 AM (the wake-up card), Chapter 2 at 11:40 PM (the "back home" card); one game minute
/// passes per real minute of play from the first time the clock is read in that chapter.
/// </summary>
public static class GameClock
{
    static int chapter = -1;
    static float start;

    public static string Now
    {
        get
        {
            int ch = GameProgressManager.Instance != null ? GameProgressManager.Instance.chapter : 1;
            if (ch != chapter) { chapter = ch; start = UnityEngine.Time.unscaledTime; }
            int minutes = (ch >= 2 ? 23 * 60 + 40 : 9 * 60 + 4) + (int)((UnityEngine.Time.unscaledTime - start) / 60f);
            minutes %= 24 * 60;
            int h = minutes / 60, m = minutes % 60;
            return $"{(h % 12 == 0 ? 12 : h % 12)}:{m:00} {(h < 12 ? "AM" : "PM")}";
        }
    }
}

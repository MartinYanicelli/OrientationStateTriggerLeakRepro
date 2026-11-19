namespace OrientationStateTriggerLeakRepro;

/// <summary>
/// Tracks every page instance through a weak reference, so we can count how many are still alive after a GC.
/// </summary>
public static class PageTracker
{
	public const string LogTag = "OSTLeak";

	static readonly List<(int Sequence, WeakReference<Page> Page)> pages = new();

	public static int Register(Page page)
	{
		lock (pages)
		{
			var sequence = pages.Count + 1;
			pages.Add((sequence, new WeakReference<Page>(page)));
			return sequence;
		}
	}

	public static (int Alive, int Created, string AliveSequences) Count()
	{
		lock (pages)
		{
			var alive = pages.Where(p => p.Page.TryGetTarget(out _)).Select(p => p.Sequence).ToList();
			return (alive.Count, pages.Count, string.Join(",", alive));
		}
	}

	public static void Log(string message)
	{
#if ANDROID
		Android.Util.Log.Info(LogTag, message);
#endif
		Console.WriteLine($"{LogTag}: {message}");
	}
}

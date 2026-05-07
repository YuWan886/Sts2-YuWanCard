using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Core.Abstracts;

public abstract class YuWanEventModel : EventModel, IYuWanContent
{
    public virtual ActModel[] Acts => [];

    public virtual string? CustomBackgroundScenePath => null;
    public virtual string? CustomVfxPath => null;

    protected virtual string? CustomEventImagePath => null;

    protected string ModResPath => AssetPathHelper.GetModResPathFromType(GetType());

    private string? _cachedImagePath;

    internal string? GetYuWanEventImagePath()
    {
        if (_cachedImagePath != null)
            return _cachedImagePath;

        if (CustomEventImagePath != null)
        {
            _cachedImagePath = CustomEventImagePath;
            return _cachedImagePath;
        }

        var modId = AssetPathHelper.GetModIdFromType(GetType());
        var prefix = $"{modId.ToUpperInvariant()}-";
        
        if (Id.Entry.StartsWith(prefix))
        {
            var fileName = Id.Entry
                .Replace(prefix, "")
                .ToLowerInvariant();
            _cachedImagePath = $"{ModResPath}/images/events/{fileName}.png";
            return _cachedImagePath;
        }

        return null;
    }

    public override IEnumerable<string> GetAssetPaths(IRunState runState)
    {
        var paths = base.GetAssetPaths(runState).ToList();

        var customImagePath = GetYuWanEventImagePath();
        if (customImagePath != null)
        {
            var defaultImagePath = ImageHelper.GetImagePath("events/" + Id.Entry.ToLowerInvariant() + ".png");
            var imageIndex = paths.IndexOf(defaultImagePath);
            if (imageIndex >= 0)
                paths[imageIndex] = customImagePath;
        }

        if (CustomBackgroundScenePath != null)
        {
            var defaultPath = SceneHelper.GetScenePath("events/background_scenes/" + Id.Entry.ToLowerInvariant());
            var index = paths.IndexOf(defaultPath);
            if (index >= 0)
                paths[index] = CustomBackgroundScenePath;
        }

        return paths;
    }

    protected EventOption Option(Func<Task>? onChosen, LocString title, LocString description,
        params IHoverTip[] tips)
    {
        return new EventOption(this, onChosen, title, description, Id.Entry, tips);
    }

    protected EventOption Option(Func<Task>? onChosen, string pageKey = EventModel._initialPageKey, params IHoverTip[] tips)
    {
        var clickMethod = onChosen?.Method;
        string name = clickMethod?.Name ?? "UNKNOWN";
        return new EventOption(this, onChosen, $"{Id.Entry}.pages.{pageKey}.options.{StringHelper.Slugify(name)}", tips);
    }

    protected EventOption Option(Func<Task>? onChosen, IEnumerable<IHoverTip> tips, string pageKey = EventModel._initialPageKey)
    {
        var clickMethod = onChosen?.Method;
        string name = clickMethod?.Name ?? "UNKNOWN";
        return new EventOption(this, onChosen, $"{Id.Entry}.pages.{pageKey}.options.{StringHelper.Slugify(name)}", tips);
    }

    protected LocString PageDescription(string pageKey)
    {
        return new LocString("events", $"{Id.Entry}.pages.{pageKey}.description");
    }
}

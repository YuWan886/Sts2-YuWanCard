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

    public virtual string? CustomInitialPortraitPath => null;
    public virtual string? CustomBackgroundScenePath => null;
    public virtual string? CustomVfxPath => null;

    public override IEnumerable<string> GetAssetPaths(IRunState runState)
    {
        var paths = base.GetAssetPaths(runState).ToList();

        if (CustomInitialPortraitPath != null)
        {
            var defaultPath = ImageHelper.GetImagePath("events/" + Id.Entry.ToLowerInvariant() + ".png");
            var index = paths.IndexOf(defaultPath);
            if (index >= 0)
                paths[index] = CustomInitialPortraitPath;
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

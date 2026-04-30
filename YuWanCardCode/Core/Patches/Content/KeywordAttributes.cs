namespace YuWanCard.Core.Patches.Content;

[AttributeUsage(AttributeTargets.Field)]
public class CustomEnumAttribute : Attribute
{
}

public enum AutoKeywordPosition
{
    Before,
    After
}

[AttributeUsage(AttributeTargets.Field)]
public class KeywordPropertiesAttribute : Attribute
{
    public AutoKeywordPosition Position { get; }

    public KeywordPropertiesAttribute(AutoKeywordPosition position = AutoKeywordPosition.After)
    {
        Position = position;
    }
}

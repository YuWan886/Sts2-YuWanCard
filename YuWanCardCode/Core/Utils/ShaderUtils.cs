using Godot;

namespace YuWanCard.Core.Utils;

public static class ShaderUtils
{
    private const string HsvShaderPath = "res://shaders/hsv.gdshader";

    private static Shader? _hsvShader;

    private static Shader? HsvShader => _hsvShader ??= (Shader?)GD.Load<Shader>(HsvShaderPath)?.Duplicate();

    public static ShaderMaterial GenerateHsv(float h, float s, float v)
    {
        var shader = HsvShader ?? throw new InvalidOperationException($"Failed to load HSV shader ({HsvShaderPath}).");

        var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("h", h);
        material.SetShaderParameter("s", s);
        material.SetShaderParameter("v", v);

        return material;
    }
}

using Godot;

namespace YuWanCard.Vfx;

public partial class VfxGlitchController : ColorRect
{
    private ShaderMaterial? _shaderMaterial;
    private double _elapsedTime;
    private double _duration = 1.5;
    private bool _isPlaying;

    public override void _Ready()
    {
        _shaderMaterial = Material as ShaderMaterial;
        if (_shaderMaterial == null)
        {
            MainFile.Logger.Error("VfxGlitchController: ShaderMaterial not found");
            QueueFree();
            return;
        }

        _shaderMaterial.SetShaderParameter("glitch_intensity", 0.0);
        _isPlaying = true;
        
        var timer = GetNode<Godot.Timer>("Timer");
        if (timer != null)
        {
            timer.Timeout += OnTimerTimeout;
        }
    }

    public override void _Process(double delta)
    {
        if (!_isPlaying || _shaderMaterial == null) return;

        _elapsedTime += delta;
        
        _shaderMaterial.SetShaderParameter("time", (float)_elapsedTime);
        
        float intensity = CalculateIntensity(_elapsedTime);
        _shaderMaterial.SetShaderParameter("glitch_intensity", intensity);
    }

    private float CalculateIntensity(double time)
    {
        double normalizedTime = time / _duration;
        
        if (normalizedTime < 0.1)
        {
            return (float)(normalizedTime / 0.1);
        }
        else if (normalizedTime < 0.3)
        {
            return 0.8f + 0.2f * Mathf.Sin((float)(time * 20.0));
        }
        else if (normalizedTime < 0.5)
        {
            return 0.6f + 0.4f * Mathf.Sin((float)(time * 15.0));
        }
        else if (normalizedTime < 0.7)
        {
            return 0.5f + 0.3f * Mathf.Sin((float)(time * 12.0));
        }
        else if (normalizedTime < 0.9)
        {
            return 0.3f + 0.2f * Mathf.Sin((float)(time * 10.0));
        }
        else
        {
            float fadeOut = 1.0f - (float)((normalizedTime - 0.9) / 0.1);
            return 0.2f * fadeOut;
        }
    }

    private void OnTimerTimeout()
    {
        _isPlaying = false;
        QueueFree();
    }

    public void SetDuration(double duration)
    {
        _duration = duration;
        var timer = GetNode<Godot.Timer>("Timer");
        if (timer != null)
        {
            timer.WaitTime = duration;
        }
    }
}

namespace FUEngine.Core;

/// <summary>
/// Avanza el frame actual de una animación según delta time.
/// </summary>
public class AnimationController
{
    private readonly AnimationDefinition _definition;
    private double _accumulatedTime;
    private int _currentFrameIndex;

    public AnimationController(AnimationDefinition definition)
    {
        _definition = definition;
    }

    public int CurrentFrameIndex => _currentFrameIndex;
    public string? CurrentFramePath => _definition.Frames.Count > 0 && _currentFrameIndex < _definition.Frames.Count
        ? _definition.Frames[_currentFrameIndex]
        : null;

    public void Update(double deltaSeconds)
    {
        if (_definition.Frames.Count == 0) return;
        _accumulatedTime += deltaSeconds;
        var frameDuration = 1.0 / Math.Max(1, _definition.Fps);
        while (_accumulatedTime >= frameDuration)
        {
            _accumulatedTime -= frameDuration;
            _currentFrameIndex = (_currentFrameIndex + 1) % _definition.Frames.Count;
        }
    }

    public void Reset()
    {
        _accumulatedTime = 0;
        _currentFrameIndex = 0;
    }
}

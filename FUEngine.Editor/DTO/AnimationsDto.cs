namespace FUEngine.Editor;

public class AnimationsDto
{
    public List<AnimationItemDto> Animations { get; set; } = new();
}

public class AnimationItemDto
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public List<string> Frames { get; set; } = new();
    public int Fps { get; set; } = 8;
}

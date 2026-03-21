using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FUEngine.Core;

namespace FUEngine.Editor;

/// <summary>Persistencia de un UICanvas en un archivo JSON (Scene/UI/CanvasId.json).</summary>
public static class UICanvasSerialization
{
    public static void Save(UICanvas canvas, string path)
    {
        var dto = ToDto(canvas);
        var json = JsonSerializer.Serialize(dto, SerializationDefaults.Options);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, json);
    }

    public static UICanvas Load(string path)
    {
        if (!File.Exists(path))
            return new UICanvas();
        var json = File.ReadAllText(path);
        var dto = JsonSerializer.Deserialize<UICanvasDto>(json, SerializationDefaults.Options);
        return dto != null ? FromDto(dto) : new UICanvas();
    }

    public static UICanvasDto ToDto(UICanvas canvas)
    {
        return new UICanvasDto
        {
            Id = canvas.Id,
            Name = canvas.Name,
            ResolutionWidth = canvas.ResolutionWidth,
            ResolutionHeight = canvas.ResolutionHeight,
            ZIndex = canvas.ZIndex,
            Children = canvas.Children.Select(ElementToDto).ToList()
        };
    }

    public static UICanvas FromDto(UICanvasDto dto)
    {
        var c = new UICanvas
        {
            Id = dto.Id ?? "",
            Name = dto.Name ?? "",
            ResolutionWidth = dto.ResolutionWidth > 0 ? dto.ResolutionWidth : 1920,
            ResolutionHeight = dto.ResolutionHeight > 0 ? dto.ResolutionHeight : 1080,
            ZIndex = dto.ZIndex
        };
        c.Children.AddRange((dto.Children ?? new List<UIElementDto>()).Select(ElementFromDto));
        return c;
    }

    private static UIElementDto ElementToDto(UIElement e)
    {
        return new UIElementDto
        {
            Id = e.Id,
            Kind = (int)e.Kind,
            X = e.Rect.X,
            Y = e.Rect.Y,
            Width = e.Rect.Width,
            Height = e.Rect.Height,
            AnchorMinX = e.Anchors.MinX,
            AnchorMinY = e.Anchors.MinY,
            AnchorMaxX = e.Anchors.MaxX,
            AnchorMaxY = e.Anchors.MaxY,
            Text = e.Text,
            ImagePath = e.ImagePath,
            SeedId = e.SeedId,
            PropertyOverrides = e.PropertyOverrides.Count > 0 ? new Dictionary<string, string>(e.PropertyOverrides) : null,
            BlocksInput = e.BlocksInput,
            Children = e.Children.Select(ElementToDto).ToList()
        };
    }

    private static UIElement ElementFromDto(UIElementDto d)
    {
        var e = new UIElement
        {
            Id = d.Id ?? "",
            Kind = d.Kind >= 0 && d.Kind <= (int)UIElementKind.Panel ? (UIElementKind)d.Kind : UIElementKind.Panel,
            Rect = new UIRect { X = d.X, Y = d.Y, Width = d.Width, Height = d.Height },
            Anchors = new UIAnchors { MinX = d.AnchorMinX, MinY = d.AnchorMinY, MaxX = d.AnchorMaxX, MaxY = d.AnchorMaxY },
            Text = d.Text ?? "",
            ImagePath = d.ImagePath ?? "",
            SeedId = d.SeedId ?? "",
            BlocksInput = d.BlocksInput
        };
        if (d.PropertyOverrides != null)
            e.PropertyOverrides = new Dictionary<string, string>(d.PropertyOverrides);
        e.Children.AddRange((d.Children ?? new List<UIElementDto>()).Select(ElementFromDto));
        return e;
    }

    public class UICanvasDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public int ResolutionWidth { get; set; }
        public int ResolutionHeight { get; set; }
        public int ZIndex { get; set; }
        public List<UIElementDto>? Children { get; set; }
    }

    public class UIElementDto
    {
        public string? Id { get; set; }
        public int Kind { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double AnchorMinX { get; set; }
        public double AnchorMinY { get; set; }
        public double AnchorMaxX { get; set; }
        public double AnchorMaxY { get; set; }
        public string? Text { get; set; }
        public string? ImagePath { get; set; }
        public string? SeedId { get; set; }
        public Dictionary<string, string>? PropertyOverrides { get; set; }
        public bool BlocksInput { get; set; } = true;
        public List<UIElementDto>? Children { get; set; }
    }
}

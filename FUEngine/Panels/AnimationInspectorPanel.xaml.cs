using System.Windows.Controls;
using FUEngine.Core;

namespace FUEngine;

public partial class AnimationInspectorPanel : System.Windows.Controls.UserControl
{
    public AnimationInspectorPanel()
    {
        InitializeComponent();
    }

    public void SetAnimation(AnimationDefinition? anim)
    {
        if (anim == null)
        {
            TxtAnimName.Text = "—";
            TxtFps.Text = "—";
            TxtFrames.Text = "—";
            return;
        }
        TxtAnimName.Text = anim.Nombre ?? anim.Id;
        TxtFps.Text = anim.Fps > 0 ? anim.Fps.ToString() : "—";
        TxtFrames.Text = anim.Frames != null && anim.Frames.Count > 0 ? string.Join(", ", anim.Frames) : "—";
    }
}

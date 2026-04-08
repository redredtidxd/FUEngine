using FUEngine.Core;
using FUEngine.Rendering;
using FUEngine.Runtime;

namespace FUEngine;

/// <summary>Estado de máquina de escribir por elemento UI (no serializado).</summary>
public sealed class UiTypewriterRuntime
{
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);

    private sealed class Entry
    {
        public string SnapshotKey = "";
        public int VisiblePlainCount;
        public double CharCarry;
        public double PauseRemain;
        public readonly List<double> RevealTimes = new();
    }

    public void Clear() => _entries.Clear();

    /// <summary>canvasId, elemento, texto plano (sin tags) cuando el typewriter acaba de mostrar todo el texto.</summary>
    public event Action<string, UIElement, string>? TypewriterLineComplete;

    /// <summary>Avanza revelación de caracteres y pausas. Llamar una vez por frame de simulación.</summary>
    public void Tick(double deltaTime, UIRoot? root, UIRuntimeBackend? ui, double gameTimeSeconds, PlayNaudioAudioEngine? audio,
        string projectRoot, LocalizationRuntime? localization)
    {
        if (root == null || ui == null || deltaTime <= 0) return;
        var alive = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var canvas in ui.GetVisibleCanvasesOrdered())
            Walk(canvas.Children, canvas.Id, alive, deltaTime, gameTimeSeconds, audio, projectRoot, localization);
        foreach (var key in _entries.Keys.ToList())
        {
            if (!alive.Contains(key))
                _entries.Remove(key);
        }
    }

    private void Walk(List<UIElement> nodes, string canvasId, HashSet<string> alive, double dt, double gameTime, PlayNaudioAudioEngine? audio,
        string projectRoot, LocalizationRuntime? localization)
    {
        foreach (var e in nodes)
        {
            Walk(e.Children, canvasId, alive, dt, gameTime, audio, projectRoot, localization);
            if (e.Kind is not (UIElementKind.Text or UIElementKind.Button)) continue;

            var resolved = UiTextResolve.Resolve(e, projectRoot, localization);
            var tw = resolved.Typewriter;
            if (tw == null || !tw.Enabled || string.IsNullOrWhiteSpace(e.Id)) continue;

            var key = canvasId + "|" + e.Id;
            alive.Add(key);
            if (!_entries.TryGetValue(key, out var st))
            {
                st = new Entry();
                _entries[key] = st;
            }

            var snap = UiTextResolve.TypewriterSnapshotKey(e, localization);
            if (!string.Equals(st.SnapshotKey, snap, StringComparison.Ordinal))
            {
                st.SnapshotKey = snap;
                st.VisiblePlainCount = 0;
                st.CharCarry = 0;
                st.PauseRemain = 0;
                st.RevealTimes.Clear();
            }

            var plain = UiRichText.StripTags(resolved.DisplayText);
            var target = plain.Length;
            if (st.VisiblePlainCount >= target) continue;

            var beforeCount = st.VisiblePlainCount;

            if (st.PauseRemain > 0)
            {
                st.PauseRemain -= dt;
                if (st.PauseRemain > 0)
                    continue;
            }

            var cps = tw.CharsPerSecond <= 0 ? 8 : tw.CharsPerSecond;
            st.CharCarry += dt * cps;
            while (st.CharCarry >= 1 && st.VisiblePlainCount < target)
            {
                st.CharCarry -= 1;
                st.VisiblePlainCount++;
                var ch = plain[st.VisiblePlainCount - 1];
                st.RevealTimes.Add(gameTime);

                if (!string.IsNullOrWhiteSpace(tw.SoundPath))
                {
                    var play = tw.SoundTrigger == UITypewriterSoundTrigger.EachCharacter ||
                               (tw.SoundTrigger == UITypewriterSoundTrigger.SpacesOnly && char.IsWhiteSpace(ch));
                    if (play)
                        audio?.PlaySfxFromProjectRelativePath(tw.SoundPath, (float)tw.SoundVolume);
                }

                if (tw.PunctuationPausesEnabled)
                    st.PauseRemain += PauseForChar(ch, tw);
            }

            if (beforeCount < target && st.VisiblePlainCount >= target)
                TypewriterLineComplete?.Invoke(canvasId, e, plain);
        }
    }

    private static double PauseForChar(char ch, UITypewriterSettings tw) =>
        ch switch
        {
            ',' => tw.PauseAfterCommaSeconds,
            '.' => tw.PauseAfterPeriodSeconds,
            '?' => tw.PauseAfterQuestionSeconds,
            '!' => tw.PauseAfterExclamationSeconds,
            _ => 0
        };

    public int GetVisiblePlainLength(string canvasId, UIElement element, string projectRoot, LocalizationRuntime? localization)
    {
        if (element.Kind is not (UIElementKind.Text or UIElementKind.Button)) return int.MaxValue;
        var resolved = UiTextResolve.Resolve(element, projectRoot, localization);
        var tw = resolved.Typewriter;
        if (tw is not { Enabled: true }) return int.MaxValue;
        if (string.IsNullOrWhiteSpace(element.Id)) return int.MaxValue;
        var key = canvasId + "|" + element.Id;
        if (!_entries.TryGetValue(key, out var st)) return 0;
        var snap = UiTextResolve.TypewriterSnapshotKey(element, localization);
        if (!string.Equals(st.SnapshotKey, snap, StringComparison.Ordinal)) return 0;
        return st.VisiblePlainCount;
    }

    public IReadOnlyList<double>? GetCharRevealTimes(string canvasId, UIElement element, string projectRoot, LocalizationRuntime? localization)
    {
        if (element.Kind is not (UIElementKind.Text or UIElementKind.Button)) return null;
        var resolved = UiTextResolve.Resolve(element, projectRoot, localization);
        var tw = resolved.Typewriter;
        if (tw is not { Enabled: true, FadeInPerChar: true }) return null;
        if (string.IsNullOrWhiteSpace(element.Id)) return null;
        var key = canvasId + "|" + element.Id;
        if (!_entries.TryGetValue(key, out var st)) return null;
        var snap = UiTextResolve.TypewriterSnapshotKey(element, localization);
        if (!string.Equals(st.SnapshotKey, snap, StringComparison.Ordinal)) return null;
        return st.RevealTimes;
    }
}

using System.Collections.Generic;
using Godot;

public partial class Soundscape : Node
{
    public static Soundscape Current { get; private set; }

    private const string Samples = "res://scripts/audio/samples/";
    private readonly Dictionary<string, AudioStreamWav> _sounds = new();
    private readonly AudioStreamPlayer3D[] _voices = new AudioStreamPlayer3D[12];
    private readonly RandomNumberGenerator _rng = new() { Seed = 78971 };
    private AudioStreamPlayer _ambience;
    private bool _quiet;
    private AudioStreamPlayer3D _fire = null!;
    private AudioStreamPlayer3D _portal = null!;
    private int _voice;
    private Vector3? _portalPosition;

    public override void _Ready()
    {
        Current = this;
        foreach (string name in new[] { "step", "attack", "impact", "dodge", "fire", "portal", "ambient" })
        {
            var clip = GD.Load<AudioStreamWav>(Samples + name + ".wav");
            if (name is "fire" or "portal" or "ambient")
            {
                clip.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
                clip.LoopEnd = (int)System.Math.Round(clip.GetLength() * clip.MixRate);
            }
            _sounds.Add(name, clip);
        }

        for (int i = 0; i < _voices.Length; i++)
            _voices[i] = AddVoice("Foley" + i);
        _ambience = new AudioStreamPlayer { Name = "CathedralWind", Stream = _sounds["ambient"], VolumeDb = -22 };
        AddChild(_ambience);
        _ambience.Play();
        _fire = AddVoice("BladeFire");
        _fire.Stream = _sounds["fire"];
        _fire.VolumeDb = -23;
        _portal = AddVoice("GateResonance");
        _portal.Stream = _sounds["portal"];
        _portal.VolumeDb = -20;
        if (_portalPosition.HasValue)
            SetPortal(_portalPosition.Value);
    }

    public override void _ExitTree()
    {
        if (Current == this)
            Current = null;
    }

    public void Stop()
    {
        _quiet = true;
        _ambience.Stop();
        _fire.Stop();
        _portal.Stop();
        foreach (var voice in _voices) voice.Stop();
    }

    public void SetFire(Vector3 position)
    {
        if (_quiet) return;
        _fire.GlobalPosition = position;
        if (!_fire.Playing)
            _fire.Play();
    }

    public void SetPortal(Vector3 position)
    {
        _portalPosition = position;
        if (_portal == null)
            return;
        _portal.GlobalPosition = position;
        if (!_portal.Playing)
            _portal.Play();
    }

    public void PlayEffect(string name, Vector3 position, float volumeDb = -7)
    {
        if (_quiet || !_sounds.TryGetValue(name, out var clip))
            return;
        var player = _voices[_voice];
        _voice = (_voice + 1) % _voices.Length;
        player.Stream = clip;
        player.GlobalPosition = position;
        player.PitchScale = _rng.RandfRange(.89f, 1.11f);
        player.VolumeDb = volumeDb;
        player.Play();
    }

    private AudioStreamPlayer3D AddVoice(string name)
    {
        var voice = new AudioStreamPlayer3D { Name = name, UnitSize = 16, MaxDistance = 100 };
        AddChild(voice);
        return voice;
    }
}

using Godot;
using System;
using System.IO;
using System.Text.Json;

public class ArtProfile
{
    public int Mode { get; set; } = 5;
    public float Cell { get; set; } = 9;
    public float Density { get; set; } = 1;
    public float Contrast { get; set; } = 1;
    public float Brightness { get; set; } = 1;
    public float Saturation { get; set; } = 1;
    public float Exposure { get; set; } = 1;
    public float Edges { get; set; } = 1;
    public float Stability { get; set; } = .8f;
    public float Glow { get; set; } = .55f;
    public float Fog { get; set; } = 1;
    public float Particles { get; set; } = 1;
    public float Light { get; set; } = 1;
    public int Palette { get; set; }
    public int GlyphSet { get; set; }
    public int Quality { get; set; } = 3;
    public float Scale => new[]{.60f,.75f,.9f,1f}[Math.Clamp(Quality,0,3)];
    public static ArtProfile Preset(int index) => index switch {
        0 => new() {Quality=0, Cell=12, Particles=.35f, Glow=.25f, Edges=.8f},
        1 => new() {Quality=1, Cell=10, Particles=.6f, Glow=.55f},
        2 => new() {Quality=2, Cell=8, Particles=.85f, Glow=.7f},
        _ => new()
    };

    public void ClampValues()
    {
        Mode=Math.Clamp(Mode,0,5); Quality=Math.Clamp(Quality,0,3);
        Palette=Math.Clamp(Palette,0,2); GlyphSet=Math.Clamp(GlyphSet,0,6);
        Cell=Math.Clamp(Cell,4,16); Density=Math.Clamp(Density,.25f,2);
        Contrast=Math.Clamp(Contrast,.25f,2); Brightness=Math.Clamp(Brightness,.25f,2);
        Saturation=Math.Clamp(Saturation,0,2); Exposure=Math.Clamp(Exposure,.25f,2);
        Edges=Math.Clamp(Edges,0,2); Stability=Math.Clamp(Stability,0,1);
        Glow=Math.Clamp(Glow,0,2); Fog=Math.Clamp(Fog,0,2);
        Particles=Math.Clamp(Particles,0,2); Light=Math.Clamp(Light,.25f,2);
    }

    static string SlotPath(int slot) => ProjectSettings.GlobalizePath($"user://profile-{slot}.json");
    public string Save(int slot)
    {
        string target=SlotPath(slot), temp=target+".tmp";
        try {
            File.WriteAllText(temp, JsonSerializer.Serialize(this, new JsonSerializerOptions{WriteIndented=true}));
            File.Move(temp,target,true);
            return "Perfil guardado";
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {return "Error al guardar el perfil";}
    }
    public static ArtProfile Load(int slot)
    {
        try {
            var profile=JsonSerializer.Deserialize<ArtProfile>(File.ReadAllText(SlotPath(slot)));
            profile?.ClampValues();
            return profile;
        } catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) {return null;}
    }
}

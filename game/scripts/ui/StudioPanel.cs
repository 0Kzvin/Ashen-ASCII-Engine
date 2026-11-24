using Godot;
using System;
using static InterfaceStyle;

public partial class StudioPanel : PanelContainer
{
    public Main Game;
    VBoxContainer form;
    Label notice;
    int slot;
    bool filling;
    public override void _Ready()
    {
        Theme=InterfaceStyle.Create();
        SetAnchorsAndOffsetsPreset(LayoutPreset.RightWide);
        OffsetLeft=-372; OffsetRight=-16; OffsetTop=16; OffsetBottom=-16;
        AddThemeStyleboxOverride("panel",Box("#10191af7","#687065",16));
        var root=new VBoxContainer(); AddChild(root);
        var head=new HBoxContainer();root.AddChild(head);
        var heading=InterfaceStyle.Label("ASCII Studio",34,true);
        heading.SizeFlagsHorizontal=SizeFlags.ExpandFill;head.AddChild(heading);
        var close=new Button{Text="×",TooltipText="Cerrar (F1)"};
        close.Pressed+=Game.ToggleStudio;head.AddChild(close);
        root.AddChild(new HSeparator());
        var scroll=new ScrollContainer{SizeFlagsVertical=SizeFlags.ExpandFill,HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};
        root.AddChild(scroll);
        form=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};scroll.AddChild(form);
        form.AddThemeConstantOverride("separation",6);
        root.AddChild(new HSeparator());
        var bar=new HBoxContainer();root.AddChild(bar);
        var slots=new OptionButton();foreach(string option in new[]{"Perfil I","Perfil II","Perfil III"})slots.AddItem(option);
        slots.ItemSelected+=i=>slot=(int)i;bar.AddChild(slots);
        var save=new Button {Text="Guardar",SizeFlagsHorizontal=SizeFlags.ExpandFill};
        save.Pressed+=()=>notice.Text=Game.Profile.Save(slot);bar.AddChild(save);
        var load=new Button {Text="Cargar",SizeFlagsHorizontal=SizeFlags.ExpandFill};
        load.Pressed+=()=>{
            var stored=ArtProfile.Load(slot);
            if(stored==null){notice.Text="Sin perfil válido en esta ranura";return;}
            Game.SetProfile(stored);Rebuild();notice.Text="Perfil cargado";
        };bar.AddChild(load);
        notice=InterfaceStyle.Label("Perfiles locales",13);notice.Modulate=Muted;root.AddChild(notice);
        Rebuild();
    }
    void Section(string name)
    {
        var label=InterfaceStyle.Label(name,14);label.Modulate=Gold;form.AddChild(label);
    }
    void Select(string name,string[] options,int index,Action<int> set)
    {
        var row=new HBoxContainer();form.AddChild(row);
        var label=InterfaceStyle.Label(name);label.SizeFlagsHorizontal=SizeFlags.ExpandFill;row.AddChild(label);
        var option=new OptionButton();foreach(string item in options) option.AddItem(item);
        option.Select(index);option.ItemSelected+=i=>{if(!filling)set((int)i);};row.AddChild(option);
    }
    void Range(string name,float value,float min,float max,Action<float> set,float step=.05f)
    {
        var row=new HBoxContainer();form.AddChild(row);
        var label=InterfaceStyle.Label(name,15);label.SizeFlagsHorizontal=SizeFlags.ExpandFill;row.AddChild(label);
        var slider=new HSlider{MinValue=min,MaxValue=max,Step=step,Value=value,CustomMinimumSize=new Vector2(118,24),SizeFlagsVertical=SizeFlags.ShrinkCenter};
        row.AddChild(slider);
        var number=InterfaceStyle.Label(value.ToString("0.00"),13);
        number.Modulate=Gold;number.CustomMinimumSize=new Vector2(32,0);row.AddChild(number);
        slider.ValueChanged+=v=>{if(filling)return;number.Text=v.ToString("0.00");set((float)v);Game.ApplyProfile();};
    }
    public void Rebuild()
    {
        filling=true;
        foreach(Node child in form.GetChildren()){form.RemoveChild(child);child.QueueFree();}
        var p=Game.Profile;
        Select("Calidad",new[]{"Bajo","Medio","Alto","Cinematográfico"},p.Quality,i=>{Game.SetProfile(ArtProfile.Preset(i));Rebuild();});
        Select("Modo",GameHud.Modes,p.Mode,i=>{p.Mode=i;Game.ApplyProfile();});
        Select("Paleta",new[]{"Ceniza","Lunar","Cobre"},p.Palette,i=>{p.Palette=i;Game.ApplyProfile();});
        Select("Glifos",new[]{"Clásico","Rúnico","Piedra","Follaje","Agua","Metal","Brasas"},p.GlyphSet,i=>{p.GlyphSet=i;Game.ApplyProfile();});
        Section("TRAMA");
        Range("Celda (px)",p.Cell,4,16,v=>p.Cell=v,1);
        Range("Densidad",p.Density,.25f,2,v=>p.Density=v);
        Range("Bordes",p.Edges,0,2,v=>p.Edges=v);
        Range("Estabilidad",p.Stability,0,1,v=>p.Stability=v);
        Section("COLOR Y LUZ");
        Range("Contraste",p.Contrast,.25f,2,v=>p.Contrast=v);
        Range("Brillo",p.Brightness,.25f,2,v=>p.Brightness=v);
        Range("Saturación",p.Saturation,0,2,v=>p.Saturation=v);
        Range("Exposición",p.Exposure,.25f,2,v=>p.Exposure=v);
        Range("Luz ambiental",p.Light,.25f,2,v=>p.Light=v);
        Range("Resplandor",p.Glow,0,2,v=>p.Glow=v);
        Section("ATMÓSFERA");
        Range("Niebla",p.Fog,0,2,v=>p.Fog=v);
        Range("Partículas",p.Particles,0,2,v=>p.Particles=v);
        filling=false;
    }
}

using Godot;
using static ArmourGeometry;

internal sealed class KnightRig
{
    private readonly bool _enemy;
    private readonly Node3D _root, _body, _head, _cape;
    private readonly Node3D[] _hips = new Node3D[2], _knees = new Node3D[2];
    private readonly Node3D[] _shoulders = new Node3D[2], _elbows = new Node3D[2], _hands = new Node3D[2];
    private readonly Node3D _sword;
    internal readonly Node3D BladeTip, BladeHeel;
    private readonly OmniLight3D _light;
    private readonly StandardMaterial3D _steel, _silver, _dark, _leather, _fabric, _hem, _gold, _ember, _core;

    internal KnightRig(Knight parent, bool enemy)
    {
        _enemy = enemy;
        _steel = Material(enemy ? "665657" : "929fae", .34f, .36f);
        _silver = Material(enemy ? "baa799" : "dce8f0", .34f, .28f);
        _dark = Material("24272f", .25f, .58f);
        _leather = Material("50392f");
        _fabric = Material(enemy ? "453449" : "ad233e");
        _hem = Material(enemy ? "98604c" : "e14d54");
        _gold = Material("dab071", .45f, .42f);
        _ember = Material(enemy ? "ff462b" : "ff9b29", .05f, .32f, 1.05f);
        _core = Material(enemy ? "ffa353" : "fff0ac", 0, .34f, 1.15f);
        _root = Joint(parent, "KnightModel", Vector3.Zero);
        _body = Joint(_root, "Trunk", new(0, 1.07f, 0));
        Shell(_body, "ArmingDoublet", Vector3.Zero, _dark, 8,
            new(.21f,-.09f,.17f), new(.25f,.24f,.18f), new(.33f,.54f,.20f), new(.20f,.66f,.17f));
        Shell(_body, "PeascodCuirass", Vector3.Zero, _steel, 10, new(.20f,.09f,.16f),
            new(.27f,.26f,.21f), new(.345f,.49f,.236f), new(.295f,.59f,.21f), new(.18f,.65f,.15f));
        Plate(_body, "BreastRidge", new(0,.38f,-.19f), _silver,.064f, new(0,.075f),
            new(-.24f,.15f), new(0,.22f), new(.24f,.15f), new(.16f,-.075f), new(0,-.23f), new(-.16f,-.075f));
        for (int i=0; i<3; i++)
            Shell(_body, "Fauld"+i, new(0,.09f-i*.073f,0), i==0 ? _dark : _steel, 8,
                new(.275f,-.055f,.202f), new(.255f,.019f,.189f), new(.22f,.05f,.16f));
        Box(_body,"BeltBuckle",new(.113f,.074f,.032f),new(0,.055f,-.195f),_gold);
        Plate(_body,"SplitSurcoat",new(0,-.10f,-.207f),_fabric,.04f,new(0,-.16f),
            new(-.18f,.02f),new(.18f,.02f),new(.15f,-.395f),new(.045f,-.48f),new(-.17f,-.40f));
        _cape = Joint(_body,"Mantle",new(0,.55f,.153f));
        for (int i=0; i<3; i++)
        {
            var fold = Plate(_cape,"WoolFold"+i,new((i-1)*.155f,0,.025f),i==1?_hem:_fabric,.064f,
                new(0,-.35f), new(-.13f,.01f),new(.13f,.01f),new(.13f,-.66f),
                new(.08f,-.85f+(i%2)*.06f),new(-.14f,-.78f));
            fold.Rotation = new(-.19f, Mathf.Pi, .065f*(i-1));
        }
        _head = Joint(_body,"Helmet",new(0,.695f,-.013f));
        Shell(_head,"Gorget",Vector3.Zero,_dark,8,new(.17f,-.09f,.15f),new(.16f,.10f,.16f));
        Shell(_head,"HelmetSkull",new(0,.02f,0),_steel,10,new(.145f,-.015f,.16f),
            new(.205f,.089f,.192f),new(.211f,.232f,.201f), new(.15f,.33f,.165f),new(.051f,.385f,.05f));
        Plate(_head,"Visor",new(0,.134f,-.138f),_silver,.111f,new(0,.004f),
            new(-.186f,.065f),new(0,.115f),new(.186f,.065f),new(.143f,-.073f),new(0,-.145f),new(-.143f,-.073f));
        for(int i=0; i<2; i++)
        {
            float sign=i==0?-1:1;
            var eye=Box(_head,"Sight"+i,new(.161f,.032f,.016f),new(sign*.087f,.184f,-.214f),_dark);
            eye.Rotation=new(0,sign*-.39f,sign*.058f);
            var slit=Box(eye,"EmberSlit",new(.112f,.01f,.005f),new(0,0,-.011f),_ember);
        }
        if (enemy)
        {
            for(int i=0;i<2;i++)
            {
                float sign=i==0?-1:1;
                var barb=Shell(_head,"CrownHorn"+i,new(sign*.173f,.19f,.025f),_gold,5,
                    new(.075f,0,.092f),new(.07f,.14f,.07f),new(.009f,.32f,.008f));
                barb.Rotation=new(.24f,0,sign*-.42f);
            }
        }
        else
        {
            var plume=Plate(_head,"Crest",new(0,.34f,.039f),_fabric,.056f,new(.04f,.03f),
                new(-.13f,.025f),new(-.04f,.114f),new(.15f,.08f),new(.37f,-.13f),new(.21f,-.135f),new(.02f,-.035f));
            plume.Rotation=new(0,-Mathf.Pi/2,0);
        }
        for(int i=0;i<2;i++) BuildLimbs(i);
        var shield=Joint(_hands[0],"VotiveShield",new(-.035f,.043f,-.073f));
        Vector2[] shape={new(-.219f,.245f),new(0,.337f),new(.219f,.245f),new(.194f,-.072f),new(0,-.37f),new(-.194f,-.072f)};
        Plate(shield,"Rim",Vector3.Zero,_gold,.085f,new(0,.025f),shape);
        var face=Plate(shield,"Enamel",new(0,0,-.022f),enemy?_dark:_fabric,.08f,new(0,.025f),shape);
        face.Scale=new(.91f,.91f,1);
        Plate(shield,"Emblem",new(0,.014f,-.101f),_gold,.012f,new(0,0),
            new(0,.177f), new(.043f,.014f),new(0,-.152f),new(-.043f,.014f));
        _sword=Joint(_body,"Oathblade",Vector3.Zero);
        Shell(_sword,"LeatherGrip",Vector3.Zero,_leather,8,new(.036f,-.13f,.033f),new(.038f,.127f,.036f));
        Shell(_sword,"Pommel",new(0,-.13f,0),_gold,6,new(.025f,-.075f,.025f),new(.063f,-.01f,.050f),new(.031f,.04f,.035f));
        Plate(_sword,"WingedCrossguard",new(0,.13f,0),_gold,.053f,new(0,.018f),
            new(-.285f,.097f),new(-.215f,.01f),new(0,-.044f),new(.22f,.01f),new(.285f,.097f),new(0,.048f));
        Plate(_sword,"FlameEdge",new(0,.161f,0),_ember,.043f,new(0,.30f),new(-.064f,0),new(-.092f,.15f),
            new(-.065f,.83f),new(0,1.19f),new(.065f,.83f),new(.092f,.15f),new(.064f,0));
        var fuller=Plate(_sword,"IncandescentRidge",new(0,.18f,-.031f),_core,.021f,new(0,.23f),
            new(-.029f,.04f),new(-.023f,.80f),new(0,1.12f),new(.023f,.8f),new(.029f,.04f));
        // The bright spine also reads when the weapon is viewed from behind.
        var rear=(MeshInstance3D)fuller.Duplicate();
        rear.Name="ReverseRidge"; rear.Position=new(0,.18f,.013f); rear.Rotation=new(0,Mathf.Pi,0); _sword.AddChild(rear);
        BladeHeel=Joint(_sword,"Heel",new(0,.235f,0)); BladeTip=Joint(_sword,"Tip",new(0,1.33f,0));
        _light=new OmniLight3D{ Name="BladeGlow",Position=new(0,.72f,0),LightColor=enemy?new("ff4d26"):new("ffad47"),
            LightEnergy=.72f,OmniRange=2.6f,ShadowEnabled=false };
        _sword.AddChild(_light);
        Pose(0,0,0,-1,false,0,0,0);
    }

    private void BuildLimbs(int i)
    {
        float side=i==0?-1:1;
        _hips[i]=Joint(_root,"Hip"+i,new(side*.147f,.97f,0));
        Shell(_hips[i],"Cuisses",Vector3.Zero,_steel,7,new(.108f,-.39f,.119f),new(.137f,-.12f,.139f),new(.12f,0,.116f));
        _knees[i]=Joint(_hips[i],"Knee",new(0,-.41f,0));
        Shell(_knees[i],"Greave",Vector3.Zero,_steel,7,new(.080f,-.422f,.09f),new(.116f,-.17f,.128f),new(.116f,-.04f,.11f));
        Plate(_knees[i],"Poleyn",new(0,-.014f,-.09f),_silver,.060f,new(0,.015f),
            new(-.121f,.02f),new(0,.098f),new(.121f,.02f),new(.075f,-.076f),new(0,-.108f),new(-.075f,-.076f));
        Shell(_knees[i],"Sabaton",new(0,-.44f,-.057f),_dark,7,new(.117f,-.085f,.192f),new(.119f,-.021f,.188f),new(.09f,.06f,.111f));
        _shoulders[i]=Joint(_body,"Shoulder"+i,new(side*.331f,.535f,0));
        Shell(_shoulders[i],"UpperArm",Vector3.Zero,_dark,7,new(.076f,-.305f,.082f),new(.103f,-.055f,.1f),new(.09f,.01f,.09f));
        for(int j=0;j<2;j++)
            Shell(_shoulders[i],"Pauldron"+j,new(side*.015f,-j*.074f,0),j==0?_silver:_steel,8,
                new(.157f,-.085f,.17f),new(.154f,-.017f,.169f),new(.115f,.062f,.128f),new(.047f,.094f,.051f));
        _elbows[i]=Joint(_body,"Elbow"+i,Vector3.Zero);
        Shell(_elbows[i],"Vambrace",Vector3.Zero,_steel,7,new(.078f,-.246f,.078f),new(.106f,-.06f,.096f),new(.091f,.025f,.089f));
        _hands[i]=Joint(_body,"Hand"+i,Vector3.Zero);
        Shell(_hands[i],"Gauntlet",Vector3.Zero,_dark,7,new(.071f,-.084f,.076f),new(.087f,.006f,.085f),new(.076f,.074f,.068f));
    }

    private static void AimArm(Node3D joint, Vector3 from, Vector3 to)
    {
        joint.Position=from;
        joint.Quaternion=new Quaternion(Vector3.Down,(to-from).Normalized());
    }

    internal void Pose(float clock, float phase, float stride, float strike, bool reverse, float roll, float hurt, float collapse)
    {
        float breath=Mathf.Sin(clock*2.5f);
        float gait=Mathf.Sin(phase);
        float swing=strike<0 ? 0 : Mathf.Sin(Mathf.Pi*strike);
        _root.Rotation=new(-.09f*stride+roll*Mathf.Tau+collapse*.96f,0,-hurt*.095f+collapse*.42f);
        _root.Position=new(0,-Mathf.Sin(Mathf.Pi*roll)*.29f-collapse*.67f,Mathf.Sin(Mathf.Pi*roll)*.14f);
        _body.Position=new(0,1.07f+breath*.009f+Mathf.Abs(Mathf.Cos(phase))*.042f*stride,0);
        _body.Rotation=new(-stride*.115f+hurt*.17f,gait*.075f*stride+swing*(reverse?.30f:-.30f),gait*.038f*stride);
        _head.Rotation=new(.015f*breath-hurt*.17f,-_body.Rotation.Y*.29f,0);
        _cape.Rotation=new(.055f*Mathf.Sin(clock*4.2f)+stride*.19f+swing*.16f,0,.028f*Mathf.Sin(clock*2.4f));
        for(int i=0;i<2;i++)
        {
            float leg=gait*(i==0?1:-1);
            _hips[i].Rotation=new(leg*.63f*stride-roll*.66f,0,(i==0?-1:1)*.04f);
            _knees[i].Rotation=new(Mathf.Max(0,-leg)*.69f*stride+roll*.83f+.035f,0,0);
        }
        Vector3 hand=new(.447f,.04f+gait*.06f*stride,-.223f);
        Vector3 direction=new(.58f,.65f,-.36f);
        if(strike>=0)
        {
            float wind=Mathf.SmoothStep(0,.28f,strike), slash=Mathf.SmoothStep(.28f,.66f,strike);
            float recover=Mathf.SmoothStep(.71f,1,strike);
            float sign=reverse?-1:1;
            float angle=sign*Mathf.Lerp(1.72f,-1.33f,slash);
            Vector3 pose=new(.27f+.30f*Mathf.Sin(angle),.27f+.17f*Mathf.Cos(strike*Mathf.Pi),-.27f-.21f*Mathf.Cos(angle));
            float weight=wind*(1-recover);
            hand=hand.Lerp(pose,weight);
            direction=direction.Lerp(new(Mathf.Sin(angle),.47f-.84f*slash,-Mathf.Cos(angle)),weight);
        }
        Vector3 left=new(-.43f,.075f-gait*.042f*stride,-.212f-.052f*swing);
        for(int i=0;i<2;i++)
        {
            float side=i==0?-1:1;
            Vector3 shoulder=new(side*.331f,.535f,0), palm=i==0?left:hand;
            Vector3 line=palm-shoulder;
            float reach=Mathf.Min(line.Length(),.556f);
            Vector3 axis=line.Normalized(), bend=new(side*.58f,-.70f,.75f);
            bend=(bend-axis*bend.Dot(axis)).Normalized();
            float along=(.302f*.302f-.262f*.262f+reach*reach)/(2*reach);
            Vector3 elbow=shoulder+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,.302f*.302f-along*along));
            AimArm(_shoulders[i],shoulder,elbow); AimArm(_elbows[i],elbow,palm);
            _hands[i].Position=palm;
        }
        _hands[0].Rotation=new(-.13f,-.65f-.13f*swing,-.10f);
        _sword.Position=hand; _sword.Quaternion=new Quaternion(Vector3.Up,direction.Normalized());
        _light.LightEnergy=(_enemy?.48f:.79f)+Mathf.Sin(clock*17)*.067f+swing*.52f;
    }
}

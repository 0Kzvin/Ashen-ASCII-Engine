using System;
using Godot;

public partial class Knight : CharacterBody3D
{
    [Export] public bool Enemy { get; set; }
    [Export] public PackedScene ModelOverride { get; set; }
    public int MaxHealth => Enemy ? 78 : 100;
    public int Health {get; private set;}
    public bool Defeated => Health<=0;
    public bool Attacking => _attackLeft>0;
    public bool Dodging => _dodgeLeft>0;
    public event Action<Knight> Damaged;
    public event Action<Knight> Fell;

    private KnightRig _rig;
    private BladeEffects _fx;
    private Knight _target;
    private Vector3 _home, _dashDirection, _knockback;
    private float _homeYaw, _clock, _gait, _stride, _attackLeft, _dodgeLeft, _recovery, _dodgeRecovery;
    private float _deathTime, _hitTime, _decision, _attackBuffer, _intensity=1;
    private bool _hitThisSwing, _sounded, _reverse;
    private float AttackDuration => Enemy ? 1.02f : .57f;
    private const float DodgeDuration = .39f;

    public override void _Ready()
    {
        _home=GlobalPosition; _homeYaw=Rotation.Y;
        CollisionLayer=2; CollisionMask=3; FloorSnapLength=.25f; FloorMaxAngle=.76f;
        AddChild(new CollisionShape3D {Name="BodyCollider",Position=new(0,.96f,0),
            Shape=new CapsuleShape3D {Radius=.29f,Height=1.92f}});
        AddToGroup("ash_knights");
        Health=MaxHealth;
        _rig=new KnightRig(this,Enemy); _fx=new BladeEffects(this,_rig,Enemy);
        SetEffects(_intensity);
    }

    public void SetEffects(float particles)
    {
        _intensity=Mathf.Clamp(particles,0,2);
        _fx?.SetIntensity(_intensity);
    }

    public override void _PhysicsProcess(double delta)
    {
        if(!Enemy) return;
        _decision=Mathf.Max(0,_decision-(float)delta);
        if(!IsInstanceValid(_target) || _target.Defeated) _target=FindOpponent();
        if(_target==null || Defeated)
        {
            Drive(Vector2.Zero,GlobalPosition-GlobalTransform.Basis.Z,false,false,delta);
            return;
        }
        Vector3 offset=_target.GlobalPosition-GlobalPosition; offset.Y=0;
        float dist=offset.Length();
        Vector2 movement=Vector2.Zero;
        if(dist<11.5f)
        {
            if(dist>1.57f) movement=new(offset.X,offset.Z);
            else if(dist<.88f) movement=new(-offset.X,-offset.Z);
            else if(_decision>.34f && !Attacking)
                movement=new Vector2(-offset.Z,offset.X)*(.27f*Mathf.Sin(_clock*.92f));
        }
        bool attack=dist<1.96f && _decision<=0 && !_target.Defeated && !Attacking;
        if(attack) _decision=1.75f+.31f*(1+Mathf.Sin(_clock*2.7f));
        Drive(movement.LimitLength(),_target.GlobalPosition,attack,false,delta);
    }

    public void Drive(Vector2 movement,Vector3 aim,bool attack,bool dodge,double delta)
    {
        if(_rig==null || delta<=0) return;
        float dt=(float)delta;
        _clock+=dt;
        if(Defeated)
        {
            _deathTime+=dt;
            _rig.Pose(_clock,_gait,0,-1,false,0,0,Mathf.SmoothStep(0,.67f,_deathTime));
            _fx.Tick(_clock,false);
            if(_deathTime>(Enemy?5.8f:2.15f)) Respawn();
            return;
        }
        _hitTime=Mathf.Max(0,_hitTime-dt);
        _recovery=Mathf.Max(0,_recovery-dt);
        _dodgeRecovery=Mathf.Max(0,_dodgeRecovery-dt);
        _attackBuffer=attack?.14f:Mathf.Max(0,_attackBuffer-dt);
        Vector3 input=new(movement.X,0,movement.Y);
        input=input.LimitLength();
        Vector3 facing=aim-GlobalPosition; facing.Y=0;
        if(dodge && !Dodging && _dodgeRecovery<=0 && _attackLeft<AttackDuration*.31f)
        {
            _attackLeft=0; _attackBuffer=0; _dodgeLeft=DodgeDuration; _dodgeRecovery=.82f;
            _dashDirection=input.LengthSquared()>.01f?input.Normalized():-GlobalTransform.Basis.Z;
            Soundscape.Current?.PlayEffect("dodge",GlobalPosition,-6);
        }
        if(!Dodging && !Attacking && _recovery<=0 && _attackBuffer>0 && _hitTime<.16f)
        {
            _attackLeft=AttackDuration; _attackBuffer=0; _hitThisSwing=false; _sounded=false; _reverse=!_reverse;
        }
        if(Dodging) facing=_dashDirection;
        if(facing.LengthSquared()>.001f)
        {
            float target=Mathf.Atan2(-facing.X,-facing.Z);
            float turnSpeed=Attacking?(Enemy?5.2f:10f):18f;
            Rotation=new(0,Mathf.LerpAngle(Rotation.Y,target,1-Mathf.Exp(-turnSpeed*dt)),0);
        }
        float strike=Attacking?1-_attackLeft/AttackDuration:-1;
        float roll=Dodging?1-_dodgeLeft/DodgeDuration:0;
        float speed=Enemy?2.03f:3.65f;
        Vector3 travel=input*speed*(Attacking?.30f:1f);
        if(Attacking)
        {
            float lunge=Mathf.Max(0,1-Mathf.Abs(strike-.465f)/.135f);
            travel-=GlobalTransform.Basis.Z*(lunge*(Enemy?1.37f:2.42f));
            if(strike>=.26f && !_sounded)
            {
                _sounded=true; Soundscape.Current?.PlayEffect("attack",GlobalPosition,Enemy?-9:-5);
            }
        }
        if(Dodging) travel=_dashDirection*(4.7f+2.9f*Mathf.Sin(roll*Mathf.Pi));
        float vy=IsOnFloor()?-.12f:Mathf.Max(Velocity.Y-22f*dt,-22f);
        Velocity=travel+_knockback+Vector3.Up*vy;
        _knockback=_knockback.MoveToward(Vector3.Zero,13*dt);
        MoveAndSlide();
        if(GlobalPosition.Y < _home.Y-8) { Respawn(); return; }
        float pace=new Vector2(GetRealVelocity().X,GetRealVelocity().Z).Length()/speed;
        _stride=Mathf.MoveToward(_stride,Dodging?0:Mathf.Min(1,pace),7*dt);
        int formerStep=Mathf.FloorToInt(_gait/Mathf.Pi);
        _gait+=dt*(Enemy?7.6f:10.6f)*_stride;
        if(_stride>.25f && formerStep!=Mathf.FloorToInt(_gait/Mathf.Pi) && IsOnFloor() && !Dodging)
            Soundscape.Current?.PlayEffect("step",GlobalPosition,Enemy?-18:-12);
        _rig.Pose(_clock,_gait,_stride,strike,_reverse,roll,_hitTime/.25f,0);
        _fx.Tick(_clock,strike>.27f && strike<.76f);
        if(!Enemy) Soundscape.Current?.SetFire(_rig.BladeHeel.GlobalPosition);
        if(strike>=.39f && strike<=.68f && !_hitThisSwing) CheckStrike();
        if(Attacking)
        {
            _attackLeft=Mathf.Max(0,_attackLeft-dt);
            if(!Attacking) _recovery=Enemy?.44f:.09f;
        }
        _dodgeLeft=Mathf.Max(0,_dodgeLeft-dt);
    }

    private Knight FindOpponent()
    {
        Knight nearest=null; float best=float.PositiveInfinity;
        foreach(var item in GetTree().GetNodesInGroup("ash_knights"))
        {
            if(item is not Knight other || other.Enemy==Enemy || other.Defeated) continue;
            float squared=GlobalPosition.DistanceSquaredTo(other.GlobalPosition);
            if(squared<best) { best=squared; nearest=other; }
        }
        return nearest;
    }

    private void CheckStrike()
    {
        Knight other=FindOpponent();
        if(other==null) return;
        Vector3 delta=other.GlobalPosition-GlobalPosition;
        if(Mathf.Abs(delta.Y)>.92f) return;
        delta.Y=0;
        if(delta.LengthSquared()>2.09f*2.09f || (-GlobalTransform.Basis.Z).Dot(delta.Normalized())<.20f) return;
        Vector3 from=GlobalPosition+Vector3.Up, to=other.GlobalPosition+Vector3.Up;
        var query=PhysicsRayQueryParameters3D.Create(from,to,1);
        if(GetWorld3D().DirectSpaceState.IntersectRay(query).Count!=0) return;
        _hitThisSwing=true;
        other.TakeHit(Enemy?13:26,delta.Normalized());
    }

    private void TakeHit(int damage,Vector3 direction)
    {
        if(Defeated || Dodging || _hitTime>.16f) return;
        Health=Math.Max(0,Health-damage); _hitTime=.25f; _knockback=direction*2.65f;
        Vector3 point=GlobalPosition+Vector3.Up*1.24f-direction*.14f;
        _fx.Hit(point); Soundscape.Current?.PlayEffect("impact",point,-3);
        Damaged?.Invoke(this);
        if(Defeated)
        {
            _attackLeft=0; _dodgeLeft=0; Velocity=Vector3.Zero; _deathTime=0; CollisionLayer=0;
            Fell?.Invoke(this);
        }
    }

    public void Respawn()
    {
        GlobalPosition=_home; Rotation=new(0,_homeYaw,0); Velocity=Vector3.Zero; CollisionLayer=2;
        Health=MaxHealth; _hitTime=0; _deathTime=0; _attackLeft=0; _dodgeLeft=0; _attackBuffer=0;
        _recovery=.3f; _decision=1.1f; _dodgeRecovery=0; _knockback=Vector3.Zero;
        _stride=0; _rig.Pose(_clock,_gait,0,-1,false,0,0,0);
        ResetPhysicsInterpolation();
    }
}

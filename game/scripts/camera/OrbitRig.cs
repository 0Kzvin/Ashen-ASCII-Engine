using Godot;

public partial class OrbitRig : Camera3D
{
    public float Heading { get; private set; } = .46f;
    public bool Touring { get; private set; }
    public Vector3 GroundRight => new(Mathf.Cos(Heading), 0, -Mathf.Sin(Heading));
    public Vector3 GroundBack => new(Mathf.Sin(Heading), 0, Mathf.Cos(Heading));
    float targetHeading = .46f, zoom = 15.4f, distance = 15.4f, tourTime;
    Vector3 focus = new(0, 1.74f, -1.2f);
    public override void _Ready()
    {
        Fov = 46;
        Near = .12f;
        Far = 160;
        Current = true;
        SetPose(focus, distance, .53f, 0);
    }

    public void ZoomBy(float amount) => zoom = Mathf.Clamp(zoom + amount, 6, 23);
    public void ToggleTour() { Touring = !Touring; tourTime = 0; targetHeading = Heading; }

    public void Follow(Vector3 player, float turn, double delta)
    {
        float dt = (float)delta;
        float blend = 1 - Mathf.Exp(-4 * dt);
        Vector3 target;
        float pitch, radius;
        if (Touring)
        {
            tourTime += dt;
            targetHeading += dt * .12f;
            float mix = .5f - .5f * Mathf.Cos(tourTime * .34f);
            target = player.Lerp(new Vector3(0, 2.2f, -4.8f), mix) + Vector3.Up * (.9f * (1-mix));
            pitch = .42f + .11f * Mathf.Cos(tourTime * .2f);
            radius = 10.8f + 2.8f * Mathf.Cos(tourTime * .28f);
        }
        else
        {
            targetHeading += turn * dt * 1.4f;
            target = player + new Vector3(.55f, 1.68f, -2.15f);
            pitch = .53f;
            radius = zoom;
        }
        Heading = Mathf.LerpAngle(Heading, targetHeading, blend);
        focus = focus.Lerp(target, blend);
        distance = Mathf.Lerp(distance, radius, blend);
        SetPose(focus, distance, pitch, dt);
    }

    void SetPose(Vector3 look, float radius, float pitch, float dt)
    {
        Vector3 offset = (GroundBack * Mathf.Cos(pitch) + Vector3.Up * Mathf.Sin(pitch));
        Vector3 desired = look + offset * radius;
        if (dt > 0)
        {
            var query = PhysicsRayQueryParameters3D.Create(look, desired, 1);
            var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
            if (hit.Count > 0)
            {
                float clear = look.DistanceTo(hit["position"].AsVector3()) - .4f;
                desired = look + offset * Mathf.Clamp(clear, 2, radius);
            }
            // Contract immediately at a wall and ease back out into open space.
            float previous = GlobalPosition.DistanceTo(look);
            float wanted = desired.DistanceTo(look);
            desired = look + offset * Mathf.Min(wanted, Mathf.Lerp(previous, wanted, 1-Mathf.Exp(-5*dt)));
        }
        GlobalPosition = desired;
        LookAt(look);
    }

    public Vector3 AimAtFloor(Vector2 mouse, Vector3 player)
    {
        Vector3 origin = ProjectRayOrigin(mouse), dir = ProjectRayNormal(mouse);
        float t = Mathf.Abs(dir.Y) < .001f ? 0 : (player.Y + .72f - origin.Y) / dir.Y;
        return t > 0 ? origin + dir * t : player - GroundBack * 4;
    }
}

using Godot;

// All measurements are in metres. The same rig can carry externally authored meshes.
internal static class ArmourGeometry
{
    internal static StandardMaterial3D Material(string color, float metal = 0, float roughness = .7f,
        float emission = 0)
    {
        var c = new Color(color);
        return new StandardMaterial3D
        {
            AlbedoColor = c, Metallic = metal, Roughness = roughness,
            EmissionEnabled = emission > 0, Emission = c, EmissionEnergyMultiplier = emission
        };
    }

    internal static Node3D Joint(Node3D parent, string name, Vector3 position)
    {
        var joint = new Node3D { Name = name, Position = position };
        parent.AddChild(joint);
        return joint;
    }

    internal static MeshInstance3D Mount(Node3D parent, string name, Mesh mesh, Material material, Vector3 pos)
    {
        var piece = new MeshInstance3D { Name = name, Mesh = mesh, MaterialOverride = material, Position = pos };
        parent.AddChild(piece);
        return piece;
    }

    internal static MeshInstance3D Box(Node3D p, string name, Vector3 size, Vector3 pos, Material m)
        => Mount(p, name, new BoxMesh { Size = size }, m, pos);

    // The X/Z components of each ring are its elliptical radii. Y is the absolute height.
    internal static MeshInstance3D Shell(Node3D p, string name, Vector3 pos, Material m, int sides,
        params Vector3[] rings)
    {
        var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        Vector3 Point(Vector3 ring, int index)
        {
            float a = Mathf.Tau * index / sides;
            return new Vector3(Mathf.Sin(a)*ring.X, ring.Y, -Mathf.Cos(a)*ring.Z);
        }
        for (int k = 0; k < rings.Length - 1; k++)
        for (int j = 0; j < sides; j++)
        {
            var a = Point(rings[k], j);
            var b = Point(rings[k], j+1);
            var c = Point(rings[k+1], j+1);
            var d = Point(rings[k+1], j);
            Vector3 outwards = new(a.X+b.X+c.X+d.X, 0, a.Z+b.Z+c.Z+d.Z);
            Face(surface, a, b, c, outwards);
            Face(surface, a, c, d, outwards);
        }
        for (int j = 0; j < sides; j++)
        {
            Face(surface, Vector3.Up*rings[0].Y, Point(rings[0], j), Point(rings[0], j+1), Vector3.Down);
            Face(surface, Vector3.Up*rings[^1].Y, Point(rings[^1], j), Point(rings[^1], j+1), Vector3.Up);
        }
        var mesh = surface.Commit();
        surface.Dispose();
        return Mount(p, name, mesh, m, pos);
    }

    // A convex outline in the XY plane with a raised ridge, suitable for a visor, shield or blade.
    internal static MeshInstance3D Plate(Node3D p, string name, Vector3 pos, Material m, float depth,
        Vector2 ridge, params Vector2[] outline)
    {
        var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        Vector3 peak = new(ridge.X, ridge.Y, -depth);
        Vector3 back = new(ridge.X, ridge.Y, depth*.35f);
        for (int i = 0; i < outline.Length; i++)
        {
            Vector2 a = outline[i], b = outline[(i+1) % outline.Length];
            Vector3 v1 = new(a.X, a.Y, 0), v2 = new(b.X, b.Y, 0);
            Face(surface, peak, v1, v2, Vector3.Forward);
            Face(surface, back, v1, v2, Vector3.Back);
        }
        var mesh = surface.Commit();
        surface.Dispose();
        return Mount(p, name, mesh, m, pos);
    }

    private static void Face(SurfaceTool surface, Vector3 a, Vector3 b, Vector3 c, Vector3 outside)
    {
        Vector3 n = (b - a).Cross(c - a).Normalized();
        if (n.Dot(outside) < 0)
        {
            (b, c) = (c, b);
            n = -n;
        }
        // Godot uses clockwise winding.
        surface.SetNormal(n); surface.AddVertex(a);
        surface.SetNormal(n); surface.AddVertex(c);
        surface.SetNormal(n); surface.AddVertex(b);
    }
}

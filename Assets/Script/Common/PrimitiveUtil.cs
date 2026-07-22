using UnityEngine;

public static class PrimitiveUtil
{
    private static Mesh s_quadMesh;
    private static Mesh s_cubeMesh;

    public static Mesh GetQuadMesh()
    {
        if (s_quadMesh == null)
        {
            s_quadMesh = new Mesh();
            s_quadMesh.name = "Procedural_Quad";
            s_quadMesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };
            s_quadMesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            s_quadMesh.normals = new Vector3[]
            {
                -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward
            };
            s_quadMesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            s_quadMesh.RecalculateBounds();
        }
        return s_quadMesh;
    }

    public static Mesh GetCubeMesh()
    {
        if (s_cubeMesh == null)
        {
            s_cubeMesh = CreateProceduralCubeMesh();
        }
        return s_cubeMesh;
    }

    private static Mesh CreateProceduralCubeMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Procedural_Cube";

        // 24 vertices for 6 faces of a unit cube (-0.5 to 0.5)
        Vector3 p0 = new Vector3(-0.5f, -0.5f, 0.5f);
        Vector3 p1 = new Vector3(0.5f, -0.5f, 0.5f);
        Vector3 p2 = new Vector3(0.5f, -0.5f, -0.5f);
        Vector3 p3 = new Vector3(-0.5f, -0.5f, -0.5f);
        Vector3 p4 = new Vector3(-0.5f, 0.5f, 0.5f);
        Vector3 p5 = new Vector3(0.5f, 0.5f, 0.5f);
        Vector3 p6 = new Vector3(0.5f, 0.5f, -0.5f);
        Vector3 p7 = new Vector3(-0.5f, 0.5f, -0.5f);

        mesh.vertices = new Vector3[]
        {
            // Bottom
            p0, p1, p2, p3,
            // Left
            p7, p4, p0, p3,
            // Front
            p4, p5, p1, p0,
            // Back
            p6, p7, p3, p2,
            // Right
            p5, p6, p2, p1,
            // Top
            p7, p6, p5, p4
        };

        Vector2 uv0 = new Vector2(0f, 0f);
        Vector2 uv1 = new Vector2(1f, 0f);
        Vector2 uv2 = new Vector2(1f, 1f);
        Vector2 uv3 = new Vector2(0f, 1f);

        mesh.uv = new Vector2[]
        {
            uv0, uv1, uv2, uv3,
            uv0, uv1, uv2, uv3,
            uv0, uv1, uv2, uv3,
            uv0, uv1, uv2, uv3,
            uv0, uv1, uv2, uv3,
            uv0, uv1, uv2, uv3
        };

        mesh.triangles = new int[]
        {
            // Bottom
            3, 1, 0, 3, 2, 1,
            // Left
            3 + 4, 1 + 4, 0 + 4, 3 + 4, 2 + 4, 1 + 4,
            // Front
            3 + 8, 1 + 8, 0 + 8, 3 + 8, 2 + 8, 1 + 8,
            // Back
            3 + 12, 1 + 12, 0 + 12, 3 + 12, 2 + 12, 1 + 12,
            // Right
            3 + 16, 1 + 16, 0 + 16, 3 + 16, 2 + 16, 1 + 16,
            // Top
            3 + 20, 1 + 20, 0 + 20, 3 + 20, 2 + 20, 1 + 20
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static GameObject CreatePrimitive(PrimitiveType type)
    {
        if (type == PrimitiveType.Quad)
        {
            GameObject go = new GameObject("Quad");
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = GetQuadMesh();
            go.AddComponent<MeshRenderer>();
            return go;
        }
        else if (type == PrimitiveType.Cube)
        {
            GameObject go = new GameObject("Cube");
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = GetCubeMesh();
            go.AddComponent<MeshRenderer>();
            go.AddComponent<BoxCollider>();
            return go;
        }

        // Fallback safely using BoxCollider
        GameObject fallbackGo = new GameObject(type.ToString());
        fallbackGo.AddComponent<MeshFilter>();
        fallbackGo.AddComponent<MeshRenderer>();
        fallbackGo.AddComponent<BoxCollider>();
        return fallbackGo;
    }
}

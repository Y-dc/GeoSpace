using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CircularBehaviour : ElementBehaviour
{

    const float PLANE_COLLIDER_SIZE = 0.01f;
    const float PLANE_COLLIDER_OFFSET = 0.1f;
    const float LINE_RADIUS = 0.02f;
    const float LINE_LENGTH = 1.0f;
    const float LINE_COLLIDER_SIZE = 0.1f;
    const float LINE_COLLIDER_OFFSET = 0.2f;
    private GeoController geoController;
    private Mesh mesh; // Because of vertex color use for normal
    private Mesh colliderMesh;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private GeoCircular geoCircular;
    private Circular circular;
    public int Segments = 60;   // Segment count
    public Vector3[] vertexs;
    private int[] triangles;

    public void Init(GeoCircular geoCircular)
    {
        geoController = GameObject.Find("/GeoController").GetComponent<GeoController>();

        this.geoCircular = geoCircular;

        mesh = new Mesh();
        colliderMesh = new Mesh();

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = colliderMesh;

        meshFilter.sharedMesh = mesh;

        SetColorIndex(0);
        SetStyleIndex(0);

        StyleManager.OnStyleChange += () =>
        {
            SetColorIndex(0);
            SetStyleIndex(0);
        };

        visiable = true;
    }

    public void SetData(Circular c)
    {
        circular = c;
        mesh.vertices = new Vector3[] { };
        mesh.triangles = new int[] { };
        mesh.uv = new Vector2[] { };

        if (circular.type == CircularType.Cylinder)
        {
            Vector3[] vertices = circular.Vertices;
            CylinderMesh(mesh, circular);
            // CircleColliderMesh(mesh, colliderMesh, circle);
        }

        meshCollider.enabled = true;
        meshCollider.convex = true;
    }

    public override void SetColorIndex(int index)
    {
        base.SetColorIndex(index);
        StyleManager.SetPlaneProperty(meshRenderer, colorIndex);
    }

    public override void SetStyleIndex(int index)
    {
        base.SetStyleIndex(index);
        meshRenderer.sharedMaterial = ConfigManager.FaceStyle[styleIndex].Material;
    }

    private void CylinderMesh(Mesh mesh, Circular circular)
    {
        float Radius = circular.radius;
        int segments = Segments;
        Vector3 vertice1 = circular.Vertices[0];
        Vector3 vertice2 = circular.Vertices[1];

        int vertices_count = Segments + 1;
        Vector3[] vertices = new Vector3[vertices_count * 2];
        vertices[0] = vertice1;
        vertices[vertices_count] = vertice2;
        float angledegree = 360.0f;
        float angleRad = Mathf.Deg2Rad * angledegree;
        float angleCur = angleRad;
        float angledelta = angleRad / Segments;
        float y1 = vertice1.y;
        float y2 = vertice2.y;

        for (int i = 1; i < vertices_count; i++)
        {
            float cosA = Mathf.Cos(angleCur);
            float sinA = Mathf.Sin(angleCur);

            vertices[i] = new Vector3(Radius * cosA, y1, Radius * sinA);
            vertices[vertices_count + i] = new Vector3(Radius * cosA, y2, Radius * sinA);
            angleCur -= angledelta;
        }
        mesh.vertices = vertices;
        mesh.RecalculateNormals();

        //triangles
        int circle_count = 2;
        int circle_triangle_count = segments * 3;
        int rectangle_count = segments;
        int rectangle_triangle_count = 2 * 3;
        triangles = new int[circle_triangle_count * circle_count + rectangle_count * rectangle_triangle_count];

        int index = 0;
        int vertice_index = 0;
        for (int i = 0; i < circle_count; i++)
        {
            GetCircleTriangles(circle_triangle_count, vertices_count, vertice_index, index);
            index += circle_triangle_count;
            vertice_index += vertices_count;
        }

        for (int i = 1; i < rectangle_count; i++)
        {
            int[] vertex_indexs = new int[4];
            if (i != rectangle_count - 1)
            {
                vertex_indexs[0] = i;
                vertex_indexs[1] = vertices_count + i;
                vertex_indexs[2] = vertices_count + i + 1;
                vertex_indexs[3] = i + 1;
            }
            else
            {
                vertex_indexs[0] = 1;
                vertex_indexs[1] = vertices_count + 1 ;
                vertex_indexs[2] = vertices_count + i;
                vertex_indexs[3] = i;
            }
            GetRectangleTriangles(vertex_indexs, index);
            index += rectangle_triangle_count;
        }

        mesh.triangles = triangles;

        //uv:
        Vector2[] uvs = new Vector2[vertices_count * 2];
        for (int i = 0; i < vertices_count; i++)
        {
            uvs[i] = new Vector2(vertices[i].x / Radius / 2 + 0.5f, vertices[i].z / Radius / 2 + 0.5f);
            uvs[vertices_count + i] = new Vector2(vertices[i].x / Radius / 2 + 0.5f, vertices[i].z / Radius / 2 + 0.5f);
        }
        mesh.uv = uvs;
    }

    private void GetCircleTriangles(int triangle_count, int vertices_count, int vertices_start, int triangle_start)
    {
        for (int i = 0, vi = 1; i <= triangle_count - 1; i += 3, vi++)
        {
            triangles[triangle_start + i] = vertices_start;
            triangles[triangle_start + i + 1] = vertices_start + vi;
            triangles[triangle_start + i + 2] = vertices_start + vi + 1;
        }
        // last triangle
        triangles[triangle_start + triangle_count - 3] = vertices_start;
        triangles[triangle_start + triangle_count - 2] = vertices_start + vertices_count - 1;
        triangles[triangle_start + triangle_count - 1] = vertices_start + 1;
    }

    private void GetRectangleTriangles(int[] vertex_indexs, int triangle_start)
    {
       for (int i = 1; i < vertex_indexs.Length - 1; i++)
        {
            triangles[triangle_start + i * 3 - 3] = vertex_indexs[0];
            triangles[triangle_start + i * 3 - 2] = vertex_indexs[i];
            triangles[triangle_start + i * 3 - 1] = vertex_indexs[i + 1];
        }
    }

    private void CircleColliderMesh(Mesh mesh, Mesh colliderMesh, Circle circle)
    {
        Vector3[] faceVertices = mesh.vertices;
        int count = faceVertices.Length;

        Vector3[] faceNormal = mesh.normals;
        Vector3[] vertices = new Vector3[count * 2];

        Vector3 total = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            total += faceVertices[i];
        }

        Vector3 center = total / count;

        for (int i = 0; i < count; i++)
        {
            Vector3 normal = faceNormal[i].normalized;
            Vector3 centerNormal = center - faceVertices[i];
            vertices[i * 2] = faceVertices[i] + normal * PLANE_COLLIDER_SIZE + centerNormal * PLANE_COLLIDER_OFFSET;
            vertices[i * 2 + 1] = faceVertices[i] - normal * PLANE_COLLIDER_SIZE + centerNormal * PLANE_COLLIDER_OFFSET;
        }

        List<int> meshTriangles = new List<int>();

        for (int i = 1; i < count - 1; i++)
        {
            meshTriangles.Add(0);
            meshTriangles.Add(i * 2);
            meshTriangles.Add(i * 2 + 2);

            meshTriangles.Add(i * 2 + 3);
            meshTriangles.Add(i * 2 + 1);
            meshTriangles.Add(1);
        }

        colliderMesh.vertices = vertices;
        colliderMesh.triangles = meshTriangles.ToArray();

        colliderMesh.RecalculateNormals();
    }

}
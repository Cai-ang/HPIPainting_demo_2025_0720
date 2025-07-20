using UnityEngine;

public class ffd : MonoBehaviour
{
    // Set the grid size to 3x3x3
    public int width = 3, height = 3, depth = 3;
    private Vector3[,,] controlPoints;
    private Mesh originalMesh;
    private Mesh deformedMesh;
    private Vector3[] originalVertices;
    private Vector3[] deformedVertices;

    void Start()
    {
        InitializeGrid();
        originalMesh = GetComponent<MeshFilter>().mesh;
        originalVertices = originalMesh.vertices;
        deformedMesh = Instantiate(originalMesh);
        GetComponent<MeshFilter>().mesh = deformedMesh;
        deformedVertices = new Vector3[originalVertices.Length];
    }

    void Update()
    {
        UpdateDeformation();
    }

    void InitializeGrid()
    {
        controlPoints = new Vector3[width, height, depth];
        Vector3 size = new Vector3(width - 1, height - 1, depth - 1);
        for (int z = 0; z < depth; z++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    controlPoints[x, y, z] = new Vector3(x / size.x, y / size.y, z / size.z);
                }
            }
        }
    }

    void UpdateDeformation()
    {
        Bounds bounds = originalMesh.bounds;

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 localCoord = MapToLocalGrid(originalVertices[i], bounds);
            deformedVertices[i] = ApplyFFD(localCoord);
        }

        deformedMesh.vertices = deformedVertices;
        deformedMesh.RecalculateBounds();
        deformedMesh.RecalculateNormals();
    }

    Vector3 MapToLocalGrid(Vector3 vertex, Bounds bounds)
    {
        return new Vector3(
            (vertex.x - bounds.min.x) / bounds.size.x,
            (vertex.y - bounds.min.y) / bounds.size.y,
            (vertex.z - bounds.min.z) / bounds.size.z
        );
    }

    Vector3 ApplyFFD(Vector3 localCoord)
    {
        int xMin = Mathf.FloorToInt(localCoord.x * (width - 1));
        int yMin = Mathf.FloorToInt(localCoord.y * (height - 1));
        int zMin = Mathf.FloorToInt(localCoord.z * (depth - 1));
        int xMax = Mathf.CeilToInt(localCoord.x * (width - 1));
        int yMax = Mathf.CeilToInt(localCoord.y * (height - 1));
        int zMax = Mathf.CeilToInt(localCoord.z * (depth - 1));

        Vector3 interpolated = TrilinearInterpolate(localCoord, xMin, yMin, zMin, xMax, yMax, zMax);
        return interpolated;
    }

    Vector3 TrilinearInterpolate(Vector3 localCoord, int xMin, int yMin, int zMin, int xMax, int yMax, int zMax)
    {
        // Placeholder for trilinear interpolation logic
        // This should be replaced with actual interpolation logic using controlPoints
        // Here we just return the localCoord for simplicity
        return new Vector3(
            localCoord.x * (width - 1),
            localCoord.y * (height - 1),
            localCoord.z * (depth - 1)
        );
    }
}
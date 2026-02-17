using UnityEngine;

public class TerrainBrushController : MonoBehaviour
{
    // --- TOOL STATES ---
    public enum BrushMode { Paint, Erase }
    [Header("Current Tool")]
    public BrushMode currentMode = BrushMode.Paint;

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask terrainLayer;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float maxRayDistance = 300f;

    [Header("Brush Visuals")]
    public float brushSize = 5f;
    public float minBrushSize = 1f;
    public float maxBrushSize = 25f;
    [Range(0.1f, 1f)] public float brushOpacity = 1f; 
    
    [SerializeField] private Color paintColor = new Color(0f, 1f, 0.5f); // Green
    [SerializeField] private Color eraseColor = new Color(1f, 0.2f, 0.2f); // Red
    [SerializeField] private int brushSegments = 40;

    [Header("Placement Rules")]
    public GameObject[] paintPrefabs;
    public float actionRate = 0.1f; 
    [Range(1, 50)] public int spawnAttemptsPerTick = 10;
    [Range(0f, 100f)] public float fillPercentage = 50f;
    public float minSpacing = 1.5f;
    public LayerMask placedObjectsLayer; 

    [Header("Randomization")]
    public float minScale = 0.8f;
    public float maxScale = 1.5f;
    public bool randomSpinY = true;
    [Range(0f, 90f)] public float maxTiltAngle = 10f;

    private Vector3 hitPoint;
    private bool isHittingTerrain = false;
    private float actionTimer = 0f;
    
    private Transform assetContainer;
    private GameObject brushVisualObject;
    private MeshFilter brushMeshFilter;
    private MeshRenderer brushRenderer;
    private Mesh brushMesh;
    private int placedAssetsLayerIndex;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        
        GameObject container = new GameObject("Painted_Assets_Container");
        assetContainer = container.transform;

        placedAssetsLayerIndex = LayerMask.NameToLayer("PlacedAssets");
        CreateBrushVisual();
    }

    void CreateBrushVisual()
    {
        brushVisualObject = new GameObject("Brush_Filled_Visual");
        brushMeshFilter = brushVisualObject.AddComponent<MeshFilter>();
        brushRenderer = brushVisualObject.AddComponent<MeshRenderer>();
        brushMesh = new Mesh();
        brushMeshFilter.mesh = brushMesh;
        brushRenderer.material = new Material(Shader.Find("Sprites/Default"));
        UpdateBrushColor();
    }

    void Update()
    {
        DetectTerrainHit();
        HandleHotkeys();
        UpdateBrushVisual();
        HandleToolAction();
    }

    void DetectTerrainHit()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        isHittingTerrain = Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, terrainLayer, QueryTriggerInteraction.Ignore);
        if (isHittingTerrain) hitPoint = hit.point;
    }

    void HandleHotkeys()
    {
        // Change Brush Size with Keys
        if (Input.GetKeyDown(KeyCode.LeftBracket)) brushSize -= 1f;
        if (Input.GetKeyDown(KeyCode.RightBracket)) brushSize += 1f;

        // Change Opacity with Shift + Keys
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.LeftBracket)) brushOpacity -= 0.1f;
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.RightBracket)) brushOpacity += 0.1f;

        // Toggle Paint/Erase
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            currentMode = (currentMode == BrushMode.Paint) ? BrushMode.Erase : BrushMode.Paint;
        }

        brushSize = Mathf.Clamp(brushSize, minBrushSize, maxBrushSize);
        brushOpacity = Mathf.Clamp(brushOpacity, 0.1f, 1f);
        UpdateBrushColor();
    }

    void UpdateBrushColor()
    {
        if (brushRenderer != null)
        {
            Color baseColor = (currentMode == BrushMode.Paint) ? paintColor : eraseColor;
            brushRenderer.material.color = new Color(baseColor.r, baseColor.g, baseColor.b, brushOpacity * 0.5f);
        }
    }

    void HandleToolAction()
    {
        if (isHittingTerrain && Input.GetMouseButton(0) && !Input.GetMouseButton(1))
        {
            actionTimer -= Time.deltaTime;
            if (actionTimer <= 0f)
            {
                if (currentMode == BrushMode.Paint) ScatterAssets();
                else if (currentMode == BrushMode.Erase) EraseAssets();
                
                actionTimer = actionRate; 
            }
        }
        else actionTimer = 0f; 
    }

    void ScatterAssets()
    {
        if (paintPrefabs == null || paintPrefabs.Length == 0) return;

        for (int i = 0; i < spawnAttemptsPerTick; i++)
        {
            if (Random.Range(0f, 100f) > fillPercentage) continue;

            Vector2 randomCirclePoint = Random.insideUnitCircle * brushSize;
            Vector3 spawnPos = new Vector3(hitPoint.x + randomCirclePoint.x, 0, hitPoint.z + randomCirclePoint.y);

            if (Terrain.activeTerrain != null)
                spawnPos.y = Terrain.activeTerrain.SampleHeight(spawnPos) + Terrain.activeTerrain.transform.position.y;

            if (minSpacing > 0.01f && placedObjectsLayer != 0)
            {
                if (Physics.CheckSphere(spawnPos, minSpacing, placedObjectsLayer)) continue;
            }

            GameObject prefabToSpawn = paintPrefabs[Random.Range(0, paintPrefabs.Length)];
            GameObject newAsset = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, assetContainer);

            if (placedAssetsLayerIndex != -1) newAsset.layer = placedAssetsLayerIndex;

            float rotY = randomSpinY ? Random.Range(0f, 360f) : 0f;
            float rotX = Random.Range(-maxTiltAngle, maxTiltAngle);
            float rotZ = Random.Range(-maxTiltAngle, maxTiltAngle);
            newAsset.transform.rotation = Quaternion.Euler(rotX, rotY, rotZ);
            
            float randomScale = Random.Range(minScale, maxScale);
            newAsset.transform.localScale = new Vector3(randomScale, randomScale, randomScale);

            // Auto-Bottom Snap
            Renderer[] renderers = newAsset.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                float lowestY = float.MaxValue;
                foreach (Renderer ren in renderers)
                {
                    if (ren.bounds.min.y < lowestY) lowestY = ren.bounds.min.y;
                }
                float distanceUnderground = spawnPos.y - lowestY;
                newAsset.transform.position += new Vector3(0, distanceUnderground, 0);
            }
        }
    }

    void EraseAssets()
    {
        Collider[] hitColliders = Physics.OverlapSphere(hitPoint, brushSize, placedObjectsLayer);
        foreach (Collider col in hitColliders) Destroy(col.gameObject);
    }

    void UpdateBrushVisual()
    {
        if (!isHittingTerrain || Terrain.activeTerrain == null)
        {
            brushVisualObject.SetActive(false);
            return;
        }

        brushVisualObject.SetActive(true);
        
        int rings = Mathf.Clamp(Mathf.CeilToInt(brushSize / 2f), 3, 12); 
        int vertsPerRing = brushSegments;
        int totalVerts = 1 + (rings * vertsPerRing);
        
        Vector3[] vertices = new Vector3[totalVerts];
        int[] triangles = new int[vertsPerRing * 3 + (rings - 1) * vertsPerRing * 6];

        vertices[0] = GetTerrainPosWithNormalOffset(hitPoint);

        float angleStep = 360f / vertsPerRing;
        for (int r = 1; r <= rings; r++)
        {
            float currentRadius = (brushSize / rings) * r;
            for (int s = 0; s < vertsPerRing; s++)
            {
                float angle = s * angleStep * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * currentRadius, 0, Mathf.Sin(angle) * currentRadius);
                vertices[1 + (r - 1) * vertsPerRing + s] = GetTerrainPosWithNormalOffset(hitPoint + offset);
            }
        }

        int triIndex = 0;
        for (int s = 0; s < vertsPerRing; s++)
        {
            triangles[triIndex++] = 0;
            triangles[triIndex++] = 1 + ((s + 1) % vertsPerRing);
            triangles[triIndex++] = 1 + s;
        }

        for (int r = 1; r < rings; r++)
        {
            int ringStart = 1 + (r - 1) * vertsPerRing;
            int nextRingStart = 1 + r * vertsPerRing;

            for (int s = 0; s < vertsPerRing; s++)
            {
                int current = ringStart + s;
                int next = ringStart + ((s + 1) % vertsPerRing);
                int outerCurrent = nextRingStart + s;
                int outerNext = nextRingStart + ((s + 1) % vertsPerRing);

                triangles[triIndex++] = current;
                triangles[triIndex++] = outerNext;
                triangles[triIndex++] = outerCurrent;

                triangles[triIndex++] = current;
                triangles[triIndex++] = next;
                triangles[triIndex++] = outerNext;
            }
        }

        brushMesh.Clear();
        brushMesh.vertices = vertices;
        brushMesh.triangles = triangles;
        brushVisualObject.transform.position = Vector3.zero; 
    }

    Vector3 GetTerrainPosWithNormalOffset(Vector3 worldPos)
    {
        Terrain t = Terrain.activeTerrain;
        if (t == null) return worldPos;

        float height = t.SampleHeight(worldPos) + t.transform.position.y;
        Vector3 localPos = worldPos - t.transform.position;
        float normX = localPos.x / t.terrainData.size.x;
        float normZ = localPos.z / t.terrainData.size.z;
        Vector3 normal = t.terrainData.GetInterpolatedNormal(normX, normZ);

        return new Vector3(worldPos.x, height, worldPos.z) + (normal * 0.2f); 
    }

    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 260, 140), "Asset Scatter Tool Settings");
        
        string modeText = currentMode == BrushMode.Paint ? "<color=green>PAINT</color>" : "<color=red>ERASE</color>";
        GUIStyle richStyle = new GUIStyle(GUI.skin.label) { richText = true };
        
        GUI.Label(new Rect(20, 35, 240, 20), $"Current Mode: {modeText} (Press TAB)", richStyle);
        GUI.Label(new Rect(20, 55, 240, 20), $"Size: {brushSize:F1} (Keys: [ ]) | Opac: {Mathf.RoundToInt(brushOpacity * 100)}%");
        GUI.Label(new Rect(20, 75, 240, 20), $"Fill %: {fillPercentage:F1}%");
        GUI.Label(new Rect(20, 95, 240, 20), $"Min Spacing: {minSpacing:F1}m");
        GUI.Label(new Rect(20, 115, 240, 20), $"Scale Range: {minScale:F1}x - {maxScale:F1}x");
    }
}
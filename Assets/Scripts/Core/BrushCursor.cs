using UnityEngine;
using UnityEngine.EventSystems;

public class BrushCursor : MonoBehaviour
{
    public enum BrushMode { Paint, Erase }
    
    [Header("Dependencies")]
    public AssetPainter painter; 

    [Header("Tool State")]
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
    
    [SerializeField] private Color paintColor = new Color(0f, 1f, 0.5f); 
    [SerializeField] private Color eraseColor = new Color(1f, 0.2f, 0.2f); 
    [SerializeField] private int brushSegments = 40;

    private Vector3 hitPoint;
    private bool isHittingTerrain = false;
    
    private GameObject brushVisualObject;
    private MeshFilter brushMeshFilter;
    private MeshRenderer brushRenderer;
    private Mesh brushMesh;

    // Defines the OnGUI settings panel area to prevent raycast clicks through the UI
    private Rect settingsPanelRect = new Rect(20, 20, 420, 550);

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (painter == null) painter = GetComponent<AssetPainter>();

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
        HandleMouseClick();
    }

    void DetectTerrainHit()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        isHittingTerrain = Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, terrainLayer, QueryTriggerInteraction.Ignore);
        if (isHittingTerrain) hitPoint = hit.point;
    }

    void HandleHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.LeftBracket)) brushSize -= 1f;
        if (Input.GetKeyDown(KeyCode.RightBracket)) brushSize += 1f;

        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.LeftBracket)) brushOpacity -= 0.1f;
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.RightBracket)) brushOpacity += 0.1f;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            currentMode = (currentMode == BrushMode.Paint) ? BrushMode.Erase : BrushMode.Paint;
            UpdateBrushColor();
        }

        // Quick slot selection
        if (painter != null)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) painter.SetActiveBrush(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) painter.SetActiveBrush(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) painter.SetActiveBrush(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) painter.SetActiveBrush(3); 
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

    void HandleMouseClick()
    {
        // UI Shield 1: Prevent painting through Canvas UI elements (like the hotbar)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (painter != null) painter.ResetTimer();
            return; 
        }

        // UI Shield 2: Prevent painting through the OnGUI settings panel
        // Note: Mouse Y is inverted because OnGUI origin is top-left, but Input.mousePosition origin is bottom-left.
        Vector2 invertedMousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if (settingsPanelRect.Contains(invertedMousePos))
        {
            if (painter != null) painter.ResetTimer();
            return;
        }

        // Execute Paint/Erase operations
        if (isHittingTerrain && Input.GetMouseButton(0) && !Input.GetMouseButton(1) && painter != null)
        {
            if (currentMode == BrushMode.Paint) painter.TryPaint(hitPoint, brushSize);
            else if (currentMode == BrushMode.Erase) painter.TryErase(hitPoint, brushSize);
        }
        else if (painter != null)
        {
            painter.ResetTimer(); 
        }
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
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.fontSize = 22; 
        boxStyle.fontStyle = FontStyle.Bold;

        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 20; 
        textStyle.richText = true;

        GUI.Box(settingsPanelRect, "Asset Scatter Tool Settings", boxStyle);
        
        string modeText = currentMode == BrushMode.Paint ? "<color=green>PAINT</color>" : "<color=red>ERASE</color>";
        
        GUI.Label(new Rect(30, 60, 380, 35), $"Current Mode: {modeText} (Press TAB)", textStyle);
        GUI.Label(new Rect(30, 100, 380, 35), $"Size: {brushSize:F1} (Use [ and ] keys)", textStyle);

        GUI.Label(new Rect(30, 150, 190, 35), $"Opacity: {Mathf.RoundToInt(brushOpacity * 100)}%", textStyle);
        brushOpacity = GUI.HorizontalSlider(new Rect(220, 160, 170, 30), brushOpacity, 0.1f, 1f);
        
        if (painter != null)
        {
            GUI.Label(new Rect(30, 200, 190, 35), $"Fill: {painter.fillPercentage:F0}%", textStyle);
            painter.fillPercentage = GUI.HorizontalSlider(new Rect(220, 210, 170, 30), painter.fillPercentage, 0f, 100f);

            GUI.Label(new Rect(30, 250, 190, 35), $"Spacing: {painter.minSpacing:F1}m", textStyle);
            painter.minSpacing = GUI.HorizontalSlider(new Rect(220, 260, 170, 30), painter.minSpacing, 0f, 10f);

            GUI.Label(new Rect(30, 300, 190, 35), $"Min Scale: {painter.minScale:F1}x", textStyle);
            painter.minScale = GUI.HorizontalSlider(new Rect(220, 310, 170, 30), painter.minScale, 0.1f, 5f);

            GUI.Label(new Rect(30, 350, 190, 35), $"Max Scale: {painter.maxScale:F1}x", textStyle);
            painter.maxScale = GUI.HorizontalSlider(new Rect(220, 360, 170, 30), painter.maxScale, 0.1f, 5f);
            
            GUI.Label(new Rect(30, 400, 190, 35), $"Tilt (Rot): {painter.maxTiltAngle:F0}°", textStyle);
            painter.maxTiltAngle = GUI.HorizontalSlider(new Rect(220, 410, 170, 30), painter.maxTiltAngle, 0f, 90f);

            GUI.Label(new Rect(30, 450, 380, 35), $"<color=yellow>Active Set: {painter.GetActiveBrushName()}</color>", textStyle);
        }

        // Controls & Hotkeys Panel
        Rect controlsRect = new Rect(Screen.width - 340, 20, 320, 260);
        GUI.Box(controlsRect, "Controls & Hotkeys", boxStyle);

        float startX = Screen.width - 320;
        GUI.Label(new Rect(startX, 70, 300, 35), "<color=yellow><b>[</b></color> / <color=yellow><b>]</b></color> : Brush Size", textStyle);
        GUI.Label(new Rect(startX, 110, 300, 35), "<color=yellow><b>TAB</b></color> : Toggle Paint/Erase", textStyle);
        GUI.Label(new Rect(startX, 150, 300, 35), "<color=yellow><b>W A S D</b></color> : Move Camera", textStyle);
        GUI.Label(new Rect(startX, 190, 300, 35), "<color=yellow><b>Right-Click</b></color> : Look Around", textStyle);
        GUI.Label(new Rect(startX, 230, 300, 35), "<color=yellow><b>Left-Click</b></color> : Use Brush", textStyle);
    }
}
using UnityEngine;

[System.Serializable]
public struct BrushSet
{
    public string setName;
    public GameObject[] prefabs;
}

public class AssetPainter : MonoBehaviour
{
    [Header("Asset Library")]
    public BrushSet[] brushCategories; 
    public int activeBrushIndex = 0;   

    [Header("Placement Rules")]
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

    [Header("Dependencies")]
    public InventoryManager inventoryManager; // Link to the UI boss!

    private Transform assetContainer;
    private float actionTimer = 0f;
    private int placedAssetsLayerIndex;

    void Start()
    {
        GameObject container = new GameObject("Painted_Assets_Container");
        assetContainer = container.transform;
        placedAssetsLayerIndex = LayerMask.NameToLayer("PlacedAssets");

        if (inventoryManager != null) inventoryManager.GenerateInitialUI();

        Camera mainCam = Camera.main;
        if (mainCam != null && placedAssetsLayerIndex != -1)
        {
            float[] distances = new float[32];
            distances[placedAssetsLayerIndex] = 150f; 
            mainCam.layerCullDistances = distances;
        }
    }

    public void SetActiveBrush(int index)
    {
        if (brushCategories != null && index >= 0 && index < brushCategories.Length)
            activeBrushIndex = index;
    }

    public string GetActiveBrushName()
    {
        if (brushCategories == null || brushCategories.Length == 0) return "No Brushes Setup!";
        return brushCategories[activeBrushIndex].setName;
    }

    // --- NEW: Forces every single piece of the 3D model onto our specific layer ---
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    public void TryPaint(Vector3 center, float radius)
    {
        actionTimer -= Time.deltaTime;
        if (actionTimer > 0f) return;

        if (brushCategories == null || brushCategories.Length == 0) return;
        
        GameObject[] currentPrefabs = brushCategories[activeBrushIndex].prefabs;
        if (currentPrefabs == null || currentPrefabs.Length == 0) return;

        // --- 100% PERFECT PACKING MODE ---
        if (fillPercentage >= 99.5f)
        {
            // We use a minimum step of 0.25f so the web browser doesn't freeze if Spacing is 0!
            float step = Mathf.Max(minSpacing, 0.25f); 
            
            // Scan a mathematical grid over the entire brush circle
            for (float x = -radius; x <= radius; x += step)
            {
                for (float z = -radius; z <= radius; z += step)
                {
                    // Check if this specific grid point is inside the brush circle
                    if (x * x + z * z <= radius * radius) 
                    {
                        Vector3 spawnPos = new Vector3(center.x + x, 0, center.z + z);
                        AttemptPlacement(spawnPos, currentPrefabs);
                    }
                }
            }
        }
        // --- RANDOM SCATTER MODE (< 100%) ---
        else 
        {
            // Scale how many things we try to spawn based on the fill percentage
            int attempts = Mathf.RoundToInt(spawnAttemptsPerTick * (fillPercentage / 100f));
            if (attempts < 1) attempts = 1;

            for (int i = 0; i < attempts; i++)
            {
                Vector2 randomPoint = Random.insideUnitCircle * radius;
                Vector3 spawnPos = new Vector3(center.x + randomPoint.x, 0, center.z + randomPoint.y);
                AttemptPlacement(spawnPos, currentPrefabs);
            }
        }
        
        actionTimer = actionRate; 
    }

    // This helper function keeps our Paint function clean!
    private void AttemptPlacement(Vector3 spawnPos, GameObject[] prefabs)
    {
        if (Terrain.activeTerrain != null)
            spawnPos.y = Terrain.activeTerrain.SampleHeight(spawnPos) + Terrain.activeTerrain.transform.position.y;

        // --- 🧠 NEW: SMART SPACING ---
        if (minSpacing > 0.01f && placedObjectsLayer != 0)
        {
            Collider[] hits = Physics.OverlapSphere(spawnPos, minSpacing, placedObjectsLayer);
            bool tooCloseToSameObject = false;
            string activeBrushName = brushCategories[activeBrushIndex].setName;

            foreach (Collider col in hits)
            {
                // Climb up the hierarchy just like the Eraser does
                Transform rootAsset = col.transform;
                while (rootAsset.parent != null && rootAsset.parent != assetContainer)
                {
                    rootAsset = rootAsset.parent;
                }

                // If the object we bumped into has the exact same name, trigger the spacing block!
                if (rootAsset != null && rootAsset.name == activeBrushName)
                {
                    tooCloseToSameObject = true;
                    break; // Stop checking, we already know we can't spawn here
                }
            }

            // Only cancel the placement if we bumped into our own clone
            if (tooCloseToSameObject) return; 
        }
        // -----------------------------

        GameObject prefabToSpawn = prefabs[Random.Range(0, prefabs.Length)];
        GameObject newAsset = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, assetContainer);

        // Stamp it with the Name Tag!
        newAsset.name = brushCategories[activeBrushIndex].setName;

        newAsset.SetActive(true);
        if (placedAssetsLayerIndex != -1) SetLayerRecursively(newAsset, placedAssetsLayerIndex);

        float rotY = randomSpinY ? Random.Range(0f, 360f) : 0f;
        float rotX = Random.Range(-maxTiltAngle, maxTiltAngle);
        float rotZ = Random.Range(-maxTiltAngle, maxTiltAngle);
        newAsset.transform.rotation = Quaternion.Euler(rotX, rotY, rotZ);
        
        float randomScale = Random.Range(minScale, maxScale);
        newAsset.transform.localScale = prefabToSpawn.transform.localScale * randomScale;

        Renderer[] renderers = newAsset.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            float lowestY = float.MaxValue;
            foreach (Renderer ren in renderers)
                if (ren.bounds.min.y < lowestY) lowestY = ren.bounds.min.y;
            newAsset.transform.position += new Vector3(0, spawnPos.y - lowestY, 0);
        }
    }

    public void TryErase(Vector3 center, float radius)
    {
        actionTimer -= Time.deltaTime;
        if (actionTimer > 0f) return;

        if (brushCategories == null || brushCategories.Length == 0) return;
        
        // Grab the name of the brush you currently have equipped in the UI
        string activeBrushName = brushCategories[activeBrushIndex].setName;

        Collider[] hitColliders = Physics.OverlapSphere(center, radius, placedObjectsLayer);
        foreach (Collider col in hitColliders) 
        {
            // Climb up the hierarchy from the collider to find the root object we spawned
            Transform rootAsset = col.transform;
            while (rootAsset.parent != null && rootAsset.parent != assetContainer)
            {
                rootAsset = rootAsset.parent;
            }

            // --- 🛡️ NEW: THE FILTER ---
            // Only attempt to delete it if its Name Tag perfectly matches your equipped brush!
            if (rootAsset != null && rootAsset.name == activeBrushName)
            {
                // Respect the "Flow/Fill" slider so we can just "thin out" the crowd if we want to
                if (Random.Range(0f, 100f) <= fillPercentage)
                {
                    Destroy(rootAsset.gameObject);
                }
            }
        }
        
        actionTimer = actionRate;
    }

    public void ResetTimer()
    {
        actionTimer = 0f;
    }

    public void AddCustomAssetToBrush(GameObject newModel, string customName)
    {
        if (newModel.GetComponent<Collider>() == null) newModel.AddComponent<BoxCollider>();

        Renderer[] renderers = newModel.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds totalBounds = renderers[0].bounds;
            foreach (Renderer r in renderers) totalBounds.Encapsulate(r.bounds);
            
            float maxDimension = Mathf.Max(totalBounds.size.x, totalBounds.size.y, totalBounds.size.z);
            if (maxDimension > 1f)
            {
                float scaleFactor = 1f / maxDimension;
                newModel.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
            }
        }

        newModel.SetActive(false);

        BrushSet newSet = new BrushSet();
        newSet.setName = customName;
        newSet.prefabs = new GameObject[] { newModel };

        if (brushCategories == null) brushCategories = new BrushSet[0];
        BrushSet[] newCategories = new BrushSet[brushCategories.Length + 1];
        for (int i = 0; i < brushCategories.Length; i++) newCategories[i] = brushCategories[i];
        
        newCategories[newCategories.Length - 1] = newSet;
        brushCategories = newCategories;

        int newIndex = brushCategories.Length - 1;
        activeBrushIndex = newIndex;

        // Tell the UI boss to make a new button!
        if (inventoryManager != null) inventoryManager.AddNewButton(newIndex, newModel);
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory UI")]
    public Transform inventoryPanel; 
    public GameObject inventoryButtonPrefab; 

    [Header("Dependencies")]
    public AssetPainter painter; 

    private Camera photoCamera;
    
    // Tracks button outlines for the UI highlight system
    private List<Outline> buttonOutlines = new List<Outline>(); 

    private Camera GetOrCreatePhotoCamera()
    {
        if (photoCamera != null) return photoCamera;
        
        GameObject camObj = new GameObject("Hidden_PhotoBooth_Camera");
        camObj.transform.position = new Vector3(0, -1000, -2.5f);
        photoCamera = camObj.AddComponent<Camera>();
        
        photoCamera.clearFlags = CameraClearFlags.SolidColor;
        photoCamera.backgroundColor = new Color(0, 0, 0, 0); 
        photoCamera.orthographic = true; 
        photoCamera.enabled = false; 

        GameObject lightObj = new GameObject("PhotoBooth_Light");
        lightObj.transform.position = new Vector3(2, -998, -2);
        lightObj.transform.LookAt(new Vector3(0, -1000, 0));
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.5f;

        return photoCamera;
    }

    private Sprite TakeSnapshot(GameObject model)
    {
        Camera pCam = GetOrCreatePhotoCamera();

        // Spawn a temporary physical clone of the model for the photoshoot
        GameObject tempModel = Instantiate(model);
        tempModel.transform.position = new Vector3(0, -1000, 0);
        tempModel.transform.rotation = Quaternion.Euler(15f, -45f, 0f); 
        tempModel.SetActive(true); 

        // Auto-frame the camera to fit the model's bounds
        Renderer[] renderers = tempModel.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);

            pCam.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z - 5f);
            
            float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            // Establish a minimum size so it doesn't zoom in too close on simple shapes like cubes
            if (maxExtent < 0.5f) maxExtent = 0.5f; 
            pCam.orthographicSize = maxExtent * 1.2f; 
        }

        RenderTexture rt = new RenderTexture(256, 256, 16);
        pCam.targetTexture = rt;
        
        pCam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
        tex.Apply();

        pCam.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
        
        // Destroy the temporary clone after the picture is taken
        Destroy(tempModel);

        return Sprite.Create(tex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
    }

    public void GenerateInitialUI()
    {
        if (inventoryPanel == null || inventoryButtonPrefab == null || painter == null) return;

        foreach (Transform child in inventoryPanel) Destroy(child.gameObject);
        buttonOutlines.Clear(); 

        for (int i = 0; i < painter.brushCategories.Length; i++)
        {
            GameObject coverModel = painter.brushCategories[i].prefabs.Length > 0 ? painter.brushCategories[i].prefabs[0] : null;
            AddNewButton(i, coverModel);
        }
        
        if (buttonOutlines.Count > 0) HighlightButton(0); 
    }

    public void AddNewButton(int index, GameObject modelToPhotograph)
    {
        if (inventoryPanel == null || inventoryButtonPrefab == null || painter == null) return;

        GameObject newBtnObj = Instantiate(inventoryButtonPrefab, inventoryPanel);
        newBtnObj.SetActive(true);

        // Add a disabled outline component for the selection highlight system
        Outline outline = newBtnObj.AddComponent<Outline>();
        outline.effectColor = Color.yellow;
        outline.effectDistance = new Vector2(4, -4);
        outline.enabled = false; 
        buttonOutlines.Add(outline); 
        
        int myIndex = buttonOutlines.Count - 1; 

        TextMeshProUGUI btnText = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null) btnText.gameObject.SetActive(false);

        Button btn = newBtnObj.GetComponent<Button>();
        if (btn != null) 
        {
            btn.onClick.AddListener(() => 
            {
                painter.SetActiveBrush(index);
                HighlightButton(myIndex); 
            });
        }

        if (modelToPhotograph != null)
        {
            Sprite snapshot = TakeSnapshot(modelToPhotograph);
            if (snapshot != null)
            {
                // Create a new UI Image object inside the button hierarchy
                GameObject iconObj = new GameObject("IconImage");
                iconObj.transform.SetParent(newBtnObj.transform, false);
                
                Image iconImage = iconObj.AddComponent<Image>();
                iconImage.sprite = snapshot;
                iconImage.preserveAspect = true; 
                
                // Set the size to fill the button with a 10% padding gap
                RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.1f, 0.1f);
                iconRect.anchorMax = new Vector2(0.9f, 0.9f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
            }
        }
        
        if (myIndex > 0) HighlightButton(myIndex);
    }

    private void HighlightButton(int selectedIndex)
    {
        for (int i = 0; i < buttonOutlines.Count; i++)
        {
            if (buttonOutlines[i] != null) buttonOutlines[i].enabled = (i == selectedIndex);
        }
    }
}
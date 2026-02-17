using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading.Tasks; // Required for Async
using GLTFast; //

public class CustomAssetImporter : MonoBehaviour
{
    [Header("Dependencies")]
    public AssetPainter painter;

    [DllImport("__Internal")]
    private static extern void OpenBrowserFilePicker();

    public void UploadCustomGLB()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            OpenBrowserFilePicker();
        #else
            Debug.LogWarning("File picking only works in the final WebGL Build!");
        #endif
    }

    public async void OnFileSelected(string data)
    {
        string[] parts = data.Split('|');
        if (parts.Length == 2)
        {
            await LoadAndImportGLB(parts[0], parts[1]);
        }
    }

    private async Task LoadAndImportGLB(string url, string fileName)
    {
        // 1. Create a GltfImport instance
        var gltf = new GltfImport(); 

        // 2. Load the asset from the URL
        bool success = await gltf.Load(url); 

        if (success)
        {
            // 3. Create a parent container for the new asset
            GameObject importedRoot = new GameObject(fileName.Replace(".glb", ""));
            
            // 4. Instantiate the main scene
            bool instSuccess = await gltf.InstantiateMainSceneAsync(importedRoot.transform);

            if (instSuccess && painter != null)
            {
                painter.AddCustomAssetToBrush(importedRoot, importedRoot.name);
            }
        }
        else
        {
            Debug.LogError("glTFast failed to load GLB from: " + url);
        }
    }
}
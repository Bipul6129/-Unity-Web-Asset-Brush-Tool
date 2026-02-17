using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using GLTFast;

public class CustomAssetImporter : MonoBehaviour
{
    [Header("Dependencies")]
    public AssetPainter painter;

    // Links to the JavaScript WebGL plugin (.jslib) to open the browser's native file dialog
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

    // Triggered externally by the browser via SendMessage() once a user selects a file
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
        var gltf = new GltfImport(); 
        bool success = await gltf.Load(url); 

        if (success)
        {
            // Create a clean root container named after the file to keep the hierarchy organized
            GameObject importedRoot = new GameObject(fileName.Replace(".glb", ""));
            
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
using UnityEditor;
using UnityEditor.U2D.PSD;
using UnityEngine;

/// Makes layered PSDs import one sprite per layer (same settings as the original PSDs).
public static class CozyImport
{
    static readonly string[] LayeredPsds =
    {
        "Assets/Art/Source/Props/PSD_Props_02.psd",
        "Assets/Art/Source/UI/PSD_DialogueBoxes.psd",
    };

    [MenuItem("Cozy Horror/Reimport Layered PSDs")]
    public static void MakeLayered()
    {
        foreach (var path in LayeredPsds)
        {
            if (AssetDatabase.GetImporterType(path) != typeof(PSDImporter))
            {
                AssetDatabase.SetImporterOverride<PSDImporter>(path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            var imp = AssetImporter.GetAtPath(path) as PSDImporter;
            if (imp == null) { Debug.LogError("[Cozy] Not a PSDImporter: " + path); continue; }
            imp.spriteImportMode = SpriteImportMode.Multiple;
            imp.useMosaicMode = true;
            imp.useCharacterMode = true;
            imp.SaveAndReimport();
            Debug.Log("[Cozy] Layered import: " + path);
        }
        EnablePhysicsShapes();
    }

    static readonly string[] WorldPsds =
    {
        "Assets/Art/Source/Rooms/PSD_House.psd",
        "Assets/Art/Source/Rooms/PSD_Bathroom.psd",
        "Assets/Art/Source/Rooms/PSD_Balcony.psd",
        "Assets/Art/Source/Props/PSD_Props_01.psd",
        "Assets/Art/Source/Props/PSD_Props_02.psd",
    };

    /// Gives every layer sprite an outline-shaped physics shape so PolygonCollider2D clicks are pixel-tight.
    [MenuItem("Cozy Horror/Enable Sprite Physics Shapes")]
    public static void EnablePhysicsShapes()
    {
        foreach (var path in WorldPsds)
        {
            var imp = AssetImporter.GetAtPath(path) as PSDImporter;
            if (imp == null) continue;
            var so = new SerializedObject(imp);
            var p = so.FindProperty("m_GeneratePhysicsShape");
            if (p == null) { Debug.LogError("[Cozy] m_GeneratePhysicsShape not found"); return; }
            if (p.boolValue) continue;
            p.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            imp.SaveAndReimport();
            Debug.Log("[Cozy] Physics shapes on: " + path);
        }
    }
}

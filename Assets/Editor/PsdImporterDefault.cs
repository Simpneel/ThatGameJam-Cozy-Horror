using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.PSD;
using UnityEngine;

/// Makes the 2D PSD Importer the default for any .psd added to the project from now on.
///
/// Unity has no setting for this: the PSD Importer package registers itself as an
/// optional "override" for .psd, so by default Unity uses the plain Texture importer.
/// This script watches for brand-new PSDs (no .meta file yet) and switches them over
/// on their first import. Existing PSDs keep the importer they already use, so the
/// flat full-screen images in Art/Source/Screens stay as single textures.
class PsdImporterDefault : AssetPostprocessor
{
    static readonly List<string> pending = new List<string>();

    void OnPreprocessAsset()
    {
        if (assetImporter.importSettingsMissing
            && assetPath.EndsWith(".psd", System.StringComparison.OrdinalIgnoreCase)
            && !(assetImporter is PSDImporter))
            pending.Add(assetPath);
    }

    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (pending.Count == 0) return;
        var paths = pending.ToArray();
        pending.Clear();

        // Switching importers queues a re-import of each file with the PSD Importer.
        foreach (var path in paths)
        {
            if (AssetDatabase.GetImporterType(path) == typeof(PSDImporter)) continue;
            AssetDatabase.SetImporterOverride<PSDImporter>(path);
            Debug.Log("[Cozy] New PSD set to the 2D PSD Importer: " + path);
        }
    }
}

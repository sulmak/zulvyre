using UnityEditor;

namespace Zulvyre.EditorTools
{
    public class HumanModelImport : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.Contains("Art3D/Humans/")) return;

            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace VillageDefense.EditorTools
{
    /// <summary>
    /// Outils de reparation des materiaux d'un FBX exporte de Blender.
    ///
    /// Rappel de ce que le FBX transporte reellement :
    ///   - une IMAGE branchee sur Base Color  -> OK, arrive dans Unity
    ///   - une couleur unie                   -> OK, arrive comme _BaseColor
    ///   - du procedural (Noise, ColorRamp)   -> PERDU, remplace par une couleur unie
    ///   - des couleurs de vertex             -> les donnees arrivent, mais URP/Lit
    ///                                           les IGNORE -> l'objet sort blanc
    ///
    /// D'ou les deux corrections proposees ici : extraire les materiaux pour pouvoir
    /// les editer, puis passer ceux qui ont des couleurs de vertex sur le shader
    /// VillageDefense/VertexColorLit.
    /// </summary>
    public static class MapMaterialTools
    {
        private const string VertexColorShader = "VillageDefense/VertexColorLit";

        // ---------------------------------------------------------------- 0. diagnostic

        [MenuItem("Village Defense/Map/0 - Diagnostic des materiaux", false, 100)]
        public static void Diagnose()
        {
            string path = PickModel();
            if (path == null) return;

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var materials = assets.OfType<Material>().ToList();
            var meshes = assets.OfType<Mesh>().ToList();

            var vertexColorMats = MaterialsUsingVertexColors(out int vcMeshes);

            var sb = new StringBuilder();
            sb.AppendLine($"=== {Path.GetFileName(path)} ===");
            sb.AppendLine($"{materials.Count} materiaux, {meshes.Count} meshes.");
            sb.AppendLine();

            foreach (var m in materials)
            {
                Texture tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                if (tex == null && m.HasProperty("_MainTex")) tex = m.GetTexture("_MainTex");

                Color col = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor")
                          : m.HasProperty("_Color") ? m.GetColor("_Color")
                          : Color.white;

                string verdict;
                if (tex != null) verdict = $"OK (texture '{tex.name}')";
                else if (vertexColorMats.Contains(m.name)) verdict = "COULEURS DE VERTEX ignorees par URP/Lit -> rend blanc/uni";
                else verdict = "aucune texture, couleur unie seulement";

                sb.AppendLine($"  {m.name,-18} shader={m.shader.name,-38} couleur={ToHex(col)}  -> {verdict}");
            }

            sb.AppendLine();
            sb.AppendLine($"{vcMeshes} meshes de la scene portent des couleurs de vertex.");
            sb.AppendLine($"Materiaux concernes : {(vertexColorMats.Count == 0 ? "aucun" : string.Join(", ", vertexColorMats))}");

            Debug.Log(sb.ToString());
        }

        // ---------------------------------------------------------------- 1. extraction

        [MenuItem("Village Defense/Map/1 - Extraire les materiaux (les rendre editables)", false, 101)]
        public static void ExtractMaterials()
        {
            string path = PickModel();
            if (path == null) return;

            if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            {
                Debug.LogError($"[Map] {path} n'est pas un modele.");
                return;
            }

            string folder = Path.Combine(Path.GetDirectoryName(path) ?? "Assets", "Materials").Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'), "Materials");

            int extracted = 0;
            foreach (var m in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().ToList())
            {
                string dest = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{m.name}.mat");
                string error = AssetDatabase.ExtractAsset(m, dest);
                if (string.IsNullOrEmpty(error)) extracted++;
                else Debug.LogWarning($"[Map] '{m.name}' non extrait : {error}");
            }

            importer.SaveAndReimport();
            AssetDatabase.Refresh();

            Debug.Log($"[Map] {extracted} materiaux extraits dans {folder}. Ils sont maintenant editables " +
                      "(couleur, shader) sans etre ecrases au reimport du FBX.");
        }

        // ---------------------------------------------------------------- 2. vertex colors

        [MenuItem("Village Defense/Map/2 - Appliquer le shader vertex color", false, 102)]
        public static void ApplyVertexColorShader()
        {
            var shader = Shader.Find(VertexColorShader);
            if (shader == null)
            {
                Debug.LogError($"[Map] Shader '{VertexColorShader}' introuvable. " +
                               "Verifie Assets/Art/Shaders/VertexColorLit.shader.");
                return;
            }

            var targets = MaterialsUsingVertexColors(out _);
            if (targets.Count == 0)
            {
                Debug.Log("[Map] Aucun materiau a convertir : aucun mesh de la scene n'a de couleurs de vertex.");
                return;
            }

            int changed = 0;
            int embedded = 0;

            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                foreach (var m in mr.sharedMaterials)
                {
                    if (m == null || !targets.Contains(m.name)) continue;
                    if (m.shader == shader) continue;

                    // Un materiau qui a deja une texture (terrain bake) rend correctement :
                    // le passer en vertex color multiplierait la texture par la couleur
                    // de vertex et fausserait le rendu.
                    if (HasBaseTexture(m)) continue;

                    // Un materiau encore dans le FBX n'est pas editable : il faut l'extraire d'abord.
                    string assetPath = AssetDatabase.GetAssetPath(m);
                    if (!assetPath.EndsWith(".mat", System.StringComparison.OrdinalIgnoreCase))
                    {
                        embedded++;
                        continue;
                    }

                    Color baseColor = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
                    Texture baseMap = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;

                    Undo.RecordObject(m, "Shader vertex color");
                    m.shader = shader;
                    m.SetColor("_BaseColor", baseColor);
                    if (baseMap != null) m.SetTexture("_BaseMap", baseMap);
                    m.SetFloat("_VertexColorStrength", 1f);
                    m.SetFloat("_VertexColorSRGB", 1f);
                    EditorUtility.SetDirty(m);
                    changed++;
                }
            }

            AssetDatabase.SaveAssets();

            if (embedded > 0)
                Debug.LogWarning($"[Map] {embedded} materiaux sont encore embarques dans le FBX (non editables). " +
                                 "Lance d'abord 'Map / 1 - Extraire les materiaux'.");

            Debug.Log($"[Map] {changed} materiaux passes sur {VertexColorShader}. " +
                      $"Concernes : {string.Join(", ", targets)}");
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>
        /// Noms des materiaux portes par au moins un mesh de la scene ayant des
        /// couleurs de vertex : ce sont eux qui rendent blanc sous URP/Lit.
        /// </summary>
        private static HashSet<string> MaterialsUsingVertexColors(out int meshCount)
        {
            var result = new HashSet<string>();
            var counted = new HashSet<Mesh>();
            meshCount = 0;

            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;

                var mesh = mf.sharedMesh;
                if (mesh.colors == null || mesh.colors.Length == 0) continue;

                if (counted.Add(mesh)) meshCount++;

                foreach (var m in mr.sharedMaterials)
                    if (m != null) result.Add(m.name);
            }

            return result;
        }

        /// <summary>Le FBX selectionne dans le Project, sinon le plus gros de Assets/Art.</summary>
        private static string PickModel()
        {
            foreach (var o in Selection.objects)
            {
                string p = AssetDatabase.GetAssetPath(o);
                if (!string.IsNullOrEmpty(p) && p.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                    return p;
            }

            var candidates = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => new FileInfo(p).Length)
                .ToList();

            if (candidates.Count == 0)
            {
                Debug.LogError("[Map] Aucun FBX trouve dans Assets/Art. Selectionne-le dans le Project puis relance.");
                return null;
            }

            return candidates[0];
        }

        private static bool HasBaseTexture(Material m)
        {
            if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null) return true;
            return m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null;
        }

        private static string ToHex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
    }
}

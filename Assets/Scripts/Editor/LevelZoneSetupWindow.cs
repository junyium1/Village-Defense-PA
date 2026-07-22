using System.Collections.Generic;
using System.Linq;
using Game;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VillageDefense.EditorTools
{
    /// <summary>
    /// Outil de setup de la zone jouable : cree le GameObject LevelZone, y range
    /// tout ce qui doit se deplacer avec le niveau, cable le placer et prepare le
    /// terrain de la map (collider + layer) pour qu'on puisse poser la zone dessus.
    ///
    /// Menu : Village Defense / Zone jouable...
    /// </summary>
    public class LevelZoneSetupWindow : EditorWindow
    {
        private const string ZoneRootName = "LevelZone";
        private const string TerrainLayerName = "Terrain";

        private Transform _gridParent;
        private Transform _sol;
        private Transform _spawn;
        private Transform _objective;
        private Transform _village;
        private Transform _turret;
        private Transform _cameraBounds;
        private MeshRenderer _terrain;
        private GameManager _gameManager;
        private PlacementSystem _placementSystem;

        private Vector2Int _sizeInCells = new Vector2Int(40, 40);
        private bool _setupTerrainCollider = true;
        private bool _addNavSurface = true;
        private bool _wireGameManager = true;
        private Vector2 _scroll;

        [MenuItem("Village Defense/Zone jouable...", false, 10)]
        public static void Open()
        {
            var win = GetWindow<LevelZoneSetupWindow>(true, "Zone jouable");
            win.minSize = new Vector2(460, 520);
            win.AutoDetect();
        }

        private void OnEnable() => AutoDetect();

        // ------------------------------------------------------------- detection

        private void AutoDetect()
        {
            _gridParent = Find("GridParent");
            _sol = Find("Sol");
            _spawn = Find("START1");
            _objective = FindByType<EnemyObjective>();
            _village = Find("Village");
            _turret = Find("Tourelle Placeholder");
            _cameraBounds = Find("CameraBounds");
            _gameManager = FindFirst<GameManager>();
            _placementSystem = FindFirst<PlacementSystem>();
            _terrain = FindTerrainRenderer();
        }

        private static Transform Find(string name)
        {
            foreach (var t in AllTransforms())
                if (t.name == name) return t;
            return null;
        }

        private static Transform FindByType<T>() where T : Component
        {
            var c = FindFirst<T>();
            return c != null ? c.transform : null;
        }

        private static T FindFirst<T>() where T : Component
        {
            var all = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return all.Length > 0 ? all[0] : null;
        }

        private static IEnumerable<Transform> AllTransforms()
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    yield return t;
        }

        /// <summary>
        /// Le terrain = le mesh le plus etendu de la scene, en ignorant les quads
        /// geants (ciel, plan d'eau) : un terrain sculpte a forcement beaucoup de
        /// sommets, un plan de fond en a 4.
        /// </summary>
        private const int MinTerrainVertices = 100;

        private static MeshRenderer FindTerrainRenderer()
        {
            MeshRenderer best = null;
            float bestArea = 0f;

            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                if (mf.sharedMesh.vertexCount < MinTerrainVertices) continue;

                var b = mr.bounds.size;
                float area = b.x * b.z;
                if (area <= bestArea) continue;

                bestArea = area;
                best = mr;
            }

            return best;
        }

        // ------------------------------------------------------------- UI

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.HelpBox(
                "Cree un GameObject 'LevelZone' et y range la grille + tout ce qui doit suivre " +
                "le niveau quand le joueur pose la zone (sol, spawn, objectif, village).\n\n" +
                "A lancer une seule fois, scene GameScene ouverte.",
                MessageType.Info);

            EditorGUILayout.Space();
            if (GUILayout.Button("Re-detecter les objets de la scene")) AutoDetect();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Objets a ranger dans la zone", EditorStyles.boldLabel);
            _gridParent = Field("Grille (GridParent)", _gridParent, true);
            _sol = Field("Sol / plateforme", _sol, false);
            _spawn = Field("Spawn ennemi (START1)", _spawn, false);
            _objective = Field("Objectif (EnemyObjective)", _objective, false);
            _village = Field("Village", _village, false);
            _turret = Field("Tourelle placeholder", _turret, false);
            _cameraBounds = Field("Limites camera", _cameraBounds, false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Reglages", EditorStyles.boldLabel);
            _sizeInCells = EditorGUILayout.Vector2IntField(
                new GUIContent("Taille (en cases)", "40x40 cases de 5 unites = 200x200 unites monde."),
                _sizeInCells);
            _addNavSurface = EditorGUILayout.Toggle(
                new GUIContent("Ajouter un NavMeshSurface", "Rebake limite au volume de la zone apres chaque pose."),
                _addNavSurface);
            _wireGameManager = EditorGUILayout.Toggle(
                new GUIContent("Cabler GameManager / PlacementSystem", "Ajoute le LevelZonePlacer et remplit les references."),
                _wireGameManager);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Terrain de la map", EditorStyles.boldLabel);
            _setupTerrainCollider = EditorGUILayout.Toggle(
                new GUIContent("Preparer le terrain", $"Ajoute un MeshCollider et met le mesh sur le layer '{TerrainLayerName}'."),
                _setupTerrainCollider);
            _terrain = (MeshRenderer)EditorGUILayout.ObjectField("Mesh du terrain", _terrain, typeof(MeshRenderer), true);
            if (_terrain != null)
                EditorGUILayout.LabelField(" ", $"{_terrain.bounds.size.x:F0} x {_terrain.bounds.size.z:F0} unites", EditorStyles.miniLabel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Cablage", EditorStyles.boldLabel);
            _gameManager = (GameManager)EditorGUILayout.ObjectField("GameManager", _gameManager, typeof(GameManager), true);
            _placementSystem = (PlacementSystem)EditorGUILayout.ObjectField("PlacementSystem", _placementSystem, typeof(PlacementSystem), true);

            EditorGUILayout.Space(12);

            using (new EditorGUI.DisabledScope(_gridParent == null))
            {
                if (GUILayout.Button("Configurer la zone jouable", GUILayout.Height(34)))
                    Configure();
            }

            if (_gridParent == null)
                EditorGUILayout.HelpBox("Impossible de trouver 'GridParent'. Assigne-le a la main.", MessageType.Error);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(_terrain == null))
            {
                if (GUILayout.Button("Analyser le terrain (ou la zone peut-elle tenir ?)"))
                    AnalyseTerrain();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Balaye le terrain et mesure le denivele sous une emprise de la taille de
        /// la zone. Repond a la question pratique : quelle tolerance mettre dans
        /// maxHeightDelta, et est-ce que la zone n'est pas simplement trop grande.
        /// </summary>
        private void AnalyseTerrain()
        {
            int mask = LayerMaskFor(TerrainLayerName);
            if (mask == 0)
            {
                Debug.LogError($"[Zone] Layer '{TerrainLayerName}' inexistant : lance d'abord la configuration.");
                return;
            }

            var bounds = _terrain.bounds;
            float cellSize = _gridParent != null && _gridParent.GetComponentInChildren<Grid>() != null
                ? _gridParent.GetComponentInChildren<Grid>().cellSize.x
                : 5f;

            float side = _sizeInCells.x * cellSize;
            float step = Mathf.Max(side / 8f, 25f);
            const int samples = 5;

            var deltas = new List<(float delta, Vector2 pos)>();

            for (float cx = bounds.min.x + side; cx <= bounds.max.x - side; cx += step)
            {
                for (float cz = bounds.min.z + side; cz <= bounds.max.z - side; cz += step)
                {
                    float min = float.PositiveInfinity, max = float.NegativeInfinity;
                    bool complete = true;

                    for (int ix = 0; ix < samples && complete; ix++)
                    {
                        for (int iz = 0; iz < samples; iz++)
                        {
                            float u = (float)ix / (samples - 1) - 0.5f;
                            float v = (float)iz / (samples - 1) - 0.5f;
                            var origin = new Vector3(cx + u * side, bounds.max.y + 50f, cz + v * side);

                            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                                    bounds.size.y + 200f, mask, QueryTriggerInteraction.Ignore))
                            {
                                complete = false;
                                break;
                            }

                            min = Mathf.Min(min, hit.point.y);
                            max = Mathf.Max(max, hit.point.y);
                        }
                    }

                    if (complete) deltas.Add((max - min, new Vector2(cx, cz)));
                }
            }

            if (deltas.Count == 0)
            {
                Debug.LogWarning("[Zone] Aucun point de terrain touche. Le mesh a-t-il bien un MeshCollider " +
                                 $"sur le layer '{TerrainLayerName}' ?");
                return;
            }

            deltas.Sort((a, b) => a.delta.CompareTo(b.delta));

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== Terrain vs zone {_sizeInCells.x}x{_sizeInCells.y} cases ({side:F0}x{side:F0} unites) ===");
            sb.AppendLine($"{deltas.Count} positions testees sur le terrain.");
            sb.AppendLine();

            foreach (float tol in new[] { 2.5f, 5f, 10f, 15f, 20f })
            {
                int count = deltas.Count(d => d.delta < tol);
                sb.AppendLine($"  maxHeightDelta = {tol,5:F1} -> {count,5} positions valides ({100f * count / deltas.Count:F0}% du terrain)");
            }

            sb.AppendLine();
            sb.AppendLine("Emplacements les plus plats :");
            foreach (var d in deltas.Take(5))
                sb.AppendLine($"  ({d.pos.x,7:F0}, {d.pos.y,7:F0})  denivele {d.delta:F1} u");

            if (deltas[0].delta > 10f)
                sb.AppendLine($"\nATTENTION : meme au meilleur endroit le denivele est de {deltas[0].delta:F1} u. " +
                              "La zone est trop grande pour le relief de cette map -> reduis sa taille en cases, " +
                              "ou aplanis un plateau dans Blender.");

            Debug.Log(sb.ToString());
        }

        private static Transform Field(string label, Transform value, bool required)
        {
            var content = new GUIContent(required ? label + " *" : label);
            return (Transform)EditorGUILayout.ObjectField(content, value, typeof(Transform), true);
        }

        // ------------------------------------------------------------- setup

        private void Configure()
        {
            Undo.SetCurrentGroupName("Setup zone jouable");
            int group = Undo.GetCurrentGroup();

            // 1. racine de la zone, centree sur la grille actuelle
            var existing = Object.FindFirstObjectByType<LevelZone>();
            GameObject rootGo;
            if (existing != null)
            {
                rootGo = existing.gameObject;
            }
            else
            {
                rootGo = new GameObject(ZoneRootName);
                Undo.RegisterCreatedObjectUndo(rootGo, "Creer LevelZone");
                rootGo.transform.position = _gridParent.position;
            }

            var zone = rootGo.GetComponent<LevelZone>();
            if (zone == null) zone = Undo.AddComponent<LevelZone>(rootGo);

            // 2. tout ce qui doit suivre le niveau devient enfant de la zone
            Reparent(_gridParent, rootGo.transform);
            Reparent(_sol, rootGo.transform);
            Reparent(_spawn, rootGo.transform);
            Reparent(_objective, rootGo.transform);
            Reparent(_village, rootGo.transform);
            Reparent(_turret, rootGo.transform);
            Reparent(_cameraBounds, rootGo.transform);

            // 3. terrain de la map : collider + layer dedie.
            //    A FAIRE AVANT le NavMeshSurface : c'est ici que le layer 'Terrain'
            //    est cree, et le masque de bake en a besoin.
            if (_setupTerrainCollider && _terrain != null) SetupTerrain(_terrain);

            // 4. NavMeshSurface sur la racine (scale 1 : le volume de bake reste juste)
            NavMeshSurface nav = null;
            if (_addNavSurface)
            {
                nav = rootGo.GetComponent<NavMeshSurface>();
                if (nav == null) nav = Undo.AddComponent<NavMeshSurface>(rootGo);

                nav.collectObjects = CollectObjects.Volume;
                nav.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
                nav.layerMask = LayerMaskFor("Ground", TerrainLayerName);

                // L'ancien surface sur le Sol ferait un double bake.
                if (_sol != null)
                {
                    var old = _sol.GetComponent<NavMeshSurface>();
                    if (old != null && old != nav) Undo.DestroyObjectImmediate(old);
                }
            }

            // 5. references de la zone
            var visual = _gridParent.Find("GridVisualization");
            var so = new SerializedObject(zone);
            SetRef(so, "grid", _gridParent.GetComponentInChildren<Grid>());
            SetRef(so, "visual", visual);
            SetRef(so, "visualRenderer", visual != null ? visual.GetComponent<MeshRenderer>() : null);
            SetRef(so, "placementSurface", visual != null ? visual.GetComponent<Collider>() : null);
            SetRef(so, "navSurface", nav);
            so.FindProperty("sizeInCells").vector2IntValue = _sizeInCells;
            so.ApplyModifiedPropertiesWithoutUndo();
            zone.ApplyVisualSize();

            // 6. placer + cablage
            if (_wireGameManager) WirePlacer(zone);

            EditorUtility.SetDirty(rootGo);
            EditorSceneManagerMarkDirty();
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = rootGo;

            Debug.Log("[LevelZone] Setup termine. Verifie le layer 'Terrain' sur le mesh de la map " +
                      "et le groundMask du LevelZonePlacer.", rootGo);
        }

        private void WirePlacer(LevelZone zone)
        {
            GameObject host = _gameManager != null ? _gameManager.gameObject : zone.gameObject;

            var placer = Object.FindFirstObjectByType<LevelZonePlacer>();
            if (placer == null) placer = Undo.AddComponent<LevelZonePlacer>(host);

            var pso = new SerializedObject(placer);
            SetRef(pso, "zone", zone);
            SetRef(pso, "cam", Camera.main);
            SetRef(pso, "cameraSystem", FindFirst<CameraSystem>());
            pso.FindProperty("groundMask").intValue = LayerMaskFor(TerrainLayerName);
            pso.ApplyModifiedPropertiesWithoutUndo();

            if (_gameManager != null)
            {
                var gso = new SerializedObject(_gameManager);
                SetRef(gso, "zonePlacer", placer);
                gso.ApplyModifiedPropertiesWithoutUndo();
            }

            if (_placementSystem != null)
            {
                var pss = new SerializedObject(_placementSystem);
                SetRef(pss, "levelZone", zone);
                pss.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetupTerrain(MeshRenderer terrain)
        {
            int layer = EnsureLayer(TerrainLayerName);
            if (layer >= 0)
            {
                Undo.RecordObject(terrain.gameObject, "Layer terrain");
                terrain.gameObject.layer = layer;
            }

            var mf = terrain.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;

            // Pas besoin de cocher Read/Write sur le FBX : Unity cuisine la collision
            // a l'import. Le forcer rechargerait les 3600+ meshes pour rien.
            if (terrain.GetComponent<MeshCollider>() == null)
            {
                var col = Undo.AddComponent<MeshCollider>(terrain.gameObject);
                col.sharedMesh = mf.sharedMesh;
            }
        }

        // ------------------------------------------------------------- helpers

        private static void Reparent(Transform child, Transform parent)
        {
            if (child == null || parent == null) return;
            if (child == parent || child.IsChildOf(parent)) return;
            Undo.SetTransformParent(child, parent, "Ranger dans la zone");
        }

        private static void SetRef(SerializedObject so, string property, Object value)
        {
            var p = so.FindProperty(property);
            if (p == null)
            {
                Debug.LogWarning($"[LevelZone] Propriete '{property}' introuvable sur {so.targetObject.GetType().Name}.");
                return;
            }
            p.objectReferenceValue = value;
        }

        private static int LayerMaskFor(params string[] names)
        {
            int mask = 0;
            foreach (var n in names)
            {
                int i = LayerMask.NameToLayer(n);
                if (i >= 0) mask |= 1 << i;
            }
            return mask;
        }

        /// <summary>Cree le layer s'il n'existe pas, dans le premier slot utilisateur libre.</summary>
        private static int EnsureLayer(string name)
        {
            int existingIndex = LayerMask.NameToLayer(name);
            if (existingIndex >= 0) return existingIndex;

            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset").First());
            var layers = tagManager.FindProperty("layers");

            for (int i = 8; i < layers.arraySize; i++)
            {
                var element = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(element.stringValue)) continue;

                element.stringValue = name;
                tagManager.ApplyModifiedProperties();
                return i;
            }

            Debug.LogError($"[LevelZone] Aucun slot de layer libre pour '{name}'.");
            return -1;
        }

        private static void EditorSceneManagerMarkDirty()
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}

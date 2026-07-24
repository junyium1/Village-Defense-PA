using System.Collections.Generic;
using UnityEngine;

namespace Game.Units
{
    /// <summary>Drop zone visuelle des ennemis : remplace le pion placeholder
    /// par un modele depose dans Resources/Enemies (glisser-deposer, zero code).
    /// enemy_default = requis ; enemy_boss = variante des niveaux boss (repli sur default).</summary>
    public static class EnemyVisuals
    {
        const string DefaultPath = "Enemies/enemy_default";
        const string BossPath = "Enemies/enemy_boss";
        const string VisualChildName = "Visual";
        const float HeightMultiplier = 1f;

        // Cache des modeles charges : un spawn par ennemi ne doit pas recharger l'asset.
        static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();

        /// <summary>Instancie le modele drop-in en enfant de l'ennemi et masque le pion.
        /// No-op si aucun modele n'est depose : le placeholder reste le visuel.</summary>
        public static void Apply(GameObject enemyRoot, bool bossLevel)
        {
            if (enemyRoot == null) return;
            Transform root = enemyRoot.transform;
            if (root.Find(VisualChildName) != null) return; // deja applique (securite)

            // Choix du modele : variante boss si dispo, sinon repli sur le defaut.
            GameObject model = bossLevel ? Load(BossPath) : null;
            if (model == null) model = Load(DefaultPath);
            if (model == null) return; // rien depose : on garde le pion

            // Hauteur du pion AVANT de le masquer : reference pour l'auto-scale.
            MeshRenderer placeholder = enemyRoot.GetComponent<MeshRenderer>();
            float targetHeight = 0f;
            if (placeholder != null && placeholder.bounds.size.y > 0f)
                targetHeight = placeholder.bounds.size.y;

            GameObject visual = Object.Instantiate(model, root);
            visual.name = VisualChildName;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            // Le pion est masque, jamais detruit : collider/logique intacts.
            if (placeholder != null) placeholder.enabled = false;

            // Auto-scale : le modele prend la hauteur du pion (drag-drop sans reglage).
            if (targetHeight > 0f)
            {
                Bounds b = ComputeLocalBounds(visual);
                if (b.size.y > 0f)
                    visual.transform.localScale = visual.transform.localScale * (targetHeight / b.size.y * HeightMultiplier);
            }
        }

        static GameObject Load(string path)
        {
            GameObject cached;
            if (cache.TryGetValue(path, out cached)) return cached;
            GameObject loaded = Resources.Load<GameObject>(path);
            cache[path] = loaded; // null memorise : pas de re-tentative a chaque spawn
            return loaded;
        }

        // Bounds combinees de tous les renderers de l'instance, en espace monde.
        static Bounds ComputeLocalBounds(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(instance.transform.position, Vector3.zero);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }
    }
}

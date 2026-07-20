using UnityEngine;

namespace Menus
{
    /// <summary>
    /// Base pour toute planche cliquable du menu 3D.
    /// Le BoxCollider est requis : c'est lui que le raycast de <see cref="Menu3DInput"/> touche.
    /// (RequireComponent avec un type concret — Collider étant abstrait, AddComponent échouerait.)
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class SignPlankBase : MonoBehaviour
    {
        /// <summary>Appelé par <see cref="Menu3DInput"/> quand la planche est cliquée.</summary>
        public virtual void OnClicked() { }
    }
}

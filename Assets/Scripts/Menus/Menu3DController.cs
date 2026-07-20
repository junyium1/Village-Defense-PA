using UnityEngine;

namespace Menus
{
    /// <summary>
    /// Navigation du menu principal 3D : panneau titre (signpost) /
    /// sélection de niveau / options. Un seul panneau actif à la fois.
    /// Les changements passent par une pirouette (<see cref="SignpostRotator"/>) si présente,
    /// sinon swap instantané. Wipe/Quit relayés au <see cref="StartMenuManager"/> existant.
    /// </summary>
    public class Menu3DController : MonoBehaviour
    {
        public static Menu3DController Instance { get; private set; }

        [SerializeField] GameObject signpostRoot;
        [SerializeField] GameObject levelSelectRoot;
        [SerializeField] GameObject optionsRoot;
        [SerializeField] StartMenuManager legacyMenu;
        [SerializeField] SignpostRotator rotator;
        [SerializeField] GameObject levelPage1;
        [SerializeField] GameObject levelPage2;

        SignLevelPlank[] _levelPlanks = new SignLevelPlank[0];
        GameObject _current;
        int _levelPage = 1;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (levelSelectRoot != null)
                _levelPlanks = levelSelectRoot.GetComponentsInChildren<SignLevelPlank>(true);
        }

        void Start()
        {
            SetActivePanels(signpostRoot);
            _current = signpostRoot;

            if (StartMenuManager.OpenLevelSelectOnStart)
            {
                StartMenuManager.OpenLevelSelectOnStart = false;
                ShowLevelSelect();
            }
        }

        public void ShowMain() { GoTo(signpostRoot, null); }
        public void ShowLevelSelect() { GoTo(levelSelectRoot, () => { ApplyLevelPage(1); RefreshLevelPlanks(); }); }
        public void ShowOptions() { GoTo(optionsRoot, null); }

        // Pagination des niveaux (page 1 ⇄ page 2) via la même pirouette que le menu.
        public void NextLevelPage() { GoToLevelPage(2); }
        public void PrevLevelPage() { GoToLevelPage(1); }

        void GoTo(GameObject target, System.Action after)
        {
            if (target == null || _current == target) return;

            System.Action swap = () =>
            {
                SetActivePanels(target);
                _current = target;
                if (after != null) after();
            };

            if (rotator != null && _current != null && Application.isPlaying)
                rotator.Flip(swap);
            else
                swap();
        }

        void SetActivePanels(GameObject show)
        {
            if (signpostRoot != null) signpostRoot.SetActive(show == signpostRoot);
            if (levelSelectRoot != null) levelSelectRoot.SetActive(show == levelSelectRoot);
            if (optionsRoot != null) optionsRoot.SetActive(show == optionsRoot);
        }

        void GoToLevelPage(int page)
        {
            if (_current != levelSelectRoot || _levelPage == page) return;

            System.Action swap = () =>
            {
                ApplyLevelPage(page);
                RefreshLevelPlanks();
            };

            if (rotator != null && Application.isPlaying)
                rotator.Flip(swap);
            else
                swap();
        }

        void ApplyLevelPage(int page)
        {
            _levelPage = page;
            if (levelPage1 != null) levelPage1.SetActive(page == 1);
            if (levelPage2 != null) levelPage2.SetActive(page == 2);
        }

        public void WipeSave()
        {
            if (legacyMenu != null) legacyMenu.WipeSave();
            RefreshLevelPlanks();
        }

        public void QuitGame()
        {
            if (legacyMenu != null) legacyMenu.QuitGame();
        }

        void RefreshLevelPlanks()
        {
            foreach (var plank in _levelPlanks) plank.Refresh();
        }
    }
}

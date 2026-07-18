using DiscordBridge.Controllers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiscordBridge.UI
{
    // Vue pure : ne parle qu'à LinkAccountController, jamais à DiscordAPIBridge/SessionStore directement.
    // Ouvert à la demande (LinkAccountMenuButton.Open), affiche l'état "lié" ou "non lié".
    public class LinkAccountScreen : MonoBehaviour
    {
        [SerializeField] LinkAccountController controller;

        [Header("Etat NON lie")]
        [SerializeField] GameObject notLinkedGroup;
        [SerializeField] TMP_InputField codeInput;
        [SerializeField] Button submitButton;

        [Header("Etat LIE")]
        [SerializeField] GameObject linkedGroup;
        [SerializeField] TextMeshProUGUI linkedInfoText;
        [SerializeField] Button unlinkButton;

        [Header("Commun")]
        [SerializeField] TextMeshProUGUI statusText;
        [SerializeField] Button closeButton;

        void Awake()
        {
            if (submitButton != null) submitButton.onClick.AddListener(OnSubmitClicked);
            if (unlinkButton != null) unlinkButton.onClick.AddListener(OnUnlinkClicked);
            if (closeButton != null) closeButton.onClick.AddListener(Close);

            if (submitButton == null)
                Debug.LogWarning("[DiscordBridge] LinkAccountScreen : aucun bouton de validation assigné.");
        }

        void OnDestroy()
        {
            if (submitButton != null) submitButton.onClick.RemoveListener(OnSubmitClicked);
            if (unlinkButton != null) unlinkButton.onClick.RemoveListener(OnUnlinkClicked);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }

        void OnEnable()
        {
            if (controller == null)
            {
                Debug.LogWarning("[DiscordBridge] LinkAccountScreen : aucun LinkAccountController assigné.");
                return;
            }

            controller.OnLinkSucceeded += HandleLinkSucceeded;
            controller.OnLinkFailed += HandleLinkFailed;
            controller.OnUnlinked += HandleUnlinked;

            SetStatus(string.Empty);
            SetInteractable(true);
            Refresh();
        }

        void OnDisable()
        {
            if (controller == null) return;

            controller.OnLinkSucceeded -= HandleLinkSucceeded;
            controller.OnLinkFailed -= HandleLinkFailed;
            controller.OnUnlinked -= HandleUnlinked;
        }

        // Ouvre l'ecran depuis un bouton du menu.
        public void Open()
        {
            gameObject.SetActive(true);
        }

        void Close()
        {
            gameObject.SetActive(false);
        }

        // Affiche le bon groupe selon l'etat de liaison courant.
        void Refresh()
        {
            bool linked = controller != null && controller.IsLinked;
            if (notLinkedGroup != null) notLinkedGroup.SetActive(!linked);
            if (linkedGroup != null) linkedGroup.SetActive(linked);
            if (linked && linkedInfoText != null)
                linkedInfoText.text = "Compte lie";
        }

        void OnSubmitClicked()
        {
            if (controller == null)
            {
                SetStatus("Erreur de configuration (controleur manquant).");
                return;
            }

            if (codeInput == null)
            {
                SetStatus("Erreur de configuration (champ de saisie manquant).");
                return;
            }

            if (!int.TryParse(codeInput.text, out int code) || code < 1000 || code > 9999)
            {
                SetStatus("Code invalide : entre 4 chiffres.");
                return;
            }

            SetStatus("Connexion en cours...", interactable: false);
            _ = controller.SubmitCodeAsync(code);
        }

        void OnUnlinkClicked()
        {
            if (controller != null)
                controller.Unlink();
        }

        void HandleLinkSucceeded()
        {
            SetStatus("Compte lie !");
            Refresh();
        }

        void HandleLinkFailed(string errorMessage) => SetStatus($"Erreur : {errorMessage}", interactable: true);

        void HandleUnlinked()
        {
            if (codeInput != null) codeInput.text = string.Empty;
            SetStatus("Compte delie.");
            Refresh();
        }

        void SetStatus(string message, bool interactable = true)
        {
            if (statusText != null) statusText.text = message;
            SetInteractable(interactable);
        }

        void SetInteractable(bool interactable)
        {
            if (submitButton != null) submitButton.interactable = interactable;
            if (codeInput != null) codeInput.interactable = interactable;
        }
    }
}

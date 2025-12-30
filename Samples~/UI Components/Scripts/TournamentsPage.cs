using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Multiversed;
using Multiversed.Models;
using Multiversed.Core;

/// <summary>
/// Tournaments page - displays horizontally scrollable tournament cards
/// Ready-to-use component for displaying tournaments in your game
/// </summary>
public class TournamentsPage : MonoBehaviour
{
    [Header("References")]
    public GameObject mainMenuPanel;
    public GameObject tournamentsPanel;

    // UI Elements
    private Text titleText;
    private Text statusText;
    private Text infoText;
    private Button backButton;
    private ScrollRect scrollRect;
    private GameObject contentContainer;

    // State
    private Dictionary<string, TournamentCard> cards = new Dictionary<string, TournamentCard>();
    private HashSet<string> registeredIds = new HashSet<string>();
    private Font font;

    // Constants
    private const float CARD_WIDTH = 280f;
    private const float CARD_HEIGHT = 320f;
    private const float CARD_SPACING = 20f;

    void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (tournamentsPanel != null)
        {
            tournamentsPanel.SetActive(false);
        }
    }

    public void Show()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (tournamentsPanel != null)
        {
            tournamentsPanel.SetActive(true);
            BuildUI();
            LoadTournaments();
        }
    }

    public void Hide()
    {
        if (tournamentsPanel != null)
            tournamentsPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    void BuildUI()
    {
        // Only build once
        if (titleText != null) return;

        // Clear any existing children
        foreach (Transform child in tournamentsPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // Panel background
        Image panelBg = tournamentsPanel.GetComponent<Image>();
        if (panelBg == null) panelBg = tournamentsPanel.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.09f, 0.12f, 1f);

        // Ensure panel fills screen
        RectTransform panelRect = tournamentsPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Create UI elements
        CreateTitle();
        CreateBackButton();
        CreateStatusText();
        CreateScrollArea();
        CreateInfoText();
    }

    void CreateTitle()
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(tournamentsPanel.transform, false);

        RectTransform rt = titleObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -20);
        rt.sizeDelta = new Vector2(0, 60);

        titleText = titleObj.AddComponent<Text>();
        titleText.font = font;
        titleText.fontSize = 36;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.text = "TOURNAMENTS";
    }

    void CreateBackButton()
    {
        GameObject btnObj = new GameObject("BackButton");
        btnObj.transform.SetParent(tournamentsPanel.transform, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(20, -90);
        rt.sizeDelta = new Vector2(120, 40);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = new Color(0.25f, 0.25f, 0.3f, 1f);

        backButton = btnObj.AddComponent<Button>();
        backButton.targetGraphic = btnBg;
        backButton.onClick.AddListener(Hide);

        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Text btnText = textObj.AddComponent<Text>();
        btnText.font = font;
        btnText.fontSize = 18;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.text = "← BACK";
    }

    void CreateStatusText()
    {
        GameObject statusObj = new GameObject("Status");
        statusObj.transform.SetParent(tournamentsPanel.transform, false);

        RectTransform rt = statusObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -140);
        rt.sizeDelta = new Vector2(0, 30);

        statusText = statusObj.AddComponent<Text>();
        statusText.font = font;
        statusText.fontSize = 18;
        statusText.color = Color.yellow;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.text = "Loading tournaments...";
    }

    void CreateScrollArea()
    {
        // Scroll View container
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(tournamentsPanel.transform, false);

        RectTransform scrollRt = scrollObj.AddComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0, 0);
        scrollRt.anchorMax = new Vector2(1, 1);
        scrollRt.offsetMin = new Vector2(0, 80);   // Bottom padding
        scrollRt.offsetMax = new Vector2(0, -180); // Top padding

        Image scrollBg = scrollObj.AddComponent<Image>();
        scrollBg.color = new Color(0.06f, 0.07f, 0.1f, 1f);

        scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.elasticity = 0.1f;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;

        // Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);

        RectTransform viewportRt = viewportObj.AddComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;

        viewportObj.AddComponent<RectMask2D>();
        Image viewportImg = viewportObj.AddComponent<Image>();
        viewportImg.color = Color.clear;

        scrollRect.viewport = viewportRt;

        // Content container
        contentContainer = new GameObject("Content");
        contentContainer.transform.SetParent(viewportObj.transform, false);

        RectTransform contentRt = contentContainer.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 0.5f);
        contentRt.anchorMax = new Vector2(0, 0.5f);
        contentRt.pivot = new Vector2(0, 0.5f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(100, CARD_HEIGHT);

        scrollRect.content = contentRt;
    }

    void CreateInfoText()
    {
        GameObject infoObj = new GameObject("Info");
        infoObj.transform.SetParent(tournamentsPanel.transform, false);

        RectTransform rt = infoObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 20);
        rt.sizeDelta = new Vector2(0, 50);

        infoText = infoObj.AddComponent<Text>();
        infoText.font = font;
        infoText.fontSize = 14;
        infoText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        infoText.alignment = TextAnchor.MiddleCenter;
        infoText.text = "Swipe to browse • Tap PARTICIPATE to join";
    }

    void LoadTournaments()
    {
        if (MultiversedSDK.Instance == null || !MultiversedSDK.Instance.IsInitialized)
        {
            statusText.text = "SDK not initialized!";
            statusText.color = Color.red;
            return;
        }

        statusText.text = "Loading tournaments...";
        statusText.color = Color.yellow;

        // Fetch both SPL and SOL tournaments
        List<Tournament> allTournaments = new List<Tournament>();
        int completedRequests = 0;
        int totalRequests = 2;
        string lastError = null;

        // Fetch SPL tournaments (tokenType 0)
        MultiversedSDK.Instance.GetTournaments(
            TokenType.SPL,
            onSuccess: (splTournaments) =>
            {
                allTournaments.AddRange(splTournaments);
                completedRequests++;
                CheckAndDisplayTournaments(allTournaments, completedRequests, totalRequests, lastError);
            },
            onError: (error) =>
            {
                Debug.LogWarning("[TournamentsPage] Failed to load SPL tournaments: " + error);
                lastError = error;
                completedRequests++;
                CheckAndDisplayTournaments(allTournaments, completedRequests, totalRequests, lastError);
            }
        );

        // Fetch SOL tournaments (tokenType 1)
        MultiversedSDK.Instance.GetTournaments(
            TokenType.SOL,
            onSuccess: (solTournaments) =>
            {
                allTournaments.AddRange(solTournaments);
                completedRequests++;
                CheckAndDisplayTournaments(allTournaments, completedRequests, totalRequests, lastError);
            },
            onError: (error) =>
            {
                Debug.LogWarning("[TournamentsPage] Failed to load SOL tournaments: " + error);
                lastError = error;
                completedRequests++;
                CheckAndDisplayTournaments(allTournaments, completedRequests, totalRequests, lastError);
            }
        );
    }

    void CheckAndDisplayTournaments(List<Tournament> tournaments, int completed, int total, string error)
    {
        // Wait for both requests to complete
        if (completed < total) return;

        if (tournaments.Count > 0)
        {
            statusText.text = "Found " + tournaments.Count + " tournament" + (tournaments.Count != 1 ? "s" : "");
            statusText.color = Color.green;

            ClearCards();
            StartCoroutine(CreateCardsWithDelay(tournaments));
        }
        else
        {
            if (!string.IsNullOrEmpty(error))
            {
                statusText.text = "Failed to load tournaments";
                statusText.color = Color.red;
                infoText.text = "Error: " + error;
            }
            else
            {
                statusText.text = "No tournaments available";
                statusText.color = Color.yellow;
                infoText.text = "No tournaments available. Check back later!";
            }
        }
    }

    IEnumerator CreateCardsWithDelay(System.Collections.Generic.List<Tournament> tournaments)
    {
        yield return null; // Wait one frame

        float xPosition = CARD_SPACING;

        foreach (var tournament in tournaments)
        {
            CreateCard(tournament, xPosition);
            xPosition += CARD_WIDTH + CARD_SPACING;
        }

        // Update content width
        RectTransform contentRt = contentContainer.GetComponent<RectTransform>();
        contentRt.sizeDelta = new Vector2(xPosition, CARD_HEIGHT);

        // Reset scroll position
        if (scrollRect != null)
        {
            scrollRect.horizontalNormalizedPosition = 0f;
        }
    }

    void CreateCard(Tournament tournament, float xPos)
    {
        GameObject cardObj = new GameObject("Card_" + tournament.id);
        cardObj.transform.SetParent(contentContainer.transform, false);

        RectTransform cardRt = cardObj.AddComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0, 0.5f);
        cardRt.anchorMax = new Vector2(0, 0.5f);
        cardRt.pivot = new Vector2(0, 0.5f);
        cardRt.anchoredPosition = new Vector2(xPos, 0);
        cardRt.sizeDelta = new Vector2(CARD_WIDTH, CARD_HEIGHT);

        TournamentCard card = cardObj.AddComponent<TournamentCard>();
        card.Initialize(tournament, font, OnParticipateClicked);

        if (registeredIds.Contains(tournament.id))
        {
            card.SetRegistered(true);
        }

        cards[tournament.id] = card;
    }

    void ClearCards()
    {
        foreach (var card in cards.Values)
        {
            if (card != null)
                Destroy(card.gameObject);
        }
        cards.Clear();
    }

    void OnParticipateClicked(Tournament tournament)
    {
        if (MultiversedSDK.Instance == null || !MultiversedSDK.Instance.IsInitialized)
        {
            ShowMessage("SDK not initialized!", Color.red);
            return;
        }

        if (!MultiversedSDK.Instance.IsWalletConnected)
        {
            ShowMessage("Please connect your wallet first!", Color.yellow);
            return;
        }

        if (registeredIds.Contains(tournament.id))
        {
            ShowMessage("Already registered for this tournament!", Color.yellow);
            return;
        }

        // Set card to registering state
        if (cards.ContainsKey(tournament.id))
        {
            cards[tournament.id].SetRegistering(true);
        }

        statusText.text = "Registering for tournament...";
        statusText.color = Color.yellow;

        MultiversedSDK.Instance.RegisterForTournament(
            tournament.id,
            tournament.tokenType,
            onSuccess: (signature) =>
            {
                registeredIds.Add(tournament.id);

                if (cards.ContainsKey(tournament.id))
                {
                    cards[tournament.id].SetRegistering(false);
                    cards[tournament.id].SetRegistered(true);
                }

                ShowMessage("Successfully registered for " + tournament.name + "!", Color.green);
            },
            onError: (error) =>
            {
                if (cards.ContainsKey(tournament.id))
                {
                    cards[tournament.id].SetRegistering(false);
                }

                ShowMessage("Registration failed: " + error, Color.red);
            }
        );
    }

    void ShowMessage(string message, Color color)
    {
        statusText.text = message;
        statusText.color = color;
    }

    // For external access
    public void ShowTournamentsPage() => Show();
}


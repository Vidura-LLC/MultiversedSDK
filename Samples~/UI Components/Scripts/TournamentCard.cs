using UnityEngine;
using UnityEngine.UI;
using System;
using Multiversed.Models;
using Multiversed.Core;

/// <summary>
/// UI component for displaying a tournament card with registration functionality
/// Ready-to-use component for displaying tournaments in your game
/// </summary>
public class TournamentCard : MonoBehaviour
{
    [Header("UI References")]
    private Text titleText;
    private Text statusText;
    private Text descriptionText;
    private Text entryFeeText;
    private Text participantsText;
    private Text countdownText;
    private Button participateButton;
    private Text participateButtonText;
    private Image cardBackground;
    private Image statusBadge;

    private Tournament tournament;
    private Action<Tournament> onParticipateClick;
    private bool isRegistering = false;
    private bool isRegistered = false;
    private Font uiFont;

    private void Update()
    {
        UpdateCountdown();
    }

    public void Initialize(Tournament tournamentData, Font font, Action<Tournament> participateCallback)
    {
        try
        {
            if (tournamentData == null)
            {
                Debug.LogError("[TournamentCard] Initialize called with null tournament!");
                throw new System.ArgumentNullException("tournamentData");
            }

            if (gameObject == null)
            {
                Debug.LogError("[TournamentCard] GameObject is null in Initialize!");
                throw new System.NullReferenceException("GameObject is null");
            }

            tournament = tournamentData;
            uiFont = font;
            onParticipateClick = participateCallback;

            // Ensure RectTransform exists
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
                if (rectTransform == null)
                {
                    throw new System.NullReferenceException("Failed to create RectTransform");
                }
            }

            CreateCardUI();
            UpdateCardData();
            
            Debug.Log("[TournamentCard] Successfully initialized card for: " + tournament.name);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[TournamentCard] Exception in Initialize: " + e.Message + "\n" + e.StackTrace);
            throw; // Re-throw so CreateTournamentCard can catch it
        }
    }

void CreateCardUI()
{
    if (tournament == null)
    {
        throw new System.NullReferenceException("Tournament is null in CreateCardUI");
    }

    RectTransform cardRect = GetComponent<RectTransform>();
    if (cardRect == null)
    {
        cardRect = gameObject.AddComponent<RectTransform>();
    }
    
    // Fixed card size - don't let it be stretched
    cardRect.sizeDelta = new Vector2(260, 280);

    // Card background
    cardBackground = gameObject.AddComponent<Image>();
    cardBackground.color = new Color(0.18f, 0.20f, 0.26f, 1f);

    // Vertical layout - control height so elements fit
    VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
    layout.spacing = 6;
    layout.padding = new RectOffset(10, 10, 10, 10);
    layout.childControlWidth = true;
    layout.childControlHeight = true;  // Changed to true
    layout.childForceExpandWidth = true;
    layout.childForceExpandHeight = false;

    // === TITLE ===
    GameObject titleObj = new GameObject("Title");
    titleObj.transform.SetParent(transform, false);
    titleText = titleObj.AddComponent<Text>();
    titleText.font = uiFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    titleText.fontSize = 16;
    titleText.fontStyle = FontStyle.Bold;
    titleText.color = Color.white;
    titleText.alignment = TextAnchor.MiddleLeft;

    LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
    titleLayout.minHeight = 24;
    titleLayout.preferredHeight = 24;

    // === STATUS BADGE ===
    GameObject statusObj = new GameObject("Status");
    statusObj.transform.SetParent(transform, false);
    statusBadge = statusObj.AddComponent<Image>();
    statusBadge.color = GetStatusColor(tournament.status);

    LayoutElement statusLayout = statusObj.AddComponent<LayoutElement>();
    statusLayout.minHeight = 20;
    statusLayout.preferredHeight = 20;

    // Status text inside badge
    GameObject statusTextObj = new GameObject("StatusText");
    statusTextObj.transform.SetParent(statusObj.transform, false);
    statusText = statusTextObj.AddComponent<Text>();
    statusText.font = uiFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    statusText.fontSize = 11;
    statusText.fontStyle = FontStyle.Bold;
    statusText.color = Color.white;
    statusText.alignment = TextAnchor.MiddleCenter;

    RectTransform statusTextRect = statusTextObj.GetComponent<RectTransform>();
    statusTextRect.anchorMin = Vector2.zero;
    statusTextRect.anchorMax = Vector2.one;
    statusTextRect.offsetMin = Vector2.zero;
    statusTextRect.offsetMax = Vector2.zero;

    // === TOKEN TYPE BADGE ===
    GameObject tokenTypeObj = new GameObject("TokenType");
    tokenTypeObj.transform.SetParent(transform, false);
    
    Image tokenTypeBadge = tokenTypeObj.AddComponent<Image>();
    tokenTypeBadge.color = tournament.tokenType == TokenType.SOL 
        ? new Color(0.2f, 0.6f, 1f, 1f) // Blue for SOL
        : new Color(0.6f, 0.3f, 0.9f, 1f); // Purple for SPL

    LayoutElement tokenTypeLayout = tokenTypeObj.AddComponent<LayoutElement>();
    tokenTypeLayout.minHeight = 18;
    tokenTypeLayout.preferredHeight = 18;

    // Token type text
    GameObject tokenTypeTextObj = new GameObject("TokenTypeText");
    tokenTypeTextObj.transform.SetParent(tokenTypeObj.transform, false);
    Text tokenTypeText = tokenTypeTextObj.AddComponent<Text>();
    tokenTypeText.font = uiFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    tokenTypeText.fontSize = 10;
    tokenTypeText.fontStyle = FontStyle.Bold;
    tokenTypeText.color = Color.white;
    tokenTypeText.alignment = TextAnchor.MiddleCenter;
    tokenTypeText.text = tournament.tokenType == TokenType.SOL ? "SOL" : "SPL";

    RectTransform tokenTypeTextRect = tokenTypeTextObj.GetComponent<RectTransform>();
    tokenTypeTextRect.anchorMin = Vector2.zero;
    tokenTypeTextRect.anchorMax = Vector2.one;
    tokenTypeTextRect.offsetMin = Vector2.zero;
    tokenTypeTextRect.offsetMax = Vector2.zero;

    // === DESCRIPTION ===
    GameObject descObj = new GameObject("Description");
    descObj.transform.SetParent(transform, false);
    descriptionText = descObj.AddComponent<Text>();
    descriptionText.font = uiFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    descriptionText.fontSize = 11;
    descriptionText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
    descriptionText.alignment = TextAnchor.UpperLeft;

    LayoutElement descLayout = descObj.AddComponent<LayoutElement>();
    descLayout.minHeight = 30;
    descLayout.preferredHeight = 30;
    descLayout.flexibleHeight = 1; // Take remaining space

    // === INFO SECTION ===
    GameObject infoSection = new GameObject("InfoSection");
    infoSection.transform.SetParent(transform, false);
    
    Image infoBg = infoSection.AddComponent<Image>();
    infoBg.color = new Color(0.12f, 0.14f, 0.18f, 1f);

    HorizontalLayoutGroup infoLayout = infoSection.AddComponent<HorizontalLayoutGroup>();
    infoLayout.spacing = 4;
    infoLayout.padding = new RectOffset(5, 5, 5, 5);
    infoLayout.childControlWidth = true;
    infoLayout.childControlHeight = true;
    infoLayout.childForceExpandWidth = true;

    LayoutElement infoLayoutElement = infoSection.AddComponent<LayoutElement>();
    infoLayoutElement.minHeight = 50;
    infoLayoutElement.preferredHeight = 50;

    // Entry Fee
    entryFeeText = CreateInfoItem(infoSection.transform, "Entry", tournament.entryFee.ToString("F2"));
    
    // Participants
    participantsText = CreateInfoItem(infoSection.transform, "Players", tournament.participantsCount.ToString());
    
    // Countdown
    countdownText = CreateInfoItem(infoSection.transform, "Time", "--:--:--");

    // === PARTICIPATE BUTTON ===
    GameObject btnObj = new GameObject("ParticipateButton");
    btnObj.transform.SetParent(transform, false);
    participateButton = btnObj.AddComponent<Button>();
    
    Image btnImage = btnObj.AddComponent<Image>();
    btnImage.color = new Color(0.2f, 0.6f, 0.9f, 1f);

    ColorBlock btnColors = participateButton.colors;
    btnColors.normalColor = new Color(0.2f, 0.6f, 0.9f, 1f);
    btnColors.highlightedColor = new Color(0.25f, 0.7f, 1f, 1f);
    btnColors.pressedColor = new Color(0.15f, 0.5f, 0.8f, 1f);
    btnColors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
    participateButton.colors = btnColors;

    LayoutElement btnLayout = btnObj.AddComponent<LayoutElement>();
    btnLayout.minHeight = 36;
    btnLayout.preferredHeight = 36;

    participateButtonText = new GameObject("Text").AddComponent<Text>();
    participateButtonText.transform.SetParent(btnObj.transform, false);
    
    RectTransform btnTextRect = participateButtonText.GetComponent<RectTransform>();
    btnTextRect.anchorMin = Vector2.zero;
    btnTextRect.anchorMax = Vector2.one;
    btnTextRect.offsetMin = Vector2.zero;
    btnTextRect.offsetMax = Vector2.zero;

    participateButtonText.font = uiFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    participateButtonText.fontSize = 14;
    participateButtonText.fontStyle = FontStyle.Bold;
    participateButtonText.color = Color.white;
    participateButtonText.alignment = TextAnchor.MiddleCenter;
    participateButtonText.text = "PARTICIPATE";

    participateButton.onClick.AddListener(() =>
    {
        if (!isRegistering && onParticipateClick != null)
        {
            onParticipateClick(tournament);
        }
    });
}

Text CreateInfoItem(Transform parent, string label, string value)
{
    GameObject item = new GameObject(label);
    item.transform.SetParent(parent, false);
    
    LayoutElement layout = item.AddComponent<LayoutElement>();
    layout.flexibleWidth = 1;

    VerticalLayoutGroup group = item.AddComponent<VerticalLayoutGroup>();
    group.spacing = 1;
    group.childControlWidth = true;
    group.childControlHeight = true;
    group.childAlignment = TextAnchor.MiddleCenter;

    // Label
    GameObject labelObj = new GameObject("Label");
    labelObj.transform.SetParent(item.transform, false);
    Text labelText = labelObj.AddComponent<Text>();
    labelText.font = uiFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    labelText.fontSize = 9;
    labelText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
    labelText.alignment = TextAnchor.MiddleCenter;
    labelText.text = label;

    // Value
    GameObject valueObj = new GameObject("Value");
    valueObj.transform.SetParent(item.transform, false);
    Text valueText = valueObj.AddComponent<Text>();
    valueText.font = uiFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    valueText.fontSize = 12;
    valueText.fontStyle = FontStyle.Bold;
    valueText.color = Color.white;
    valueText.alignment = TextAnchor.MiddleCenter;
    valueText.text = value;

    return valueText;
}    void UpdateCardData()
    {
        if (tournament == null) return;

        titleText.text = tournament.name;
        statusText.text = tournament.status;
        descriptionText.text = tournament.description;
        entryFeeText.text = tournament.entryFee.ToString("F2");
        participantsText.text = tournament.participantsCount.ToString();

        statusBadge.color = GetStatusColor(tournament.status);
        UpdateButtonState();
    }

    void UpdateCountdown()
    {
        if (tournament == null || countdownText == null) return;
        if (string.IsNullOrEmpty(tournament.startTime) || string.IsNullOrEmpty(tournament.endTime)) return;

        try
        {
            System.DateTime now = System.DateTime.Now;
            System.DateTime startTime = System.DateTime.Parse(tournament.startTime);
            System.DateTime endTime = System.DateTime.Parse(tournament.endTime);

        string countdown = "";
        string label = "";

        if (now < startTime)
        {
            // Tournament hasn't started
            System.TimeSpan remaining = startTime - now;
            countdown = string.Format("{0:D2}:{1:D2}:{2:D2}", 
                remaining.Hours, remaining.Minutes, remaining.Seconds);
            label = "Starts in";
        }
        else if (now < endTime)
        {
            // Tournament is active
            System.TimeSpan remaining = endTime - now;
            countdown = string.Format("{0:D2}:{1:D2}:{2:D2}", 
                remaining.Hours, remaining.Minutes, remaining.Seconds);
            label = "Ends in";
        }
        else
        {
            // Tournament ended
            countdown = "00:00:00";
            label = "Ended";
        }

        countdownText.text = countdown;
        
        // Update label
        Transform countdownLabel = countdownText.transform.parent.Find("Label");
        if (countdownLabel != null)
        {
            countdownLabel.GetComponent<Text>().text = label;
        }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[TournamentCard] Error updating countdown: " + e.Message);
            if (countdownText != null)
            {
                countdownText.text = "N/A";
            }
        }
    }

    Color GetStatusColor(string status)
    {
        if (string.IsNullOrEmpty(status))
        {
            return new Color(0.4f, 0.4f, 0.4f, 1f); // Default gray
        }

        switch (status.ToLower())
        {
            case "active":
                return new Color(0.2f, 0.7f, 0.3f, 1f); // Green
            case "ended":
            case "distributed":
            case "awarded":
                return new Color(0.7f, 0.2f, 0.2f, 1f); // Red
            case "not started":
                return new Color(0.5f, 0.5f, 0.5f, 1f); // Gray
            default:
                return new Color(0.4f, 0.4f, 0.4f, 1f);
        }
    }

    void UpdateButtonState()
    {
        if (participateButton == null || tournament == null) return;

        Image btnImage = participateButton.GetComponent<Image>();
        if (btnImage == null) return;

        if (isRegistered)
        {
            participateButton.interactable = false;
            participateButtonText.text = "✓ REGISTERED";
            btnImage.color = new Color(0.2f, 0.7f, 0.3f, 1f); // Green
            return;
        }

        bool canParticipate = tournament.IsOpenForRegistration && 
                             !isRegistering && 
                             System.DateTime.Now >= System.DateTime.Parse(tournament.startTime) &&
                             System.DateTime.Now < System.DateTime.Parse(tournament.endTime);

        participateButton.interactable = canParticipate;

        if (isRegistering)
        {
            participateButtonText.text = "REGISTERING...";
            btnImage.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Gray
        }
        else if (!canParticipate)
        {
            if (System.DateTime.Now < System.DateTime.Parse(tournament.startTime))
            {
                participateButtonText.text = "NOT STARTED";
                btnImage.color = new Color(0.4f, 0.4f, 0.4f, 1f); // Gray
            }
            else if (System.DateTime.Now >= System.DateTime.Parse(tournament.endTime))
            {
                participateButtonText.text = "ENDED";
                btnImage.color = new Color(0.5f, 0.2f, 0.2f, 1f); // Red
            }
            else
            {
                participateButtonText.text = "PARTICIPATE";
                btnImage.color = new Color(0.2f, 0.6f, 0.9f, 1f); // Blue
            }
        }
        else
        {
            participateButtonText.text = "PARTICIPATE";
            btnImage.color = new Color(0.2f, 0.6f, 0.9f, 1f); // Blue
        }
    }

    public void SetRegistering(bool registering)
    {
        isRegistering = registering;
        UpdateButtonState();
    }

    public void SetRegistered(bool registered)
    {
        isRegistered = registered;
        UpdateButtonState();
    }

    public Tournament GetTournament()
    {
        return tournament;
    }
}


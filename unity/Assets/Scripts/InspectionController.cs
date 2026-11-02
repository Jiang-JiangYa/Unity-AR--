using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[Serializable] public class IssuePosition { public float x, y, z; }
[Serializable] public class CreateIssue { public string title, description, priority; public IssuePosition position; }
[Serializable] public class IssueResponse { public string id, title, description, priority, status, createdAt, updatedAt; public IssuePosition position; }
[Serializable] public class ApiError { public string error; }

public class InspectionController : MonoBehaviour
{
    public ARRaycastManager raycasts;
    public ARPlaneManager planes;
    public string apiBaseUrl = "http://192.168.1.100:8080";
    readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    InputField titleInput, descriptionInput, urlInput;
    Text feedback, tracking, priorityLabel;
    Button submitButton, priorityButton;
    GameObject resultDialog;
    Text dialogTitle, dialogMessage;
    Button dialogConfirm;
    GameObject marker;
    Vector3 selectedPosition;
    bool busy;
    int priorityIndex = 1;
    readonly string[] priorities = { "low", "medium", "high" };
    Font font;

    void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUI();
        SetFeedback("Move phone slowly to scan a horizontal surface, then tap it.");
    }

    void Update()
    {
        if (tracking != null) tracking.text = "AR: " + ARSession.state + " | Planes: " + planes.trackables.count;
        var screen = Touchscreen.current;
        if (busy || DialogIsOpen || ARSession.state != ARSessionState.SessionTracking || screen == null) return;
        int activeTouches = 0;
        foreach (var contact in screen.touches) if (contact.press.isPressed) activeTouches++;
        var touch = screen.primaryTouch;
        if (activeTouches != 1 || !touch.press.wasPressedThisFrame) return;
        var position = touch.position.ReadValue();
        if (TouchIsUI(position, touch.touchId.ReadValue())) return;
        if (!raycasts.Raycast(position, hits, TrackableType.PlaneWithinPolygon))
        { SetFeedback("No plane here yet. Scan and tap a highlighted surface."); return; }
        selectedPosition = hits[0].pose.position;
        if (marker == null)
        {
            marker = new GameObject("Issue marker (current session)");
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.transform.SetParent(marker.transform, false);
            visual.transform.localScale = Vector3.one * 0.12f;
            visual.transform.localPosition = Vector3.up * 0.06f;
            Destroy(visual.GetComponent<Collider>());
            visual.GetComponent<Renderer>().material.color = new Color(1f, 0.35f, 0.1f);
        }
        // Store the surface hit, not the raised sphere centre.
        marker.transform.position = selectedPosition;
        SetFeedback($"Marker placed: {selectedPosition.x:F2}, {selectedPosition.y:F2}, {selectedPosition.z:F2} m");
    }

    bool TouchIsUI(Vector2 position, int touchId)
    {
        if (EventSystem.current == null) return false;
        // Explicit raycast avoids Update ordering issues with IsPointerOverGameObject alone.
        var pointer = new PointerEventData(EventSystem.current) { position = position, pointerId = touchId };
        uiHits.Clear();
        EventSystem.current.RaycastAll(pointer, uiHits);
        return uiHits.Count > 0;
    }

    void Submit()
    {
        if (busy || DialogIsOpen) return;
        if (string.IsNullOrWhiteSpace(titleInput.text)) { ShowDialog("Cannot submit", "Title is required."); return; }
        if (marker == null) { ShowDialog("Cannot submit", "Place a marker on a detected plane first."); return; }
        if (ARSession.state != ARSessionState.SessionTracking) { ShowDialog("Cannot submit", "AR tracking lost. Scan the scene again before submitting."); return; }
        string url = urlInput.text.Trim().TrimEnd('/');
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
        { ShowDialog("Cannot submit", "Enter a valid backend URL, e.g. http://192.168.1.100:8080"); return; }
        var input = new CreateIssue {
            title = titleInput.text.Trim(), description = descriptionInput.text,
            priority = priorities[priorityIndex],
            position = new IssuePosition { x = selectedPosition.x, y = selectedPosition.y, z = selectedPosition.z }
        };
        StartCoroutine(Send(url, input));
    }

    IEnumerator Send(string url, CreateIssue input)
    {
        SetBusy(true); ShowDialog("Submitting...", "Please wait while the issue is saved.", false);
        using (var req = new UnityWebRequest(url + "/api/issues", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(input)));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json"); req.timeout = 15;
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success && req.responseCode == 201)
            {
                IssueResponse saved = null;
                try { saved = JsonUtility.FromJson<IssueResponse>(req.downloadHandler.text); } catch (Exception) { }
                if (saved != null && !string.IsNullOrEmpty(saved.id))
                {
                    ShowDialog("Submitted successfully", "Your issue has been saved.\n\nID: " + saved.id);
                    SetFeedback("Scan and tap a plane to report another issue.");
                    Destroy(marker); marker = null;
                    titleInput.text = ""; descriptionInput.text = "";
                }
                else ShowDialog("Response not confirmed", "Unexpected response. Check admin before retrying to avoid a duplicate.");
            }
            else
            {
                string reason = req.error;
                try { var body = JsonUtility.FromJson<ApiError>(req.downloadHandler.text); if (!string.IsNullOrEmpty(body?.error)) reason = body.error; } catch (Exception) { }
                ShowDialog("Submission failed", reason + "\n\nYour input and marker have been kept. If timed out, check admin before retrying.");
            }
        }
        SetBusy(false);
    }

    void SetBusy(bool value)
    {
        busy = value; submitButton.interactable = !value; priorityButton.interactable = !value;
        titleInput.interactable = !value; descriptionInput.interactable = !value; urlInput.interactable = !value;
    }
    void SetFeedback(string message) { feedback.text = message; }

    bool DialogIsOpen => resultDialog != null && resultDialog.activeSelf;

    void ShowDialog(string title, string message, bool canConfirm = true)
    {
        titleInput.DeactivateInputField(); descriptionInput.DeactivateInputField(); urlInput.DeactivateInputField();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        dialogTitle.text = title; dialogMessage.text = message;
        dialogConfirm.gameObject.SetActive(canConfirm);
        resultDialog.SetActive(true);
        resultDialog.transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        dialogMessage.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 1f;
    }

    void BuildResultDialog(Transform canvas)
    {
        resultDialog = UIObject("Submission dialog", canvas);
        var overlay = resultDialog.GetComponent<RectTransform>();
        overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        // Full-screen raycast target blocks touches on both the form and AR scene.
        resultDialog.AddComponent<Image>().color = new Color(0, 0, 0, .65f);
        var card = UIObject("Dialog card", resultDialog.transform);
        var rect = card.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.06f, .28f); rect.anchorMax = new Vector2(.94f, .72f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        card.AddComponent<Image>().color = new Color(.06f, .12f, .17f, 1);
        var layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 24, 24); layout.spacing = 16;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        var heading = UIObject("Heading", card.transform); Height(heading, 70);
        dialogTitle = Label(heading.transform, "", 30); dialogTitle.fontStyle = FontStyle.Bold;
        var viewport = UIObject("Message viewport", card.transform);
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        viewport.AddComponent<RectMask2D>();
        var flexible = viewport.AddComponent<LayoutElement>(); flexible.minHeight = 100; flexible.flexibleHeight = 1;
        var scroll = viewport.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.viewport = viewport.GetComponent<RectTransform>();
        dialogMessage = Label(viewport.transform, "", 25);
        dialogMessage.alignment = TextAnchor.UpperLeft;
        dialogMessage.supportRichText = false;
        var content = dialogMessage.rectTransform;
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
        content.offsetMin = content.offsetMax = Vector2.zero;
        dialogMessage.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;
        dialogConfirm = ActionButton(card.transform, "OK", () => resultDialog.SetActive(false));
        resultDialog.SetActive(false);
    }

    GameObject UIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return go;
    }
    void Stretch(RectTransform rect)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(12, 4); rect.offsetMax = new Vector2(-12, -4); }
    Text Label(Transform parent, string value, int size)
    {
        var go = UIObject("Text", parent); var text = go.AddComponent<Text>();
        text.font = font; text.fontSize = size; text.text = value; text.color = Color.white;
        text.raycastTarget = false; text.alignment = TextAnchor.MiddleLeft; Stretch(go.GetComponent<RectTransform>()); return text;
    }
    void Height(GameObject go, float height) { var layout = go.AddComponent<LayoutElement>(); layout.minHeight = height; layout.preferredHeight = height; }
    InputField Field(Transform parent, string placeholder, string value, int height, bool multiline = false)
    {
        var go = UIObject(placeholder, parent); go.AddComponent<Image>().color = new Color(.95f,.97f,.98f); Height(go, height);
        var input = go.AddComponent<InputField>(); var text = Label(go.transform,"",24); text.color = new Color(.1f,.15f,.2f);
        var hint = Label(go.transform,placeholder,24); hint.color = Color.gray;
        input.textComponent = text; input.placeholder = hint; input.text = value; input.characterLimit = multiline ? 2000 : 200;
        input.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;
        return input;
    }
    Button ActionButton(Transform parent, string caption, UnityEngine.Events.UnityAction action)
    {
        var go = UIObject(caption,parent); go.AddComponent<Image>().color = new Color(.06f,.44f,.4f); Height(go,64);
        var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>();
        Label(go.transform,caption,26).alignment = TextAnchor.MiddleCenter; button.onClick.AddListener(action); return button;
    }
    void BuildUI()
    {
        var canvasObject = new GameObject("Inspection UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720,1280); scaler.matchWidthOrHeight = .5f;
        var safe = UIObject("Safe area",canvasObject.transform); var safeRect = safe.GetComponent<RectTransform>();
        var area = Screen.safeArea; safeRect.anchorMin = new Vector2(area.xMin/Screen.width,area.yMin/Screen.height);
        safeRect.anchorMax = new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);safeRect.offsetMin = safeRect.offsetMax = Vector2.zero;
        var top = UIObject("Tracking",safe.transform); var tr = top.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0,1); tr.anchorMax = Vector2.one; tr.pivot = new Vector2(.5f,1); tr.sizeDelta = new Vector2(0,70);
        top.AddComponent<Image>().color = new Color(.05f,.1f,.15f,.85f); tracking = Label(top.transform,"Starting AR...",22);
        var panel = UIObject("Form",safe.transform); var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(1,0);rect.pivot = new Vector2(.5f,0);rect.sizeDelta = new Vector2(0,570);
        panel.AddComponent<Image>().color = new Color(.05f,.1f,.15f,.94f);
        var layout = panel.AddComponent<VerticalLayoutGroup>(); layout.padding = new RectOffset(20,20,12,12);layout.spacing=8;
        layout.childControlHeight=true;layout.childControlWidth=true;layout.childForceExpandHeight=false;
        urlInput = Field(panel.transform,"Backend URL",apiBaseUrl,52);
        titleInput = Field(panel.transform,"Issue title (required)","",56);
        descriptionInput = Field(panel.transform,"Description (optional)","",80,true);
        priorityButton = ActionButton(panel.transform,"Priority: medium",()=>{priorityIndex=(priorityIndex+1)%3;priorityLabel.text="Priority: "+priorities[priorityIndex];});
        priorityLabel = priorityButton.GetComponentInChildren<Text>();
        submitButton = ActionButton(panel.transform,"Submit issue",Submit);
        var message = UIObject("Feedback",panel.transform);Height(message,140);feedback=Label(message.transform,"",22);
        BuildResultDialog(canvasObject.transform);
    }
}

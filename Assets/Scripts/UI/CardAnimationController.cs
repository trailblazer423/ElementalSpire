using System;
using System.Collections;
using ElementalSpire.Cards;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 通用卡牌动画控制器。项目未安装 DOTween 时使用无依赖补间，
/// 接口保持 Hover / Select / PlayCard / AttackEffect / ResetAnimation。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class CardAnimationController : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Hover")]
    [SerializeField, Min(0f)] private float hoverHeight = 55f;
    [SerializeField, Min(1f)] private float hoverScale = 1.18f;
    [SerializeField, Min(0.01f)] private float hoverDuration = 0.16f;
    [SerializeField, Min(0f)] private float floatingAmplitude = 4f;
    [SerializeField, Min(0f)] private float floatingSpeed = 2.4f;

    [Header("Selected")]
    [SerializeField] private Color hoverGlowColor = new Color(1f, 0.82f, 0.24f, 0.95f);
    [SerializeField] private Color selectedGlowColor = new Color(0.2f, 0.72f, 1f, 1f);
    [SerializeField, Min(0f)] private float selectedShake = 2.2f;
    [SerializeField, Min(0f)] private float haloRotationSpeed = 70f;

    [Header("Play Card")]
    [SerializeField, Min(0.05f)] private float playDuration = 0.42f;
    [SerializeField, Min(1f)] private float playScale = 1.32f;
    [SerializeField] private float playRotation = 420f;
    [SerializeField, Range(0f, 1f)] private float arcHeight = 0.24f;

    [Header("Optional VFX")]
    [SerializeField] private GameObject hoverVfxPrefab;
    [SerializeField] private GameObject playVfxPrefab;

    private RectTransform _rect;
    private Canvas _canvas;
    private Outline _glow;
    private RectTransform _halo;
    private Image[] _sparkles;
    private GameObject _hoverVfxInstance;
    private CardData _cardData;
    private CardAnimationProfile _profile;
    private Vector2 _restPosition;
    private Vector3 _restScale = Vector3.one;
    private Quaternion _restRotation = Quaternion.identity;
    private bool _poseCaptured;
    private bool _hovered;
    private bool _selected;
    private bool _dragging;
    private bool _playing;
    private bool _interactable = true;

    public bool IsPlaying => _playing;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        EnsureGlowAndHalo();
    }

    public void Initialize(CardData cardData)
    {
        _cardData = cardData;
        _profile = CardAnimationProfile.Resolve(cardData);
        ApplyProfile(_profile);
        EnsureGlowAndHalo();
        // CardView 创建后，BattleUI/牌组界面还会继续设置卡牌位置。
        // 此处不能提前记录 (0,0)，应由布局完成后或首次交互时再捕获。
        _poseCaptured = false;
    }

    /// <summary>布局系统设置完卡牌位置后，刷新动画恢复点。</summary>
    public void SetRestPoseFromCurrent()
    {
        if (_playing || _dragging)
            return;
        CaptureRestPose();
    }

    private void ApplyProfile(CardAnimationProfile profile)
    {
        if (profile == null)
        {
            hoverVfxPrefab = _cardData != null ? _cardData.hoverVfxPrefab : null;
            playVfxPrefab = _cardData != null ? _cardData.playVfxPrefab : null;
            return;
        }

        hoverHeight = profile.hoverHeight;
        hoverScale = profile.hoverScale;
        hoverDuration = profile.hoverDuration;
        floatingAmplitude = profile.floatingAmplitude;
        floatingSpeed = profile.floatingSpeed;
        hoverGlowColor = profile.hoverGlowColor;
        selectedGlowColor = profile.selectedGlowColor;
        selectedShake = profile.selectedShake;
        haloRotationSpeed = profile.haloRotationSpeed;
        playDuration = profile.playDuration;
        playScale = profile.playScale;
        playRotation = profile.playRotation;
        arcHeight = profile.arcHeight;
        hoverVfxPrefab = profile.hoverVfxPrefab != null ? profile.hoverVfxPrefab : _cardData?.hoverVfxPrefab;
        playVfxPrefab = profile.playVfxPrefab != null ? profile.playVfxPrefab : _cardData?.playVfxPrefab;
    }

    private void LateUpdate()
    {
        if (_playing || _dragging || !_poseCaptured)
            return;

        bool raised = _hovered || _selected;
        float floatOffset = raised
            ? Mathf.Sin(Time.unscaledTime * floatingSpeed * Mathf.PI * 2f) * floatingAmplitude
            : 0f;
        float shake = _selected
            ? Mathf.Sin(Time.unscaledTime * 31f) * selectedShake
            : 0f;
        Vector2 targetPosition = _restPosition + new Vector2(shake, raised ? hoverHeight + floatOffset : 0f);
        Vector3 targetScale = _restScale * (raised ? hoverScale : 1f);
        Quaternion targetRotation = _restRotation * Quaternion.Euler(0f, 0f,
            _selected ? Mathf.Sin(Time.unscaledTime * 24f) * selectedShake : 0f);

        float response = 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.01f, hoverDuration));
        _rect.anchoredPosition = Vector2.Lerp(_rect.anchoredPosition, targetPosition, response);
        _rect.localScale = Vector3.Lerp(_rect.localScale, targetScale, response);
        _rect.localRotation = Quaternion.Slerp(_rect.localRotation, targetRotation, response);

        if (_halo != null && _halo.gameObject.activeSelf)
            _halo.Rotate(0f, 0f, -haloRotationSpeed * Time.unscaledDeltaTime);
        UpdateSparkles(raised);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_interactable || _playing || _dragging)
            return;
        Hover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_playing || _dragging)
            return;
        _hovered = false;
        if (!_selected)
            SetGlow(false, hoverGlowColor);
        SetHoverVfx(false);
    }

    public void Hover()
    {
        CaptureRestPoseIfNeeded();
        _hovered = true;
        transform.SetAsLastSibling();
        SetGlow(true, hoverGlowColor);
        SetHoverVfx(true);
    }

    public void Select()
    {
        if (!_interactable || _playing)
            return;
        CaptureRestPoseIfNeeded();
        _selected = true;
        _hovered = true;
        transform.SetAsLastSibling();
        SetGlow(true, selectedGlowColor);
        if (_halo != null)
            _halo.gameObject.SetActive(true);
        SetHoverVfx(true);
    }

    public void ResetAnimation()
    {
        if (_playing)
            return;
        _hovered = false;
        _selected = false;
        _dragging = false;
        SetGlow(false, hoverGlowColor);
        if (_halo != null)
            _halo.gameObject.SetActive(false);
        SetHoverVfx(false);
    }

    public void SetInteractable(bool interactable)
    {
        _interactable = interactable;
        if (!interactable && !_selected)
        {
            _hovered = false;
            SetGlow(false, hoverGlowColor);
            SetHoverVfx(false);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!_interactable || _playing)
            return;
        CaptureRestPoseIfNeeded();
        _dragging = true;
        _hovered = true;
        transform.SetAsLastSibling();
        SetGlow(true, selectedGlowColor);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging || _playing)
            return;
        RectTransform parentRect = _rect.parent as RectTransform;
        if (parentRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            _rect.anchoredPosition = localPoint;
            _rect.localScale = _restScale * hoverScale;
            _rect.localRotation = Quaternion.Euler(0f, 0f, -eventData.delta.x * 0.7f);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging)
            return;
        _dragging = false;
        _hovered = false;
        if (!_selected)
            SetGlow(false, hoverGlowColor);
    }

    public void PlayCard(Transform target, ElementType chosenElement, Action onImpact)
    {
        if (_playing)
            return;
        StartCoroutine(PlayCardRoutine(target, chosenElement, onImpact));
    }

    public void AttackEffect(Transform target, ElementType chosenElement)
    {
        VFXManager.EnsureExists().PlayAttackEffect(_cardData, chosenElement, target);
    }

    private IEnumerator PlayCardRoutine(Transform target, ElementType chosenElement, Action onImpact)
    {
        _playing = true;
        _interactable = false;
        _dragging = false;
        _selected = true;
        SetHoverVfx(false);
        SetGlow(true, selectedGlowColor);

        _canvas = _canvas != null ? _canvas : GetComponentInParent<Canvas>();
        RectTransform canvasRect = _canvas != null ? _canvas.transform as RectTransform : null;
        Camera eventCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _canvas.worldCamera
            : null;

        Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(eventCamera, _rect.position);
        Vector2 targetScreen = new Vector2(Screen.width * 0.5f, Screen.height * 0.62f);
        if (target != null)
        {
            Camera worldCamera = Camera.main;
            if (worldCamera != null)
                targetScreen = worldCamera.WorldToScreenPoint(target.position);
        }

        Vector2 startLocal = _rect.anchoredPosition;
        Vector2 targetLocal = targetScreen;
        if (canvasRect != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startScreen, eventCamera, out startLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetScreen, eventCamera, out targetLocal);
            _rect.SetParent(canvasRect, false);
            _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.anchoredPosition = startLocal;
            transform.SetAsLastSibling();
        }

        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;

        Vector2 control = Vector2.Lerp(startLocal, targetLocal, 0.5f)
            + Vector2.up * Mathf.Abs(targetLocal.y - startLocal.y + 220f) * arcHeight;
        float elapsed = 0f;
        float nextSpark = 0f;
        while (elapsed < playDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / playDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            Vector2 a = Vector2.Lerp(startLocal, control, eased);
            Vector2 b = Vector2.Lerp(control, targetLocal, eased);
            _rect.anchoredPosition = Vector2.Lerp(a, b, eased);
            _rect.localScale = Vector3.Lerp(_restScale * hoverScale, _restScale * playScale, eased);
            _rect.localRotation = Quaternion.Euler(0f, 0f, playRotation * eased);

            if (elapsed >= nextSpark && canvasRect != null)
            {
                CreateTrailSpark(canvasRect, _rect.anchoredPosition, chosenElement);
                nextSpark += 0.045f;
            }
            yield return null;
        }

        if (playVfxPrefab != null && target != null)
            Destroy(Instantiate(playVfxPrefab, target.position, Quaternion.identity), 3f);

        if (_cardData != null && _cardData.HasCardType(CardType.Attack))
            AttackEffect(target, chosenElement);
        else
            VFXManager.EnsureExists().PlayMagicEffect(_cardData, chosenElement, target);
        onImpact?.Invoke();
    }

    private void CaptureRestPoseIfNeeded()
    {
        if (!_poseCaptured || (!_hovered && !_selected && !_dragging))
            CaptureRestPose();
    }

    private void CaptureRestPose()
    {
        if (_rect == null)
            _rect = GetComponent<RectTransform>();
        _restPosition = _rect.anchoredPosition;
        _restScale = _rect.localScale;
        _restRotation = _rect.localRotation;
        _poseCaptured = true;
    }

    private void EnsureGlowAndHalo()
    {
        _glow = GetComponent<Outline>();
        if (_glow == null)
            _glow = gameObject.AddComponent<Outline>();
        _glow.effectDistance = new Vector2(5f, -5f);
        _glow.useGraphicAlpha = true;
        _glow.enabled = false;

        if (_halo == null)
        {
            GameObject haloObject = new GameObject("SelectedHalo", typeof(RectTransform));
            haloObject.transform.SetParent(transform, false);
            _halo = haloObject.GetComponent<RectTransform>();
            _halo.anchorMin = _halo.anchorMax = new Vector2(0.5f, 0.5f);
            _halo.sizeDelta = new Vector2(184f, 244f);
            CreateHaloEdge(_halo, new Vector2(0f, 120f), new Vector2(120f, 4f));
            CreateHaloEdge(_halo, new Vector2(0f, -120f), new Vector2(120f, 4f));
            CreateHaloEdge(_halo, new Vector2(-90f, 0f), new Vector2(4f, 150f));
            CreateHaloEdge(_halo, new Vector2(90f, 0f), new Vector2(4f, 150f));
            _halo.SetAsFirstSibling();
            _halo.gameObject.SetActive(false);
        }

        if (_sparkles == null)
        {
            _sparkles = new Image[8];
            for (int i = 0; i < _sparkles.Length; i++)
            {
                GameObject sparkle = new GameObject("HoverSpark_" + i, typeof(RectTransform), typeof(Image));
                sparkle.transform.SetParent(transform, false);
                Image image = sparkle.GetComponent<Image>();
                image.raycastTarget = false;
                image.color = new Color(1f, 0.85f, 0.3f, 0f);
                RectTransform rect = sparkle.GetComponent<RectTransform>();
                float angle = i * Mathf.PI * 2f / _sparkles.Length;
                rect.anchoredPosition = new Vector2(Mathf.Cos(angle) * 92f, Mathf.Sin(angle) * 122f);
                rect.sizeDelta = new Vector2(5f, 5f);
                _sparkles[i] = image;
            }
        }
    }

    private static void CreateHaloEdge(RectTransform parent, Vector2 position, Vector2 size)
    {
        GameObject edge = new GameObject("GlowEdge", typeof(RectTransform), typeof(Image));
        edge.transform.SetParent(parent, false);
        Image image = edge.GetComponent<Image>();
        image.color = new Color(0.2f, 0.72f, 1f, 0.7f);
        image.raycastTarget = false;
        RectTransform rect = edge.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void SetGlow(bool visible, Color color)
    {
        if (_glow == null)
            return;
        _glow.effectColor = color;
        _glow.enabled = visible;
        if (_halo != null)
        {
            foreach (Image image in _halo.GetComponentsInChildren<Image>(true))
                image.color = new Color(color.r, color.g, color.b, 0.72f);
        }
    }

    private void UpdateSparkles(bool visible)
    {
        if (_sparkles == null)
            return;
        for (int i = 0; i < _sparkles.Length; i++)
        {
            Image image = _sparkles[i];
            if (image == null) continue;
            float pulse = visible ? 0.2f + 0.55f * Mathf.PingPong(Time.unscaledTime * 1.7f + i * 0.17f, 1f) : 0f;
            Color baseColor = _selected ? selectedGlowColor : hoverGlowColor;
            image.color = new Color(baseColor.r, baseColor.g, baseColor.b, pulse);
            image.rectTransform.localScale = Vector3.one * (0.7f + pulse);
        }
    }

    private void SetHoverVfx(bool visible)
    {
        if (visible && _hoverVfxInstance == null && hoverVfxPrefab != null)
        {
            _hoverVfxInstance = Instantiate(hoverVfxPrefab, transform);
            _hoverVfxInstance.transform.localPosition = Vector3.zero;
        }
        if (_hoverVfxInstance != null)
            _hoverVfxInstance.SetActive(visible);
    }

    private static void CreateTrailSpark(RectTransform canvasRect, Vector2 position, ElementType element)
    {
        GameObject spark = new GameObject("CardTrailSpark", typeof(RectTransform), typeof(Image), typeof(TransientUiSpark));
        spark.transform.SetParent(canvasRect, false);
        RectTransform rect = spark.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(16f, 16f);
        Image image = spark.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = VFXManager.GetElementColor(element);
    }
}

/// <summary>卡牌飞行残影，自行淡出，避免依赖发起动画的卡牌对象。</summary>
public sealed class TransientUiSpark : MonoBehaviour
{
    private Image _image;
    private float _life;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    private void Update()
    {
        _life += Time.unscaledDeltaTime;
        transform.localScale = Vector3.one * (1f + _life * 2.4f);
        if (_image != null)
        {
            Color color = _image.color;
            color.a = Mathf.Clamp01(1f - _life / 0.28f);
            _image.color = color;
        }
        if (_life >= 0.28f)
            Destroy(gameObject);
    }
}

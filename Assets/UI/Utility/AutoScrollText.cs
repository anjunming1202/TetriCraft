using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Auto-scrolls a TextMeshProUGUI horizontally when its rendered width exceeds the viewport width.
/// - If text does NOT overflow, keep original alignment and do not scroll.
/// - If text DOES overflow, set horizontal alignment to Left (preserving vertical alignment when possible),
///   then start scrolling from left to right edge and back in a loop.
/// 
/// Enhancement: When content/layout changes, if overflow status remains "overflowing", do NOT restart the scroll:
/// - Continue scrolling from current position using a preserved scroll progress mapped into the new range,
///   so the text does not jump back to the start.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class AutoScrollText : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The TextMeshProUGUI to scroll. If null, will try to get one from this GameObject.")]
    public TextMeshProUGUI text;

    [Tooltip("The RectTransform that defines the visible viewport. If null, parent RectTransform is used.")]
    public RectTransform viewportRect;

    [Header("Scrolling Settings")]
    [Tooltip("Scroll speed in pixels per second.")]
    public float scrollSpeed = 80f;

    [Tooltip("Whether to pause at each end.")]
    public bool pauseAtEnds = true;

    [Tooltip("Pause duration (seconds) at each end).")]
    public float pauseDuration = 1.0f;

    [Tooltip("Smoothing time for movement. Use small values like 0.05 for snappier movement.")]
    public float smoothTime = 0.08f;

    [Tooltip("Delay before first scroll starts (seconds).")]
    public float startDelay = 0.25f;

    [Tooltip("If true, will only update scrolling while this GameObject and viewport are active/enabled.")]
    public bool onlyScrollWhenActive = true;

    // Internal state
    RectTransform _textRect;
    float _contentWidth;
    float _viewportWidth;

    float _leftX = 0f;
    float _rightX = 0f;

    // preserve original alignment so we can restore when not overflowing
    TextAlignmentOptions _originalAlignment;
    bool _isOverflowing = false;
    bool _wasOverflowing = false;

    Coroutine _scrollCoroutine;
    bool _isScrolling = false;

    // scroll progress in [0,1] where 0 == leftX, 1 == rightX (note rightX may be negative)
    float _scrollProgress = 0f;
    // direction true => moving toward rightX (text moves left), false => moving toward leftX
    bool _movingToRightEdge = true;

    void Reset()
    {
        text = GetComponent<TextMeshProUGUI>();
        if (text == null)
            text = GetComponentInChildren<TextMeshProUGUI>();
        if (viewportRect == null && transform.parent != null)
            viewportRect = transform.parent as RectTransform;
    }

    void Awake()
    {
        if (text == null)
        {
            text = GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                Debug.LogError("[AutoScrollText] No TextMeshProUGUI assigned or found.", this);
                enabled = false;
                return;
            }
        }

        _textRect = text.rectTransform;

        if (viewportRect == null)
        {
            if (transform.parent != null)
                viewportRect = transform.parent as RectTransform;
            else
                viewportRect = (_textRect.parent as RectTransform);
        }

        _originalAlignment = text.alignment;
    }

    void OnEnable()
    {
        // initial start (small delay for layout)
        RestartIfNeededDelayed();
    }

    void OnDisable()
    {
        StopScrolling();
    }

    void OnDestroy()
    {
        StopScrolling();
    }

    /// <summary>
    /// Call this when text content or layout changed to re-evaluate whether scrolling is necessary.
    /// The method will avoid resetting current scroll if overflow status remains true.
    /// </summary>
    public void Refresh()
    {
        RecalculateSizesAndHandleState(considerPreserveProgress: true);
    }

    void OnRectTransformDimensionsChange()
    {
        RecalculateSizesAndHandleState(considerPreserveProgress: true);
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        if (!onlyScrollWhenActive || (gameObject.activeInHierarchy && (viewportRect == null || viewportRect.gameObject.activeInHierarchy)))
        {
            // Lightweight check: if sizes changed, re-evaluate preserving progress if needed.
            float prevContent = _contentWidth;
            float prevViewport = _viewportWidth;
            RecalculateSizesSilent();
            if (!Mathf.Approximately(prevContent, _contentWidth) || !Mathf.Approximately(prevViewport, _viewportWidth))
            {
                RecalculateSizesAndHandleState(considerPreserveProgress: true);
            }
        }
    }

    #region Size & detection

    /// <summary>
    /// Recalculate sizes and perform appropriate start/stop behavior.
    /// If considerPreserveProgress == true and we were previously overflowing and still overflowing,
    /// do not restart the scroll; instead remap current progress into the new range.
    /// </summary>
    void RecalculateSizesAndHandleState(bool considerPreserveProgress)
    {
        if (text == null || _textRect == null || viewportRect == null) return;

        // measure viewport width
        _viewportWidth = viewportRect.rect.width;

        // Temporarily set alignment to original to measure natural width
        var prevAlign = text.alignment;
        text.alignment = _originalAlignment;
        _contentWidth = text.GetPreferredValues(float.PositiveInfinity, _textRect.rect.height).x;

        bool nowOverflowing = _contentWidth > _viewportWidth + 0.01f;
        _isOverflowing = nowOverflowing;

        if (nowOverflowing)
        {
            // switch to left-aligned horizontally (preserve vertical) so measurement of scroll range is reliable
            var leftAligned = ComputeLeftAlignedVariant(_originalAlignment);
            text.alignment = leftAligned;

            // recompute content width under left alignment
            _contentWidth = text.GetPreferredValues(float.PositiveInfinity, _textRect.rect.height).x;

            // compute new left/right bounds
            float newLeftX = 0f;
            float newRightX = _contentWidth > _viewportWidth ? -(_contentWidth - _viewportWidth) : 0f;

            // handle transitions
            if (_wasOverflowing && considerPreserveProgress)
            {
                // We were overflowing before, and still are: preserve progress/direction instead of restarting.
                // Compute new scroll progress based on current anchoredPosition.
                float currentX = GetTextAnchoredX();
                // If previous _leftX/_rightX are equal (edge-case), set progress to 0
                if (!Mathf.Approximately(_leftX, _rightX))
                {
                    _scrollProgress = Mathf.InverseLerp(_leftX, _rightX, currentX);
                    _scrollProgress = Mathf.Clamp01(_scrollProgress);
                }
                else
                {
                    _scrollProgress = 0f;
                }

                // Update left/right and set anchored position to mapped progress on the new range
                _leftX = newLeftX;
                _rightX = newRightX;

                float mappedX = Mathf.Lerp(_leftX, _rightX, _scrollProgress);
                SetTextAnchoredX(mappedX);

                // If coroutine not running, start it (we continue scrolling where we left)
                if (_scrollCoroutine == null)
                {
                    _scrollCoroutine = StartCoroutine(ScrollLoop());
                }
            }
            else
            {
                // Either not previously overflowing, or we don't preserve progress: start fresh
                _leftX = newLeftX;
                _rightX = newRightX;

                // Start scrolling (this will set alignment to left and place at left X initially)
                RestartScrollingFresh();
            }

            _wasOverflowing = true;
        }
        else
        {
            // not overflowing: restore original alignment and stop scrolling
            text.alignment = _originalAlignment;
            _isOverflowing = false;

            StopScrolling();

            // reset anchored position to left (consistent)
            _leftX = 0f;
            _rightX = 0f;
            SetTextAnchoredX(_leftX);

            _wasOverflowing = false;
        }

        // restore alignment if we temporarily set it earlier and ended up not overflowing (handled above).
        // else alignment already set to left.
    }

    // Silent recalculation used to detect changes without side-effects
    void RecalculateSizesSilent()
    {
        if (text == null || _textRect == null || viewportRect == null) return;
        var prevAlign = text.alignment;
        text.alignment = _originalAlignment;
        _contentWidth = text.GetPreferredValues(float.PositiveInfinity, _textRect.rect.height).x;
        _viewportWidth = viewportRect.rect.width;
        text.alignment = prevAlign;
    }

    #endregion

    #region Coroutine control

    void RestartIfNeededDelayed()
    {
        StopScrolling();
        _scrollCoroutine = StartCoroutine(RestartDelayedCoroutine(startDelay));
    }

    IEnumerator RestartDelayedCoroutine(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        RecalculateSizesAndHandleState(considerPreserveProgress: false);
    }

    void RestartScrollingFresh()
    {
        // Stop any existing coroutine and start from left side
        StopScrolling();

        // ensure left aligned
        var leftAligned = ComputeLeftAlignedVariant(_originalAlignment);
        text.alignment = leftAligned;

        // reset progress and direction
        _scrollProgress = 0f;
        _movingToRightEdge = true;

        // place at left
        SetTextAnchoredX(_leftX);

        // start loop
        _scrollCoroutine = StartCoroutine(ScrollLoop());
    }

    void StopScrolling()
    {
        if (_scrollCoroutine != null)
        {
            StopCoroutine(_scrollCoroutine);
            _scrollCoroutine = null;
        }
        _isScrolling = false;
    }

    #endregion

    #region Scrolling implementation

    IEnumerator ScrollLoop()
    {
        _isScrolling = true;

        // Ensure alignment is left for overflowing case
        var leftAligned = ComputeLeftAlignedVariant(_originalAlignment);
        text.alignment = leftAligned;

        // small frame to allow layout to settle
        yield return null;

        // optional initial delay
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        // If we started fresh, ensure anchored pos corresponds to _scrollProgress
        SetTextAnchoredX(Mathf.Lerp(_leftX, _rightX, _scrollProgress));

        while (true)
        {
            if (onlyScrollWhenActive)
            {
                while (!(gameObject.activeInHierarchy && (viewportRect == null || viewportRect.gameObject.activeInHierarchy)))
                {
                    yield return null;
                }
            }

            float targetX = _movingToRightEdge ? _rightX : _leftX;

            while (!Mathf.Approximately(GetTextAnchoredX(), targetX))
            {
                float currentX = GetTextAnchoredX();
                float delta = scrollSpeed * Time.deltaTime;
                float nextX = Mathf.MoveTowards(currentX, targetX, delta);

                float appliedX;
                if (smoothTime > 0f)
                {
                    float t = Mathf.Clamp01(Time.deltaTime / smoothTime);
                    appliedX = Mathf.Lerp(currentX, nextX, t);
                }
                else
                {
                    appliedX = nextX;
                }

                SetTextAnchoredX(appliedX);

                // update scroll progress (map into [0,1] for current left/right)
                if (!Mathf.Approximately(_leftX, _rightX))
                {
                    _scrollProgress = Mathf.InverseLerp(_leftX, _rightX, appliedX);
                    _scrollProgress = Mathf.Clamp01(_scrollProgress);
                }
                else
                {
                    _scrollProgress = 0f;
                }

                yield return null;
            }

            // snap exactly
            SetTextAnchoredX(targetX);
            _scrollProgress = _movingToRightEdge ? 1f : 0f;

            if (pauseAtEnds)
            {
                float timer = 0f;
                while (timer < pauseDuration)
                {
                    timer += Time.deltaTime;
                    yield return null;
                }
            }

            // flip direction
            _movingToRightEdge = !_movingToRightEdge;
        }
    }

    #endregion

    #region Helpers for anchoredPosition X

    float GetTextAnchoredX()
    {
        if (_textRect == null) return 0f;
        return _textRect.anchoredPosition.x;
    }

    void SetTextAnchoredX(float x)
    {
        if (_textRect == null) return;
        Vector2 ap = _textRect.anchoredPosition;
        if (!Mathf.Approximately(ap.x, x))
        {
            ap.x = x;
            _textRect.anchoredPosition = ap;
        }
    }

    #endregion

    #region Alignment helper

    /// <summary>
    /// Compute a left-aligned variant that preserves vertical alignment when possible.
    /// This is a best-effort mapping for common TextAlignmentOptions values.
    /// </summary>
    TextAlignmentOptions ComputeLeftAlignedVariant(TextAlignmentOptions original)
    {
        string name = original.ToString();

        if (name.Contains("Top"))
            return TextAlignmentOptions.TopLeft;
        if (name.Contains("Bottom"))
            return TextAlignmentOptions.BottomLeft;
        if (name.Contains("Baseline"))
            return TextAlignmentOptions.BaselineLeft;

        return TextAlignmentOptions.Left;
    }

    #endregion
}

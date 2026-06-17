using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Collections.AllocatorManager;

public class NextTetrominoUIController : MonoBehaviour
{
    [SerializeField] private RectTransform layoutRoot;

    [SerializeField] private bool isAnimated = true;
    [SerializeField] private float animationDuration = 0.4f;

    // Injected by PlayerGameManager; the Inspector field is a fallback for standalone use.
    [SerializeField] private TetrisManager tetrisManager;

    public void SetTetrisManager(TetrisManager tm) => tetrisManager = tm;

    [SerializeField] private RectTransform[] slotTransforms;
    [SerializeField] private TetrominoIcon[] icons;
    private RectTransform[] iconTransforms;

    [SerializeField] private float blockSize;

    public Vector2[] slotPositions = null;
    private Vector2 spacing;
    private Vector2[] iconPositions = null;

    private bool hasInit = false;

    public async UniTask InitialiseAsync()
    {
        // subscribe
        tetrisManager.OnTetrominoListInitialised += InitialisePanel;
        tetrisManager.OnStartedTurn += UpdatePanel;

        Canvas.ForceUpdateCanvases();
        await UniTask.WaitForEndOfFrame(tetrisManager);

        GetTetrominoTransformsInfo();
    }

    public void ClearTetrominoIcons()
    {
        foreach (var icon in icons)
        {
            icon.ClearIcons();
        }
    }

    public void GetTetrominoTransformsInfo()
    {
        if (hasInit)
            return; // only initialise once

        // record icon RectTransform reference
        iconTransforms = new RectTransform[icons.Length];
        for (int i = 0; i < icons.Length; i++)
            iconTransforms[i] = icons[i].GetComponent<RectTransform>();

        // get slot anchored positions
        slotPositions = new Vector2[icons.Length];
        for (int i = 0; i < icons.Length; i++)
            slotPositions[i] = slotTransforms[i].anchoredPosition;

        // get spacing between slots
        spacing = slotPositions[1] - slotPositions[0];
        Debug.Assert(((slotPositions[slotPositions.Length - 1] - slotPositions[0]) / (slotPositions.Length - 1) - spacing).magnitude < float.Epsilon,
            "Tetromino dispays are not equally spaced");

        // set icon anchored positions, at the slot centre
        iconPositions = new Vector2[icons.Length];
        for (int i = 2; i < icons.Length; i++)
        {
            iconTransforms[i].anchorMax = new Vector2(0.5f, 0.5f);
            iconTransforms[i].anchorMin = new Vector2(0.5f, 0.5f);
            iconTransforms[i].pivot = new Vector2(0.5f, 0.5f);
            iconTransforms[i].anchoredPosition = new Vector2(0, 0);
            iconPositions[i] = iconTransforms[i].anchoredPosition;
        }

        hasInit = true;
    }

    private void Dispose()
    {
        tetrisManager.OnTetrominoListInitialised -= InitialisePanel;
        tetrisManager.OnStartedTurn -= UpdatePanel;
    }

    private void InitialisePanel()
    {
        for (int i = 0; i < icons.Length; i++)
        {
            icons[i].Init(tetrisManager.nextTetrominos[i], blockSize);
        }
    }

    private void UpdatePanel()
    {
        bool isInit = icons[0].isInit;

        // update panel tetromino icons
        for (int i = 0; i < icons.Length; i++)
        {
            icons[i].Init(tetrisManager.nextTetrominos[i], blockSize);
        }

        // set animation
        if (isAnimated)
        {
            int offset = isInit ? 1 : 4;
            ScrollAnimationOnSet(offset).Forget();
        }
    }

    private async UniTask ScrollAnimationOnSet(int offset)
    {
        // offset
        for (int i = 0; i < icons.Length; i++)
        {
            iconTransforms[i].anchoredPosition = iconPositions[i] + spacing * offset;
        }

        // scrolling animation
        UniTask[] scrollAnimationTasks = new UniTask[icons.Length];
        for (int i = 0; i < icons.Length; i++)
        {
            var icon = icons[i];
            scrollAnimationTasks[i] = icon.gameObject.UIAnimator().MoveTo(iconPositions[i], animationDuration);
        }

        await UniTask.WhenAll(scrollAnimationTasks);
    }
}

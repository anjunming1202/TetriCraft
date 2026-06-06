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

    [SerializeField] private TetrisManager tetrisManager;
    [SerializeField] private TetrominoIcon[] icons;

    [SerializeField] private float blockSize;

    private RectTransform[] rectTransforms;
    public Vector2[] iconPositions = null;
    private Vector2 spacing;

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

        rectTransforms = new RectTransform[icons.Length];
        for (int i = 0; i < icons.Length; i++)
            rectTransforms[i] = icons[i].GetComponent<RectTransform>();

        iconPositions = new Vector2[icons.Length];
        for (int i = 0; i < icons.Length; i++)
            iconPositions[i] = rectTransforms[i].anchoredPosition;

        spacing = iconPositions[1] - iconPositions[0];
        Debug.Assert(((iconPositions[iconPositions.Length - 1] - iconPositions[0]) / (iconPositions.Length - 1) - spacing).magnitude < float.Epsilon,
            "Tetromino dispays are not equally spaced");

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
            rectTransforms[i].anchoredPosition = iconPositions[i] + spacing * offset;
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

// Scripts/SceneMgmt/SceneLoader.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class SceneLoader : PersistentSingleton<SceneLoader>
{
    protected override void Awake()
    {
        base.Awake();
    }

    public void LoadScene(string sceneName, Action onComplete = null)
    {
        Time.timeScale = 1f; // init timescale
        StartCoroutine(LoadRoutine(sceneName, onComplete));
    }

    IEnumerator LoadRoutine(string sceneName, Action onComplete)
    {
        // 显示 Loading Panel
        LoadingPanel loadingPanel = UIManager.Instance.ShowPanel<LoadingPanel>("Loading", "Loading Text ...");

        var async = SceneManager.LoadSceneAsync(sceneName);
        async.allowSceneActivation = false;

        float timer = 0f;
        float displayedProgress = 0f;
        float minShowTime = 0.8f; // 最小显示时间，避免闪烁

        while (!async.isDone || !async.allowSceneActivation)
        {
            // Unity's async.progress maxes at 0.9 until activation
            float targetProgress = Mathf.Clamp01(async.progress / 0.9f);
            // 平滑显示 progress
            displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, Time.deltaTime * 3f);
            loadingPanel.SetProgress(displayedProgress);

            timer += Time.deltaTime;

            if (async.progress >= 0.9f && displayedProgress >= 0.99f && timer >= minShowTime)
            {
                async.allowSceneActivation = true;
            }
            yield return null;
        }

        // margin for scene activation
        yield return new WaitForSeconds(0.1f);

        // 隐藏 Loading Panel
        UIManager.Instance.HidePanel("Loading");
        onComplete?.Invoke();
    }
}

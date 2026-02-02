using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using TMPro;
using UnityEngine.EventSystems;
using Unity.VisualScripting;
using System.Collections.Generic;

/// <summary>
/// RebindButtonUI (PlayerInput 版本)
/// 说明：
///  - 该脚本用于 UI 中单行按键重绑定（例如 Jump → Space）
///  - 依赖 PlayerInput 来切换 Action Map（推荐用法）
///  - 请按注释在 Inspector 配置所需引用（actionReference、playerInput、bindingText、statusText、rebindButton）
/// </summary>
public class RebindButtonUI : MonoBehaviour
{
    [Header("=== 必填 - 在 Inspector 里拖的字段 ===")]

    // 1) 拖入具体的 Action（例如：Gameplay -> Jump）
    //    在 Input Actions 资源里右键某个 Action，选择 "Create Input Action Reference"，或直接把 Action 从资源里拖进来。
    public InputActionReference actionReference; // JUST FOR FINDING THE ACTION NAME, NOT THE REFERENCE!

    // 2) 要重绑定的是 action 的第几个 binding（如果该 action 在 InputActions 里只有一个 binding，通常为 0）
    //    如果同一个 Action 在 InputActions 中绑定了多个设备（Keyboard / Gamepad），按索引选择要替换哪一个。
    public int bindingIndex = 0;

    // 3) 显示绑定名称的 Text（可以换成 TMP_Text，如果你用 TextMeshPro）
    public TextMeshProUGUI bindingText;

    [Header("=== PlayerInput 相关（必填或强烈推荐）")]

    // 5) 如果你的场景使用 PlayerInput（推荐），把 PlayerInput 组件拖进来。
    //    PlayerInput 用来切换 action map，支持多人/设备自动管理。如果没有 PlayerInput，请用我之前发的没有 PlayerInput 的版本。

    [Header("=== ActionMap 名称配置（替换为你项目的 Map 名称）")]

    // 6) 切换到哪个 Map 用于重绑定（通常是 "UI" 或 "Rebind"）
    //public string rebindActionMapName = "UI";

    [Header("=== 可选 UI 关联 / 行为配置 ===")]

    // 8) 如果你有 Button，可以把它拖进来，脚本会自动把 Button.onClick 绑定到 StartRebind（你也可以手动在 Inspector 里绑定）。
    public Button rebindButton;

    // 9) 是否启用超时（比如 10 秒后自动取消）。若不需要设为 false。
    public bool enableTimeout = false;
    public float rebindTimeoutSeconds = 10f;

    // 10) 绑定完成/取消事件（可在 Inspector 里绑定额外行为）
    public UnityEvent onBindingsUpdate;
    public UnityEvent onRebindComplete;
    public UnityEvent onRebindCanceled;

    // player input reference => the player that rebinding operates on
    private PlayerInput playerInput;

    // 内部使用的重绑定操作引用
    private InputActionRebindingExtensions.RebindingOperation rebindingOp;

    // 记录开始重绑定前的当前 Map（用于恢复）
    private InputActionMap previousActionMap = null;

    // 用于超时 coroutine 的引用
    private Coroutine timeoutCoroutine;

    public void Init(PlayerInput bindedPlayerInput)
    {
        playerInput = bindedPlayerInput;
    }

    void OnEnable()
    {
        Debug.Assert(playerInput != null, "PlayerInput not set to this rebind button ui");
        // 启用时刷新显示当前绑定（确保 UI 与当前 binding 保持一致）
        //UpdateBindingDisplay();
        //Debug.Log(actionReference.action.GetBindingDisplayString(bindingIndex, InputBinding.DisplayStringOptions.DontUseShortDisplayNames));
    }

    /// <summary>
    /// 开始交互式重绑定：通常在 UI 的 Rebind 按钮点击时调用
    /// 请把 Button 的 OnClick 事件连接到这个方法，或在 Inspector 中设置 rebindButton。
    /// </summary>
    public void StartRebind()
    {
        // 基本校验
        if (actionReference == null)
        {
            Debug.LogError($"[{nameof(RebindButtonUI)}] actionReference 没有设置（挂载在 {gameObject.name}）。");
            return;
        }

        var action = GetRuntimeAction(playerInput);
        if (action == null)
        {
            Debug.LogError($"[{nameof(RebindButtonUI)}] actionReference.action 为 null（检查 InputActionReference 配置）。");
            return;
        }

        // 如果已有重绑定在进行，先取消它（避免重复）
        if (rebindingOp != null)
        {
            rebindingOp.Cancel();
            rebindingOp.Dispose();
            rebindingOp = null;
            if (timeoutCoroutine != null) StopCoroutine(timeoutCoroutine);
        }

        // OnRebindStart
        /*// 1) 尝试通过 PlayerInput 切换到重绑定需要的 Action Map（推荐）
        if (playerInput != null)
        {
            // 记录之前的 active map（用于恢复）
            previousActionMap = playerInput.currentActionMap;

            // 切换到你配置的重绑定 Map（例如 "UI"）
            playerInput.(rebindActionMapName);
        }
        else
        {
            // 如果没有 PlayerInput，建议自己手动禁用可能会干扰的 Action Map（见无 PlayerInput 版本）。
            Debug.LogWarning($"[{nameof(RebindButtonUI)}] playerInput 未设置，确保其他 ActionMap 已被禁用以避免在重绑定时触发游戏逻辑。");
        }*/

        // 1) 禁用该 action（防止重绑定时触发游戏逻辑）
        action.Disable();

        // 2) disable eventsystem navigate, point, click, and submit
        InputActionMap uiMap = actionReference.asset.FindActionMap("UI", throwIfNotFound: false); // asset means the template asset (not instantiated clone)
        var navigate = uiMap?.FindAction("Navigate", throwIfNotFound: false);
        var click = uiMap?.FindAction("Click", throwIfNotFound: false);
        var submit = uiMap?.FindAction("Submit", throwIfNotFound: false);
        var point = uiMap?.FindAction("Point", throwIfNotFound: false);

        navigate?.Disable();
        point?.Disable();
        click?.Disable();
        submit?.Disable();

        // 3) 更新状态文本提示玩家按键
        bindingText.text = "Press any key...";
        bindingText.color = new Color(1f, 1f, 1f); // yellow highlight

        // 4) 开始交互式重绑定
        //    主要链式配置：排除鼠标移动/鼠标 delta，允许 ESC 取消，完成/取消回调
        rebindingOp = action.PerformInteractiveRebinding(bindingIndex)
            // 排除鼠标坐标防止鼠标一动就绑定
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            // 排除触摸位置（若你的项目有触控）
            .WithControlsExcluding("<Touchscreen>/position")
            // 允许通过 ESC 取消（替换或移除都可）
            .WithCancelingThrough("<Keyboard>/escape")
            // 如果你只想允许键盘（而不允许手柄），可打开下一行并注释上面的 WithCancelingThrough
            // .WithControlsHavingToMatchPath("<Keyboard>")
            // 重绑定完成
            .OnComplete(operation =>
            {
                operation.Dispose();
                rebindingOp = null;

                // 解绑超时协程
                if (timeoutCoroutine != null) { StopCoroutine(timeoutCoroutine); timeoutCoroutine = null; }

                // 重新启用 action
                action.Enable();

                // 恢复之前的 Map
                /*if (playerInput != null)
                {
                    // 如果你想恢复到之前具体 Map，则恢复 previousActionMap；
                    // 也可以固定恢复到 restoreActionMapName
                    if (previousActionMap != null)
                        playerInput.(previousActionMap.name);
                    else
                        Debug.LogError("Missing previous action map");
                }*/
                //navigate?.Enable();
                point?.Enable();
                click?.Enable();
                submit?.Enable();

                // 刷新显示 + 保存
                UpdateBindingDisplay();
                onBindingsUpdate?.Invoke(); // => save               

                // 触发完成事件（便于外部 UI 做动画或提示）
                onRebindComplete?.Invoke();
            })
            // 取消（例如按 ESC）
            .OnCancel(operation =>
            {
                operation.Dispose();
                rebindingOp = null;

                if (timeoutCoroutine != null) { StopCoroutine(timeoutCoroutine); timeoutCoroutine = null; }

                // 重新启用 action
                action.Enable();

                // 恢复 action map
                /*if (playerInput != null)
                {
                    if (previousActionMap != null)
                        playerInput.(previousActionMap.name);
                    else
                        Debug.LogError("Missing previous action map");
                }*/
                navigate?.Enable();
                point?.Enable();
                click?.Enable();
                submit?.Enable();

                // 更新 UI
                UpdateBindingDisplay();

                onRebindCanceled?.Invoke();
            });

        // 启动重绑定（这是实际开始监听输入的调用）
        rebindingOp.Start();

        // 可选：开启超时取消
        if (enableTimeout)
        {
            timeoutCoroutine = StartCoroutine(RebindTimeoutCoroutine(rebindTimeoutSeconds, navigate, point, click, submit));
        }
    }

    // 超时协程：到点取消重绑定并回滚
    private IEnumerator RebindTimeoutCoroutine(float seconds, InputAction navigate, InputAction point, InputAction click, InputAction submit)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // 超时，取消重绑定
        if (rebindingOp != null)
        {
            rebindingOp.Cancel();
            rebindingOp.Dispose();
            rebindingOp = null;
        }

        // 恢复 UI / Action
        UpdateBindingDisplay();

        // 恢复 actionMap
        /*if (playerInput != null)
        {
            if (previousActionMap != null)
                playerInput.(previousActionMap.name);
            else
                Debug.LogError("Missing previous action map");
        }*/
        navigate?.Enable();
        point?.Enable();
        click?.Enable();
        submit?.Enable();

        // 触发取消事件
        onRebindCanceled?.Invoke();

        timeoutCoroutine = null;
    }

    /// <summary>
    /// 将 action 的 binding 名称显示到 UI
    /// 注意：GetBindingDisplayString 可以接受 DisplayStringOptions 来控制显示风格
    /// </summary>
    public void UpdateBindingDisplay()
    {
        if (actionReference == null || bindingText == null) return;

        var action = GetRuntimeAction(playerInput);
        if (action == null) return;

        // 取 binding 的可读字符串，例如 "Space" / "Left Ctrl" / "Gamepad button south"
        // bindingIndex 是你在 Inspector 里配置的索引
        bindingText.text = $"[{action.GetBindingDisplayString(bindingIndex, InputBinding.DisplayStringOptions.DontUseShortDisplayNames)}]";
        bindingText.color = Color.white;
    }

    /// <summary>
    /// 恢复单个 binding 到默认（RemoveBindingOverride）
    /// 按钮可绑定到这个方法以实现 "Reset to default"
    /// </summary>
    public void ResetBindingToDefault()
    {
        if (actionReference == null) return;

        var action = GetRuntimeAction(playerInput);
        action.RemoveBindingOverride(bindingIndex);
        // update display
        UpdateBindingDisplay();
        onBindingsUpdate?.Invoke();
    }

    void OnDisable()
    {
        // 确保销毁时取消正在进行的重绑定
        if (rebindingOp != null)
        {
            rebindingOp.Cancel();
            rebindingOp.Dispose();
            rebindingOp = null;
        }

        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
            timeoutCoroutine = null;
        }
    }

    private InputAction GetRuntimeAction(PlayerInput playerInput)
    {
        return playerInput.actions.FindAction(
            actionReference.action.name,
            throwIfNotFound: true
        );
    }
}

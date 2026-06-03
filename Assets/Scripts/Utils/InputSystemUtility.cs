namespace UnityEngine.InputSystem
{
    public static class InputSystemUtility
    {
        public static InputAction GetRuntimeAction(PlayerInput playerInput, InputActionReference actionReference)
        {
            //Debug.Log($"GetRuntimeAction {playerInput.actions.FindAction(actionReference.action.name,throwIfNotFound: true)}");
            var map = playerInput.actions.FindActionMap(actionReference.action.actionMap.name);
            return map.FindAction(
                actionReference.action.name,
                throwIfNotFound: true
            );
        }

        public static void LogActionMaps(PlayerInput playerInput)
        {
            Debug.Log($"Current Map: {playerInput.currentActionMap?.name}");

            foreach (var map in playerInput.actions.actionMaps)
            {
                Debug.Log($"{map.name}: Enabled = {map.enabled}");
            }
        }
    }
}

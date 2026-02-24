using System;
using UnityEngine;

namespace RinCore
{
    [RequireComponent(typeof(UnityEngine.UI.Selectable))]
    public class PreventEventSystemUnselect : MonoBehaviour
    {
        [System.Flags]
        enum ButtonBuildSetting
        {
            None = 0,
            WebGL = 1 << 1,
            Win = 1 << 2,
            Editor = 1 << 3,
        }
        [SerializeField] ButtonBuildSetting buildTarget = (ButtonBuildSetting.Win | ButtonBuildSetting.WebGL | ButtonBuildSetting.Editor);
        [SerializeField] bool SelectWhenEnabled = false;
        public bool IsBuildCorrect
        {
            get
            {
                bool active = true;
                if (!buildTarget.HasFlag(ButtonBuildSetting.WebGL) && GeneralManager.IsWebGL) active = false;
                if (!buildTarget.HasFlag(ButtonBuildSetting.Editor) && GeneralManager.IsEditor) active = false;
                return active;
            }
        }
        private void LateUpdate()
        {
            if (IsBuildCorrect && !RinHelper.HasSelectWithEventSystem)
            {
                gameObject.Select_WithEventSystem();
            }
        }
        private void OnEnable()
        {
            if (IsBuildCorrect && SelectWhenEnabled)
            {
                gameObject.Select_WithEventSystem();
            }
        }
    }
}
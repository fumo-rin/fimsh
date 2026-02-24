using RinCore;
using UnityEngine;
using UnityEngine.UI;
namespace RinCore
{
    [RequireComponent(typeof(Button))]
    public class FumoMenuSceneButton : MonoBehaviour
    {
        Button b;
        [SerializeField] ScenePairSO sceneToLoad;

        [System.Flags]
        enum ButtonBuildSetting
        {
            None = 0,
            WebGL = 1 << 1,
            Win = 1 << 2,
            Editor = 1 << 3,
        }
        [SerializeField] ButtonBuildSetting buildTarget = (ButtonBuildSetting.Win | ButtonBuildSetting.WebGL | ButtonBuildSetting.Editor);
        private void Awake()
        {
            b = GetComponent<Button>();
        }
        private void Start()
        {
            bool active = true;
            if (!buildTarget.HasFlag(ButtonBuildSetting.WebGL) && GeneralManager.IsWebGL) active = false;
            if (!buildTarget.HasFlag(ButtonBuildSetting.Editor) && GeneralManager.IsEditor) active = false;
            if (!active)
            {
                b.interactable = false;
                return;
            }
            b.BindSingleEventAction(PressStart);
        }
        private void OnDestroy()
        {
            b.RemoveAllClickActions();
        }
        private void PressStart()
        {
            sceneToLoad.Load();
        }
    }
}

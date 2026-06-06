using RinCore;
using UnityEngine;
using UnityEngine.UI;

namespace rinCore
{
    [RequireComponent(typeof(Button))]
    public class ClickToHideButton : MonoBehaviour
    {
        Button b;
        private void Awake()
        {
            b = GetComponent<Button>();
            if (b == null && gameObject != null)
            {
                gameObject.SetActive(false);
            }
        }
        private void Start()
        {
            if (b == null)
            {
                return;
            }
            b.BindSingleAction(() =>
            {
                b.RemoveAllClickActions();
                Destroy(gameObject);
            });
        }
    }
}

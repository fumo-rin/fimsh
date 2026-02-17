using RinCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishPropButton : MonoBehaviour
{
    [SerializeField] Button pressButton;
    public bool WasPressedThisFrame { get; private set; }
    public void OnDestroy()
    {
        pressButton.RemoveAllClickActions();
    }
    public FishPropButton Bind(string s, System.Action a)
    {
        WasPressedThisFrame = false;
        pressButton.BindSingleAction(a);
        pressButton.AddClickAction(() => WasPressedThisFrame = true);
        if (pressButton.GetComponentInChildren<TMP_Text>() is TMP_Text t)
        {
            t.text = s;
        }
        return this;
    }
    private void LateUpdate()
    {
        WasPressedThisFrame = false;
    }
}

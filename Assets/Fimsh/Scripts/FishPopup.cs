using TMPro;
using UnityEngine;

public class FishPopup : MonoBehaviour
{
    [SerializeField] TMP_Text popupText;
    [SerializeField] Animator popupAnimator;
    [SerializeField] string popupAnimKey = "FISHPOPUP";
    static FishPopup instance;
    private void Awake()
    {
        instance = this;
    }
    public static void TriggerPopup(string text)
    {
        if (instance is FishPopup p && p.gameObject != null && p.gameObject.activeInHierarchy)
        {
            p.popupText.text = text;
            p.popupAnimator.SetTrigger(p.popupAnimKey);
        }
    }
}
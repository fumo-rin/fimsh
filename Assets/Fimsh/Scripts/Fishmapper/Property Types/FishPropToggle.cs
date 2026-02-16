using UnityEngine;
using UnityEngine.UI;

public class FishPropToggle : MonoBehaviour
{
    [SerializeField] Toggle toggle;
    public Toggle PropGet => toggle;
    [SerializeField] TMPro.TMP_Text titleText;
    public void SetTitle(string title)
    {
        titleText.text = title;
    }
}

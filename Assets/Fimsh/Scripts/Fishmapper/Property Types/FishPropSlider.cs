using UnityEngine;
using UnityEngine.UI;

public class FishPropSlider : MonoBehaviour
{
    [SerializeField] Slider slider;
    public Slider SliderGet => slider;
    [SerializeField] TMPro.TMP_Text titleText, valueText;
    public void SetTitle(string title)
    {
        titleText.text = title;
    }
    public void SetValueText(string val)
    {
        valueText.text = val;
    }
}

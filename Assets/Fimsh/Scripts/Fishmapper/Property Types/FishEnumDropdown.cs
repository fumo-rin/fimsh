using System;
using System.Linq;
using TMPro;
using UnityEngine;
public class FishEnumDropdown : MonoBehaviour
{
    [SerializeField] TMP_Dropdown dropdown;
    [SerializeField] TMP_Text titleText;
    private Type enumType;
    public TMP_Dropdown DropdownGet => dropdown;
    public void SetTitle(string title)
    {
        titleText.text = title;
    }
    public void BindEnum<TEnum>(TEnum currentValue) where TEnum : Enum
    {
        enumType = typeof(TEnum);

        dropdown.ClearOptions();

        var names = Enum.GetNames(enumType).ToList();
        dropdown.AddOptions(names);

        int index = Array.IndexOf(names.ToArray(), currentValue.ToString());
        dropdown.SetValueWithoutNotify(Mathf.Max(0, index));
    }
    public TEnum GetValue<TEnum>() where TEnum : Enum
    {
        string selected = dropdown.options[dropdown.value].text;
        return (TEnum)Enum.Parse(typeof(TEnum), selected);
    }
    public void SetValue<TEnum>(TEnum value) where TEnum : Enum
    {
        int index = Array.IndexOf(Enum.GetNames(typeof(TEnum)), value.ToString());
        if (index >= 0)
            dropdown.SetValueWithoutNotify(index);
    }
}

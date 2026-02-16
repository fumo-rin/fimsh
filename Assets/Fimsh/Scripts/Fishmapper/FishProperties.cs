using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class FishProperties : MonoBehaviour
{
    static FishProperties instance;
    [SerializeField] Transform propContainer;
    [SerializeField] FishPropSlider propSlider;
    [SerializeField] FishEnumDropdown enumDropdown;
    [SerializeField] FishPropToggle propToggle;
    Coroutine drawRoutine;
    HashSet<GameObject> spawnedItems = new();
    void CleanUp()
    {
        foreach (var item in spawnedItems.ToList())
        {
            if (item != null && item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
        }
        spawnedItems.Clear();
    }
    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        propSlider.gameObject.SetActive(false);
        enumDropdown.gameObject.SetActive(false);
        propToggle.gameObject.SetActive(false);
    }
    public static void DrawItem(FishNode item)
    {
        if (instance != null && instance is FishProperties f)
        {
            if (f.drawRoutine != null)
            {
                f.StopCoroutine(f.drawRoutine);
                f.CleanUp();
            }
            IEnumerator CO_Run()
            {
                yield return item.DrawNode(f);
                f.CleanUp();
                f.drawRoutine = null;
            }
            f.drawRoutine = f.StartCoroutine(CO_Run());
        }
    }
    public FishPropSlider StartSlider()
    {
        FishPropSlider spawned = Instantiate(propSlider, propContainer);
        spawned.gameObject.SetActive(true);
        spawnedItems.Add(spawned.gameObject);
        return spawned;
    }
    public FishEnumDropdown StartEnumDropdown()
    {
        FishEnumDropdown spawned = Instantiate(enumDropdown, propContainer);
        spawned.gameObject.SetActive(true);
        spawnedItems.Add(spawned.gameObject);
        return spawned;
    }
    public FishPropToggle StartToggle()
    {
        FishPropToggle spawned = Instantiate(propToggle, propContainer);
        spawned.gameObject.SetActive(true);
        spawnedItems.Add(spawned.gameObject);
        return spawned;
    }
}

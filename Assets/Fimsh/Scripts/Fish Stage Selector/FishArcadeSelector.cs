using RinCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class FishArcadeSelector : MonoBehaviour
{
    [System.Serializable]
    public class FishArcadeLevel
    {
        [SerializeField] private TextAsset levelFile;
        public string LevelString
        {
            get
            {
                return levelFile != null ? levelFile.text : string.Empty;
            }
        }
        public string levelName;
        public bool Validate()
        {
            bool changed = false;
            if (levelFile.name != levelName)
            {
                changed = true;
                string fileName = levelFile.name;
                int dashIndex = fileName.IndexOf('-');

                levelName = dashIndex >= 0 && dashIndex + 1 < fileName.Length
                    ? fileName.Substring(dashIndex + 1).Trim()
                    : fileName.Trim();
            }
            return changed;
        }
    }
    [SerializeField] List<FishArcadeLevel> levels = new();
    [SerializeField] Button startButton;
    [SerializeField] ScenePairSO gameScene, arcadeSelectorScene;
    private void Start()
    {
        startButton.BindSingleAction(() => FishTools.StartArcadeMode(levels, gameScene, arcadeSelectorScene));
    }
    private void OnDestroy()
    {
        startButton.RemoveAllClickActions();
    }
    private void OnValidate()
    {
        bool changed = false;
        foreach (var item in levels)
        {
            changed = item.Validate();
        }
        if (changed)
        {
            this.Dirty();
        }
    }
}
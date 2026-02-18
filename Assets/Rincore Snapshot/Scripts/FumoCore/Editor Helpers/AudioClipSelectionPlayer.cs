using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
using System;
using UnityEditor.ShortcutManagement;
public static class AudioUtils
{
#if UNITY_EDITOR

    public static class AudioEditorShortcuts
    {
        [Shortcut("Audio/Stop Preview Clip", KeyCode.Space)]
        private static void StopPreviewClip()
        {
            if (AudioUtils.IsPlaying)
            {
                AudioUtils.Stop();
            }
        }
    }
#endif
    private static GameObject editorAudioObject;
    private static AudioSource audioSource;

    public static bool IsPlaying => audioSource != null && audioSource.isPlaying;

    public static void PlayClip(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("EditorAudioPlayer: PlayClip called with null clip.");
            return;
        }

        if (editorAudioObject == null)
        {
            editorAudioObject = new GameObject("EditorAudioPlayer");
            editorAudioObject.hideFlags = HideFlags.HideAndDontSave;

            audioSource = editorAudioObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.hideFlags = HideFlags.HideAndDontSave;
        }

        if (audioSource.isPlaying)
            audioSource.Stop();

        audioSource.clip = clip;
        audioSource.Play();

        EditorApplication.update -= EditorUpdate;
        EditorApplication.update += EditorUpdate;

        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    public static void Stop()
    {
        if (audioSource == null)
            return;

        if (audioSource.isPlaying)
            audioSource.Stop();
    }

    private static void EditorUpdate()
    {
        if (audioSource == null || !audioSource.isPlaying)
        {
            EditorApplication.update -= EditorUpdate;
            SceneView.duringSceneGui -= OnSceneGUI;

            if (editorAudioObject != null)
            {
                UnityEngine.Object.DestroyImmediate(editorAudioObject);
                editorAudioObject = null;
                audioSource = null;
            }
        }
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Space)
        {
            Stop();
            e.Use();
        }
    }
}
#endif

namespace RinCore
{
#if UNITY_EDITOR
    [InitializeOnLoad]
    public static class AudioClipPlayTest
    {
        static AudioClipPlayTest()
        {
            Selection.selectionChanged += () =>
            {
                if (Selection.activeObject is AudioClip clip)
                {
                    AudioUtils.PlayClip(clip);
                }
                else
                {
                    if (AudioUtils.IsPlaying)
                        AudioUtils.Stop();
                }
            };
        }
    }
#endif
}

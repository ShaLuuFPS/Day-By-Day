using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Quick animation clip preview tool.
/// Usage: Tools → Animation Preview → click ▶ on any clip to preview it on the Player.
/// Click ⏸ or close the window to restore the original controller.
/// Delete this file when no longer needed.
/// </summary>
public class AnimationPreviewWindow : EditorWindow
{
    private AnimatorController _controller;
    private List<AnimationClip> _clips = new();
    private Vector2 _scrollPos;
    private int _playingIndex = -1;
    private Animator _targetAnimator;

    [MenuItem("Tools/Animation Preview")]
    public static void ShowWindow()
    {
        var window = GetWindow<AnimationPreviewWindow>("Anim Preview");
        window.minSize = new Vector2(300, 200);
        window.Show();
    }

    void OnEnable()
    {
        RefreshClips();
        FindTargetAnimator();
    }

    void OnFocus()
    {
        // Re-find when user switches back to Unity
        FindTargetAnimator();
    }

    void RefreshClips()
    {
        _controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            "Assets/Project/Animations/Player.controller");
        if (_controller != null)
        {
            _clips = _controller.animationClips
                .Where(c => c != null)
                .Distinct()
                .OrderBy(c => c.name)
                .ToList();
        }
    }

    void FindTargetAnimator()
    {
        var player = GameObject.Find("Player");
        if (player != null)
        {
            _targetAnimator = player.GetComponentInChildren<Animator>();
        }

        // Fallback: find any Animator using Player controller
        if (_targetAnimator == null)
        {
            var allAnimators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
            foreach (var anim in allAnimators)
            {
                if (anim.runtimeAnimatorController != null &&
                    anim.runtimeAnimatorController.name == "Player")
                {
                    _targetAnimator = anim;
                    break;
                }
            }
        }
    }

    void OnGUI()
    {
        // --- Header ---
        if (_controller == null)
        {
            EditorGUILayout.HelpBox(
                "Player.controller not found at Assets/Project/Animations/",
                MessageType.Error);
            if (GUILayout.Button("Refresh")) { RefreshClips(); FindTargetAnimator(); }
            return;
        }

        if (_targetAnimator == null)
        {
            EditorGUILayout.HelpBox(
                "No Animator found. Enter Play Mode or place a Player in the scene.",
                MessageType.Warning);
            if (GUILayout.Button("Find Player")) FindTargetAnimator();
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to preview animations.", MessageType.Info);
        }

        EditorGUILayout.LabelField($"Controller: {_controller.name}    Clips: {_clips.Count}",
            EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Target: {_targetAnimator.name}",
            EditorStyles.miniLabel);
        EditorGUILayout.Space();

        // --- Clip List ---
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        for (int i = 0; i < _clips.Count; i++)
        {
            var clip = _clips[i];
            bool isPlaying = (i == _playingIndex);

            var bgColor = isPlaying
                ? new Color(0.2f, 0.5f, 0.2f, 0.3f)
                : (i % 2 == 0 ? Color.clear : new Color(0.5f, 0.5f, 0.5f, 0.1f));

            var rect = EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(rect, bgColor);

            // Play/Stop button
            var btnLabel = isPlaying ? "⏸" : "▶";
            var btnColor = isPlaying ? Color.green : Color.white;
            var oldColor = GUI.color;
            GUI.color = btnColor;
            if (GUILayout.Button(btnLabel, GUILayout.Width(28), GUILayout.Height(20)))
            {
                if (isPlaying)
                {
                    RestoreController();
                    _playingIndex = -1;
                }
                else
                {
                    _playingIndex = i;
                    PlayClip(clip);
                }
            }
            GUI.color = oldColor;

            // Clip name
            EditorGUILayout.LabelField(clip.name, GUILayout.MinWidth(160));

            // Duration
            EditorGUILayout.LabelField($"{clip.length:F2}s", GUILayout.Width(45));

            // Loop indicator
            EditorGUILayout.LabelField(clip.isLooping ? "⟳" : "→", GUILayout.Width(18));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        // --- Footer ---
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
        if (GUILayout.Button("Stop & Restore", GUILayout.Height(28)))
        {
            RestoreController();
            _playingIndex = -1;
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("↻ Refresh", GUILayout.Height(28)))
        {
            RefreshClips();
            FindTargetAnimator();
        }
        EditorGUILayout.EndHorizontal();
    }

    void PlayClip(AnimationClip clip)
    {
        if (_targetAnimator == null) return;
        if (!EditorApplication.isPlaying) return; // Only work in Play Mode

        // Always use the asset controller as the override base (never chain overrides)
        var overrideCtrl = new AnimatorOverrideController(_controller);
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideCtrl.GetOverrides(overrides);

        for (int ov = 0; ov < overrides.Count; ov++)
        {
            overrideCtrl[overrides[ov].Key] = clip;
        }

        _targetAnimator.runtimeAnimatorController = overrideCtrl;

        // Reset state so it picks up the new overrides
        var currentState = _targetAnimator.GetCurrentAnimatorStateInfo(0);
        _targetAnimator.Play(currentState.fullPathHash, 0, 0f);
    }

    void RestoreController()
    {
        // Restore the asset controller directly
        if (_targetAnimator != null && _controller != null)
        {
            _targetAnimator.runtimeAnimatorController = _controller;
        }
    }

    void OnDisable()
    {
        RestoreController();
        _playingIndex = -1;
    }

    void OnDestroy()
    {
        RestoreController();
    }
}

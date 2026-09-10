#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Run with Unity -batchmode -projectPath <project> -executeMethod SkillFilterCheck.Run -quit.
public static class SkillFilterCheck
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("Run in batch mode; this check opens scenes without saving.");
        foreach (string scene in new[] { "Assets/Scenes/SampleScene.unity", "Assets/Scenes/Android/AndroidSampleScene.unity" })
        {
            EditorSceneManager.OpenScene(scene);
            SkillManager manager = UnityEngine.Object.FindAnyObjectByType<SkillManager>();
            typeof(SkillManager).GetProperty("Instance").SetValue(null, manager);
            manager.ResetState();
            typeof(PlayerManager).GetProperty("Instance").SetValue(null, UnityEngine.Object.FindAnyObjectByType<PlayerManager>());
            MarketManager market = UnityEngine.Object.FindAnyObjectByType<MarketManager>();
            typeof(MarketManager).GetProperty("Instance").SetValue(null, market);
            typeof(MarketManager).GetProperty("CurrentStat").SetValue(market, new PlayerStat());
            SkillPanelUI panel = UnityEngine.Object.FindAnyObjectByType<SkillPanelUI>(FindObjectsInactive.Include);
            panel.gameObject.SetActive(true);
            typeof(SkillPanelUI).GetMethod("Start", Private).Invoke(panel, null);
            var selected = (HashSet<string>)typeof(SkillPanelUI).GetField("selectedEffects", Private).GetValue(panel);
            var buttons = (Dictionary<string, Button>)typeof(SkillPanelUI).GetField("effectButtons", Private).GetValue(panel);
            var popup = (GameObject)typeof(SkillPanelUI).GetField("filterPanel", Private).GetValue(panel);
            Action<SkillCategory> selectTab = category => typeof(SkillPanelUI).GetMethod("SelectTab", Private).Invoke(panel, new object[] { category });
            var profiles = panel.icons.Select(slot => manager.GetSkillProfile(slot.id)).Where(p => p != null).ToArray();
            var labels = profiles.SelectMany(p => p.effects).Select(e => EventEffectFormatter.GetEffectLabel(e.effectType)).ToHashSet();
            Check(labels.SetEquals(buttons.Keys), "Every effect has a filter button");
            Check(!popup.activeSelf, "Initially collapsed");

            foreach (SkillCategory category in Enum.GetValues(typeof(SkillCategory)))
            {
                selected.Clear();
                selectTab(category);
                Check(panel.icons.Count(s => s.button.gameObject.activeSelf) == profiles.Count(p => p.category == category), "Unfiltered category");
                foreach (string label in labels)
                {
                    buttons[label].onClick.Invoke();
                    foreach (var slot in panel.icons)
                    {
                        SkillSO profile = manager.GetSkillProfile(slot.id);
                        bool expected = profile != null && profile.category == category && profile.effects.Any(e => EventEffectFormatter.GetEffectLabel(e.effectType) == label);
                        Check(slot.button.gameObject.activeSelf == expected, "Effect match: " + label);
                    }
                    buttons[label].onClick.Invoke();
                }
            }

            selectTab(SkillCategory.CoinDesign);
            foreach (string label in labels.Take(2)) buttons[label].onClick.Invoke();
            Check(profiles.Where(p => p.category == SkillCategory.CoinDesign && p.effects.Any(e => selected.Contains(EventEffectFormatter.GetEffectLabel(e.effectType))))
                .Select(p => p.id).ToHashSet().SetEquals(panel.icons.Where(s => s.button.gameObject.activeSelf).Select(s => s.id)), "Multiple effects use OR");
            var first = profiles.First(p => p.category == SkillCategory.CoinDesign);
            typeof(SkillManager).GetMethod("HandleSkillClicked", Private).Invoke(manager, new object[] { first.id });
            selected.Clear();
            selectTab(SkillCategory.CoinDesign);
            Check(panel.purchaseBtn.gameObject.activeSelf, "Visible selected skill has purchase control");
            selected.Add("No matching effect");
            selectTab(SkillCategory.CoinDesign);
            Check(panel.icons.All(s => !s.button.gameObject.activeSelf) && !panel.purchaseBtn.gameObject.activeSelf, "Empty result hides purchase");
            popup.transform.Find("Reset").GetComponent<Button>().onClick.Invoke();
            Check(selected.Count == 0 && panel.icons.Any(s => s.button.gameObject.activeSelf), "Reset restores skills");
            panel.transform.Find("FilterButton").GetComponent<Button>().onClick.Invoke();
            Check(popup.activeSelf, "Expand button opens popup");

            buttons[labels.First()].onClick.Invoke();
            foreach (TutorialUI tutorial in UnityEngine.Object.FindObjectsByType<TutorialUI>(FindObjectsInactive.Include))
                tutorial.tutorialPanel.SetActive(false);
            foreach (StatTooltipUI tooltip in UnityEngine.Object.FindObjectsByType<StatTooltipUI>(FindObjectsInactive.Include))
                tooltip.gameObject.SetActive(false);

            // Render the actual scene UI for visual review, without modifying the scene asset.
            var cameraObject = new GameObject("FilterCheckCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            var target = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = target;
            Canvas canvas = panel.GetComponentInParent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4];
            ((RectTransform)popup.transform).GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector3 point = ((RectTransform)canvas.transform).InverseTransformPoint(corner);
                Check(((RectTransform)canvas.transform).rect.Contains(point), "Popup stays inside canvas");
            }
            camera.Render();
            RenderTexture.active = target;
            var screenshot = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            screenshot.Apply();
            System.IO.File.WriteAllBytes("Logs/skill-filter-" + (scene.Contains("Android") ? "android" : "pc") + ".png", screenshot.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = null;
            target.Release();
            UnityEngine.Object.DestroyImmediate(screenshot);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            popup.transform.Find("Close").GetComponent<Button>().onClick.Invoke();
            Check(!popup.activeSelf, "Close collapses popup");
            Debug.Log("PASS SkillFilterCheck: " + scene + " / " + labels.Count + " effects / " + profiles.Length + " skills");
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAIL SkillFilterCheck: " + message);
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

// Unity -batchmode -projectPath <project> -executeMethod CashInterestCheck.Run -quit
public static class CashInterestCheck
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Run()
    {
        if (!Application.isBatchMode) throw new Exception("Run in batch mode; scenes are opened without saving.");
        foreach (string scene in new[] { "Assets/Scenes/SampleScene.unity", "Assets/Scenes/Android/AndroidSampleScene.unity" })
        {
            EditorSceneManager.OpenScene(scene);
            PlayerManager player = UnityEngine.Object.FindAnyObjectByType<PlayerManager>();
            MarketManager market = UnityEngine.Object.FindAnyObjectByType<MarketManager>();
            TimeManager time = UnityEngine.Object.FindAnyObjectByType<TimeManager>();
            typeof(PlayerManager).GetProperty("Instance").SetValue(null, player);
            typeof(MarketManager).GetProperty("Instance").SetValue(null, market);
            typeof(TimeManager).GetProperty("Instance").SetValue(null, time);
            market.ResetState();
            time.ResetState();
            Action pay = () => typeof(MarketManager).GetMethod("PayMonthlyCashInterest", Private).Invoke(market, null);
            EventHub.OnMonthChanged += pay;
            try
            {
                player.currentMoney = 12000000;
                Advance(time, new DateTime(2021, 1, 30), new DateTime(2021, 1, 31));
                Check(player.currentMoney == 12000000 && market.EventLog.Count == 0, "No interest within month");
                Advance(time, new DateTime(2021, 1, 31), new DateTime(2021, 2, 1));
                Check(player.currentMoney == 12001000 && market.EventLog.Count == 1, "Annual 0.1% / 12 pays 1000");
                typeof(TimeManager).GetMethod("Update", Private).Invoke(time, null);
                Check(market.EventLog.Count == 1, "No duplicate payment on same date");
                EventLogEntry interest = market.EventLog[0];
                Check(interest.CashInterest == 1000 && interest.InterestPrincipal == 12000000, "Log preserves principal and payment");
                Advance(time, new DateTime(2021, 12, 31), new DateTime(2022, 1, 1));
                Check(market.EventLog.Count == 2, "December to January pays");
                Advance(time, new DateTime(2024, 2, 28), new DateTime(2024, 2, 29));
                Check(market.EventLog.Count == 2, "Leap day is not a new month");
                Advance(time, new DateTime(2024, 2, 29), new DateTime(2024, 3, 1));
                Check(market.EventLog.Count == 3, "Leap year March pays");

                market.ResetState();
                player.currentMoney = 6000;
                pay();
                Check(player.currentMoney == 6000, "Half won carried forward");
                pay();
                Check(player.currentMoney == 6001, "Fractional interest eventually pays");
                market.ResetState();
                long annualInterest = 0;
                for (int month = 0; month < 12; month++)
                {
                    player.currentMoney = 10000;
                    pay();
                    annualInterest += player.currentMoney - 10000;
                }
                Check(annualInterest == 10, "Twelve monthly fractions equal exact annual interest");
                market.ResetState();
                player.currentMoney = 6000;
                pay();
                Check(player.currentMoney == 6000, "New game clears remainder");
                player.currentMoney = 0;
                int count = market.EventLog.Count;
                pay();
                player.currentMoney = -1;
                pay();
                Check(market.EventLog.Count == count, "Nonpositive cash receives no interest");
                player.currentMoney = long.MaxValue;
                pay();
                Check(player.currentMoney == long.MaxValue, "Maximum balance does not overflow");
                typeof(MarketManager).GetProperty("IsGameOver").SetValue(market, true);
                count = market.EventLog.Count;
                pay();
                Check(market.EventLog.Count == count, "No payments after game over");

                EventNotificationUI notification = UnityEngine.Object.FindAnyObjectByType<EventNotificationUI>(FindObjectsInactive.Include);
                Color original = notification.cardView.titleText.color;
                var news = ScriptableObject.CreateInstance<EventSO>();
                news.message = "Regular event";
                var newsEntry = new EventLogEntry { Profile = news, Date = interest.Date };
                var receive = typeof(EventNotificationUI).GetMethod("HandleEventTriggered", Private);
                var close = typeof(EventNotificationUI).GetMethod("Close", Private);
                receive.Invoke(notification, new object[] { interest });
                Check(notification.cardView.background.color == Color.white, "Interest background is white");
                Check(notification.cardView.titleText.color == Color.black && notification.cardView.dateText.color == Color.black
                    && notification.cardView.effectsText.color == Color.black, "All interest text is black");
                var border = notification.cardView.background.GetComponent<UnityEngine.UI.Outline>();
                Check(border != null && border.enabled && border.effectColor == Color.black
                    && border.effectDistance == new Vector2(1.5f, -1.5f), "Thin black interest border");
                foreach (TutorialUI tutorial in UnityEngine.Object.FindObjectsByType<TutorialUI>(FindObjectsInactive.Include))
                    tutorial.tutorialPanel.SetActive(false);
                foreach (StatTooltipUI tooltip in UnityEngine.Object.FindObjectsByType<StatTooltipUI>(FindObjectsInactive.Include))
                    tooltip.gameObject.SetActive(false);
                var cameraObject = new GameObject("InterestCheckCamera", typeof(Camera));
                Camera camera = cameraObject.GetComponent<Camera>();
                var target = new RenderTexture(1920, 1080, 24);
                camera.targetTexture = target;
                Canvas canvas = notification.GetComponentInParent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                var screenshot = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                screenshot.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                screenshot.Apply();
                System.IO.File.WriteAllBytes("Logs/cash-interest-" + (scene.Contains("Android") ? "android" : "pc") + ".png", screenshot.EncodeToPNG());
                RenderTexture.active = null;
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.DestroyImmediate(screenshot);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                string title = notification.cardView.titleText.text;
                receive.Invoke(notification, new object[] { newsEntry });
                Check(notification.cardView.titleText.text == title, "Simultaneous news waits in queue");
                close.Invoke(notification, null);
                Check(notification.panel.activeSelf && notification.cardView.titleText.text == news.message, "Closing interest shows queued news");
                Check(notification.cardView.titleText.color == original, "Regular text color restored");
                Check(!border.enabled, "Interest border removed for regular news");
                close.Invoke(notification, null);
                Check(!notification.panel.activeSelf, "Last close hides notification");

                var result = new EventLogEntry
                {
                    Profile = news, Date = interest.Date, HasChoiceResult = true,
                    ChoiceLabel = "Choice", Succeeded = false, CashBefore = 100, CashAfter = 50
                };
                notification.cardView.Populate(result);
                Check(notification.cardView.background.color == EventEffectFormatter.NegativeColor
                    && notification.cardView.titleText.text == EventEffectFormatter.BuildEntryTitle(result)
                    && notification.cardView.effectsText.text == EventEffectFormatter.BuildEntryEffectsText(result),
                    "Log card preserves choice outcome and full details");
                receive.Invoke(notification, new object[] { interest });
                typeof(EventNotificationUI).GetField("awaitingChoice", Private).SetValue(notification, true);
                receive.Invoke(notification, new object[] { newsEntry });
                close.Invoke(notification, null);
                Check(notification.panel.activeSelf, "Unresolved choice cannot close");
                receive.Invoke(notification, new object[] { result });
                Check(notification.cardView.effectsText.text == EventEffectFormatter.BuildChoiceResultText(result)
                    && !border.enabled && notification.cardView.titleText.color == original,
                    "Choice result replaces active choice and restores regular styling");
                close.Invoke(notification, null);
                Check(notification.cardView.titleText.text == news.message, "Queued news follows choice result");
                close.Invoke(notification, null);

                receive.Invoke(notification, new object[] { interest });
                var pending = (System.Collections.Generic.Queue<Action>)typeof(EventNotificationUI)
                    .GetField("pendingEvents", Private).GetValue(notification);
                typeof(EventNotificationUI).GetMethod("HandleEventChoiceRequired", Private).Invoke(notification,
                    new object[] { new EventChoiceRequest(news, interest.Date, 1) });
                typeof(EventNotificationUI).GetMethod("HandleDebtPaymentRequired", Private).Invoke(notification,
                    new object[] { new DebtPaymentRequest(30, 100, 0.1f, 10, 10, 0, 0) });
                Check(pending.Count == 2 && notification.cardView.titleText.text == title,
                    "Choice and debt requests wait behind interest instead of overwriting it");
                pending.Clear();
                close.Invoke(notification, null);
                UnityEngine.Object.DestroyImmediate(news);
                Debug.Log("PASS CashInterestCheck: " + scene);
            }
            finally { EventHub.OnMonthChanged -= pay; }
        }
    }

    private static void Advance(TimeManager time, DateTime previous, DateTime current)
    {
        typeof(TimeManager).GetProperty("CurrentGameDate").SetValue(time, current);
        typeof(TimeManager).GetField("previousDate", Private).SetValue(time, previous);
        Time.timeScale = 1;
        typeof(TimeManager).GetMethod("Update", Private).Invoke(time, null);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAIL CashInterestCheck: " + message);
    }
}
#endif

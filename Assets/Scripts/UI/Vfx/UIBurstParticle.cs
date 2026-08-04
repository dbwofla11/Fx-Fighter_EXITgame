using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 파티클 애셋 없이 순수 uGUI Image 조각으로 만드는 간이 버스트 이펙트.
// 캔들 차트/거래 확정 버튼/스킬 구매 버튼 3곳에서 공용으로 쓴다.
// intensity01(0~1)에 비례해 조각 개수·크기·이동거리가 커진다.
// UI는 대부분 EventHub.RaiseGamePaused()로 Time.timeScale=0인 상태에서 열리므로(거래/스킬 모달),
// 애니메이션은 전부 unscaled time으로 돌려야 멈춰있는 동안에도 재생된다.
public static class UIBurstParticle
{
    private const int MinPieces = 6;
    private const int MaxPieces = 18;
    private const float MinSize = 6f;
    private const float MaxSize = 16f;
    private const float Lifetime = 0.5f;
    private const float TravelDistance = 60f;

    // 반환값(버스트 루트)은 선택적으로 쓴다 — 호출부가 곧바로 닫히는 패널(예: 거래 확정 직후 모달이
    // 스스로를 SetActive(false)하는 TradeModalUI)이라면, 이 루트를 SetParent(안 닫히는 조상, true)로
    // 옮겨야 한다. 안 그러면 부모가 비활성화되는 순간 코루틴이 멈춘 채(Destroy 도달 전) 얼어붙어서,
    // 다음에 그 패널이 다시 열릴 때 다 안 사라진 조각(작은 점)이 그대로 남아있는 채로 다시 보인다.
    public static RectTransform Spawn(RectTransform parent, Vector2 anchoredPos, Color color, float intensity01)
    {
        if (parent == null)
            return null;

        intensity01 = Mathf.Clamp01(intensity01);
        int pieceCount = Mathf.RoundToInt(Mathf.Lerp(MinPieces, MaxPieces, intensity01));
        float size = Mathf.Lerp(MinSize, MaxSize, intensity01);
        float distance = TravelDistance * (0.6f + intensity01);

        GameObject root = new GameObject("UIBurst", typeof(RectTransform));
        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.SetParent(parent, false);
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = anchoredPos;
        rootRect.sizeDelta = Vector2.zero;

        BurstRunner runner = root.AddComponent<BurstRunner>();
        runner.StartCoroutine(runner.Run(rootRect, pieceCount, size, distance, color));

        // 파티클이 생길 때마다 그 크기에 비례해 화면도 같이 흔든다(ScreenShaker 미배치 시 조용히 무시).
        ScreenShaker.Instance?.Shake(intensity01);

        return rootRect;
    }

    private class BurstRunner : MonoBehaviour
    {
        public IEnumerator Run(RectTransform root, int pieceCount, float size, float distance, Color color)
        {
            var rects = new RectTransform[pieceCount];
            var images = new Image[pieceCount];
            var dirs = new Vector2[pieceCount];

            for (int i = 0; i < pieceCount; i++)
            {
                GameObject piece = new GameObject("Piece", typeof(RectTransform), typeof(Image));
                RectTransform rect = (RectTransform)piece.transform;
                rect.SetParent(root, false);
                rect.sizeDelta = new Vector2(size, size);
                rect.anchoredPosition = Vector2.zero;

                Image img = piece.GetComponent<Image>();
                img.color = color;
                img.raycastTarget = false;

                float angle = (360f / pieceCount * i) + Random.Range(-15f, 15f);
                rects[i] = rect;
                images[i] = img;
                dirs[i] = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            }

            float t = 0f;
            while (t < Lifetime)
            {
                t += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(t / Lifetime);
                float ease = 1f - (1f - progress) * (1f - progress);

                for (int i = 0; i < pieceCount; i++)
                {
                    rects[i].anchoredPosition = dirs[i] * distance * ease;
                    rects[i].localScale = Vector3.one * (1f - progress * 0.5f);

                    Color c = images[i].color;
                    c.a = 1f - progress;
                    images[i].color = c;
                }

                yield return null;
            }

            Destroy(root.gameObject);
        }
    }
}

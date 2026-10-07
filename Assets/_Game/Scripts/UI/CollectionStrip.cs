using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blackhole
{
    /// <summary>
    /// 행성 도감 띠 12칸 (11단계 + 블랙홀). 만든 적 없는 칸은 ? 로 둔다.
    /// 칸 크기는 14 + 1.9 × i 이고 폭이 모자라면 같은 비율로 줄인다 (프로토타입 buildEvo).
    /// </summary>
    public sealed class CollectionStrip : MonoBehaviour
    {
        [SerializeField] RectTransform box;
        [SerializeField] RectTransform[] cells = new RectTransform[12];
        [SerializeField] PlanetIcon[] icons = new PlanetIcon[12];
        [SerializeField] Image[] unknownDiscs = new Image[12];
        [SerializeField] TMP_Text[] unknownMarks = new TMP_Text[12];
        [SerializeField] float paddingX = 10;
        [SerializeField] float paddingBottom = 8;
        [SerializeField] float gap = 3;

        PlanetData data;
        int shownDiscovered = -1;
        float shownWidth = -1;

        public void Bind(PlanetData planetData) => data = planetData;

        public void Refresh(int discovered)
        {
            shownDiscovered = discovered;
            Layout();
        }

        void LateUpdate()
        {
            if (data != null && !Mathf.Approximately(box.rect.width, shownWidth)) Layout();
        }

        void Layout()
        {
            if (data == null) return;
            shownWidth = box.rect.width;
            int n = cells.Length;
            float avail = Mathf.Min(shownWidth, 456) - paddingX * 2 - (n - 1) * gap;
            float sum = 0;
            for (int i = 0; i < n; i++) sum += 14 + i * 1.9f;
            float k = Mathf.Min(1, avail / sum);

            var sizes = new float[n];
            float total = (n - 1) * gap;
            for (int i = 0; i < n; i++)
            {
                sizes[i] = Mathf.Round((14 + i * 1.9f) * k);
                total += sizes[i];
            }

            float x = -total / 2;
            for (int i = 0; i < n; i++)
            {
                float s = sizes[i], view = s * 1.25f;
                var cell = cells[i];
                cell.anchorMin = cell.anchorMax = new Vector2(0.5f, 0);
                cell.pivot = new Vector2(0.5f, 0);
                cell.sizeDelta = new Vector2(view, view);
                cell.anchoredPosition = new Vector2(x + s / 2, paddingBottom);
                x += s + gap;

                bool known = i <= shownDiscovered;
                float r = s / 2 * (known && i == 8 ? 0.7f : known && i == 10 ? 0.78f : 0.95f);
                icons[i].gameObject.SetActive(known);
                unknownDiscs[i].gameObject.SetActive(!known);
                unknownMarks[i].gameObject.SetActive(!known);
                if (known)
                {
                    if (i <= data.MaxTier) icons[i].SetPlanet(data, i, r, null);
                    else icons[i].SetBlackHole(data, r * 0.55f, 0.6f);
                }
                else
                {
                    unknownDiscs[i].rectTransform.sizeDelta = new Vector2(r * 2, r * 2);
                    unknownMarks[i].fontSize = Mathf.Round(r * 1.1f);
                    // 프로토타입은 R×0.08 아래에 그린다. TMP 가운데 정렬은 줄 높이 기준이라 조금 더 내린다
                    unknownMarks[i].rectTransform.anchoredPosition = new Vector2(0, -r * 0.2f);
                }
            }
        }
    }
}

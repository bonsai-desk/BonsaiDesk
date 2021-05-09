using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Mirror.Profiler.Chart
{

    public class ChartView
    {
        Material mat;

        public int MaxFrames = 1000;

        private int selectedFrame = -1;

        public int SelectedFrame
        {
            get => selectedFrame;
            set
            {
                this.selectedFrame = value;
                OnSelectFrame?.Invoke(value);
            }
        }

        public event Action<int> OnSelectFrame;

        private static readonly int[] Scales = { 4, 6, 8, 12, 16, 20, 28 };

        private static readonly Color[] SeriesPalette = {
            ToColor(0xCC7000),
            ToColor(0x5AB2BC),
            ToColor(0xfdc086),
            ToColor(0xffff99),
            ToColor(0x386cb0),
            ToColor(0xf0027f),
            ToColor(0xbf5b17),
            ToColor(0x666666)};

        private static Color ToColor(int hex)
        {
            float r = ((hex & 0xff0000) >> 0x10) / 255f;
            float g = ((hex & 0xff00) >> 8) / 255f;
            float b = (hex & 0xff) / 255f;

            return new Color(r, g, b);
        }

        public List<ISeries> Series = new List<ISeries>();

        public ChartView(params ISeries [] series)
        {
            Series.AddRange(series);
        }

        public void OnGUI(Rect rect)
        {
            if (mat == null)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                mat = new Material(shader);
            }

            if (Event.current.type == EventType.MouseDown)
            {
                OnMouseDown(rect, Event.current);
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.LeftArrow)
            {
                SelectedFrame -= 1;

                Event.current.Use();
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.RightArrow)
            {
                SelectedFrame += 1;

                Event.current.Use();
            }

            if (Event.current.type == EventType.Repaint)
            {
                RectInt axisBounds = AxisBounds();
                GUI.BeginClip(rect);
                GL.PushMatrix();
                try
                {
                    GL.Clear(true, false, Color.black);
                    mat.SetPass(0);

                    Rect glRect = new Rect(0, 0, rect.width, rect.height);
                    ClearBackground(glRect, Color.black);

                    acccumulatedValues.Clear();
                    for (int i = 0; i < Series.Count; i++)
                    {
                        DrawSeries(rect, axisBounds, Series[i], SeriesPalette[i % SeriesPalette.Count()]);
                    }

                    DrawGrid(rect, axisBounds);

                    DrawSelectedFrame(rect, axisBounds);

                    DrawLegend(rect);
                }
                finally
                {
                    GL.PopMatrix();
                    GUI.EndClip();
                }
            }
        }

        private void OnMouseDown(Rect rect, Event current)
        {
            Vector2 position = current.mousePosition;

            // was the click for us?
            if (!rect.Contains(position))
                return;

            // need to map to axis
            RectInt axisBounds = AxisBounds();

            var (x, _) = ScreenToDataSpace(rect, axisBounds, position);

            SelectedFrame = Mathf.RoundToInt(x);
        }

        private (float, float) ScreenToDataSpace(Rect rect, RectInt axisBounds, Vector2 position)
        {
            Vector2 normalized = (position - rect.min) / rect.size ;

            float x = axisBounds.width * normalized.x + axisBounds.xMin;
            float y = axisBounds.yMax - axisBounds.height * normalized.y ;
            return (x, y);
        }

        private void DrawSelectedFrame( Rect rect, RectInt axisBounds)
        {
            int selected =  SelectedFrame;

            if (selected >= 0)
            {
                GL.Begin(GL.LINES);

                Vector2 selectedPosition = Project(selected, 0, axisBounds, rect);
                GL.Color(Color.yellow);

                GL.Vertex3(selectedPosition.x, 0, 0);
                GL.Vertex3(selectedPosition.x, rect.height, 0);
                GL.End();
            }
            
        }

        private RectInt AxisBounds()
        {
            Rect dataBounds = DataBounds();
            int ymin = 0;
            int ymax = PrettyScale(dataBounds.yMax);
            int xmax = Mathf.RoundToInt(dataBounds.xMax);
            int xmin = xmax - MaxFrames;

            return new RectInt(xmin, ymin, xmax - xmin, ymax - ymin);

        }

        private int PrettyScale(float max)
        {
            int baseScale = 1;

            while (true)
            {
                foreach (int scale in Scales)
                {
                    if (scale * baseScale > max)
                        return scale * baseScale;
                }

                baseScale *= 10;
            }
        }


        List<float> stackedValues = new List<float>();

        private Rect DataBounds()
        {
            Vector2 min = new Vector2Int(int.MaxValue, 0);
            Vector2 max = new Vector2Int(int.MinValue, int.MinValue);

            stackedValues.Clear();

            foreach (ISeries serie in Series)
            {
                int index = 0;

                foreach (var point in serie.Data)
                {
                    if (stackedValues.Count <= index)
                    {
                        stackedValues.Add(0);
                    }

                    float newvalue = stackedValues[index] + point.value;

                    max.y = Math.Max(max.y, newvalue);
                    max.x = Math.Max(max.x, point.id);
                    min.x = Math.Min(min.x, point.id);

                    stackedValues[index] = newvalue;

                    index++;
                }
            }

            if (min.x > max.x)
                min = max = new Vector2Int(0, 0);

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        List<float> acccumulatedValues = new List<float>();

        private void DrawSeries(Rect rect, RectInt axisBounds, ISeries series, Color color)
        {
            GL.Begin(GL.TRIANGLE_STRIP);
            GL.Color(color);

            int pframe = -1;

            float pLow = 0;
            float pHigh = 0;

            float low = 0;
            float high = 0;

            int index = 0;

            foreach (var point in series.Data)
            {
                if (pframe < point.id - 1)
                {
                    // there were empty frames,  need to draw zeros
                    DrawDataPoint(rect, axisBounds, pframe, pLow, pHigh, pframe+1, 0, 0);
                    DrawDataPoint(rect, axisBounds, pframe+1, 0, 0, point.id - 1, 0, 0);
                    pLow = 0;
                    pHigh = 0;
                    pframe = point.id - 1;
                }

                if (acccumulatedValues.Count <= index)
                {
                    acccumulatedValues.Add(0);
                }

                low = acccumulatedValues[index];
                high = low + point.value;

                DrawDataPoint(rect, axisBounds, pframe, pLow, pHigh, point.id, low, high);

                acccumulatedValues[index] = high;

                pLow = low;
                pHigh = high;

                pframe = point.id;
                index++;
            }
            GL.End();
        }

        private void DrawDataPoint(Rect rect, RectInt axisBounds, int pframe, float pLow, float pHigh, int frame, float low, float high)
        {
            // assume we ended the strip in ph
            if (frame >= axisBounds.xMin && frame <= axisBounds.xMax)
            {
                Vector2 pl = Project(pframe, pLow, axisBounds, rect);
                Vector2 ph = Project(pframe, pHigh, axisBounds, rect);

                Vector2 l = Project(frame, low, axisBounds, rect);
                Vector2 h = Project(frame, high, axisBounds, rect);

                GL.Vertex3(h.x, h.y, 0);
                GL.Vertex3(pl.x, pl.y, 0);
                GL.Vertex3(l.x, l.y, 0);
                GL.Vertex3(h.x, h.y, 0);
            }
        }

        private Vector2 Project(int x, float y, RectInt axisBounds, Rect rect)
        {
            float px = rect.width * (x - axisBounds.xMin) / axisBounds.width;
            float py = rect.height * (y - axisBounds.yMin) / axisBounds.height;

            return new Vector2(px, rect.height - py);
        }

        private void DrawGrid(Rect rect, RectInt axisBounds)
        {
            Vector2 labelSize = Vector2.zero;

            for (int i = 1; i < 4; i++)
            {
                float f = 0.4f;
                int lineValue = i * axisBounds.height / 4;

                string labelTxt = lineValue.ToString();

                GUIStyle labelStyle = new GUIStyle();
                labelStyle.normal.textColor = new Color(f, f, f, 1);
                labelStyle.alignment = TextAnchor.MiddleLeft;

                Rect labelPosition = new Rect(2, rect.height * (4 - i) / 4 - 20, rect.width, 40);
                GUI.Label(labelPosition, labelTxt, labelStyle);

                Vector2 size = labelStyle.CalcSize(new GUIContent(labelTxt));
                labelSize = Vector2.Max(labelSize, size);
            }

            mat.SetPass(0);
            GL.Begin(GL.LINES);

            // 4 lines
            for (int i = 1; i < 4; i++)
            {
                float f = 0.2f;
                GL.Color(new Color(f, f, f, 1));

                GL.Vertex3(2 + 2 + labelSize.x, i * rect.height / 4, 0);
                GL.Vertex3(rect.width, i * rect.height / 4, 0);
            }
            GL.End();

        }

        private static void ClearBackground(Rect rect, Color color)
        {
            GL.Begin(GL.QUADS);
            GL.Color(color);
            GL.Vertex3(rect.xMin, rect.yMin, 0);
            GL.Vertex3(rect.xMax, rect.yMin, 0);
            GL.Vertex3(rect.xMax, rect.yMax, 0);
            GL.Vertex3(rect.xMin, rect.yMax, 0);
            GL.End();
        }

        public void DrawLegend(Rect rect)
        {
            Rect areaRect = new Rect(4, 4, rect.width, rect.height);
            float width = 0;
            float height = 0;

            for (int i = 0; i < Series.Count; i++)
            {
                GUIStyle style = GetSeriesStyle(i);
                string legend = string.Format(Series[i].Legend, Series[i].Average());
                Vector2 size = style.CalcSize(new GUIContent(legend));
                width = Mathf.Max(width, size.x);
                height = size.y;
            }

            ClearBackground(new Rect(2, 2, width + 4, height + (Series.Count-1) * 15 + 4), Color.black);

            for (int i = 0; i< Series.Count; i++)
            {
                GUIStyle style = GetSeriesStyle(i);
                string legend = string.Format(Series[i].Legend, Series[i].Average());
                GUI.Label(areaRect, legend, style);
            }
        }

        private GUIStyle GetSeriesStyle(int i)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = SeriesPalette[i % SeriesPalette.Count()];
            style.contentOffset = new Vector2(0, (Series.Count - i - 1) * 15);
            style.alignment = TextAnchor.UpperLeft;
            return style;
        }
    }
}
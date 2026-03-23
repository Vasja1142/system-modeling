using ScottPlot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace GasStation
{
    public class GraphService
    {
        private List<double> CmlAvr {  get; set; } = new List<double>();

        private int NCrit { get; set; }

        private int N2Crit { get; set; }

        public GraphService(List<double> cmlAvr, int n2Crit) 
        {
            CmlAvr = new List<double>(cmlAvr);
            N2Crit = n2Crit;
            NCrit = n2Crit / 2;
        }

        private string GeneratePlotBase64(List<double> cmlAvg, int nCrit)
        {
            var plt = new Plot();

            // Настройка осей
            plt.XLabel("<color=white>Количество заявок (N)", size: 14);
            plt.YLabel("Значение", size: 14);
            plt.Title("Определение N_{крит} (сходимость процесса к мат. ожиданию)",
                      size: 16);

            // Данные
            double[] x = Enumerable.Range(0, cmlAvg.Count).Select(i => (double)i).ToArray();
            double[] y = cmlAvg.ToArray();

            // Основной график
            var scatter = plt.Add.Scatter(x, y);
            scatter.Color = ScottPlot.Color.FromColor(System.Drawing.Color.Cyan);
            scatter.LineWidth = 2;
            scatter.LegendText = "CML_AVG (Сходимость)";

            // Горизонтальные линии
            var line1 = plt.Add.HorizontalLine(1.1);
            line1.LegendText = "Верхняя граница (1.1)";

            var line2 = plt.Add.HorizontalLine(0.9);
            line2.LegendText = "Нижняя граница (0.9)";

            var line3 = plt.Add.HorizontalLine(1);
            line2.LegendText = "Идеальный центр";

            // Точка N_crit
            if (nCrit >= 0 && nCrit < cmlAvg.Count)
            {
                var point = plt.Add.Scatter(nCrit, cmlAvg[nCrit]);
                point.Color = Colors.Gold;
                point.MarkerSize = 12;
                point.MarkerShape = MarkerShape.FilledCircle;

                // Вертикальная линия
                var vline = plt.Add.VerticalLine(nCrit, color: Colors.Gold);
                vline.LineWidth = 2;

                // Аннотация
                var annotation = plt.Add.Text(
                    $"N_крит = {nCrit}",
                    nCrit + cmlAvg.Count * 0.03,
                    cmlAvg[nCrit] + 0.15
                );
            }

            // Плашка с результатами
            string infoText = $"РЕЗУЛЬТАТЫ АНАЛИЗА:\n" +
                             $"Точка входа в коридор: N_крит = {nCrit}\n" +
                             $"Удвоенное значение: 2 * N_крит = {nCrit * 2}";

            var textBox = plt.AddText(infoText, 50, 50, size: 11, color: Color.White);
            textBox.FontBold = true;
            textBox.BackgroundColor = Color.FromArgb(180, 34, 34, 34);
            textBox.BorderColor = Color.Cyan;
            textBox.BorderWidth = 1;

            // Настройка сетки
            plt.Grid(color: ScottPlot.Color.FromArgb(68, 68, 68), lineStyle: LineStyle.Dot);

            // Легенда
            plt.Legend(location: Alignment.UpperRight, fontSize: 10);

            // Конвертация в Base64
            using (var ms = new MemoryStream())
            {
                plt.SaveFig(ms);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        private void AddHorizontalLine(Plot plt, double y, float width, LineStyle style, string label)
        {
            var line = plt.Add.HorizontalLine(y);
            line.LineWidth = width;
            line.LineStyle = style;
            line.Text = label;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using ScottPlot;

namespace ConsoleApp1
{
    public static class FigureDrawer
    {
        public static void GenerateNCritPlot(List<double> cmlAvg, int nCrit)
        {
            var plt = new Plot();

            // Настройка осей
            plt.XLabel("Количество заявок (N)", size: 14);
            plt.YLabel("Значение", size: 14);
            plt.Title("Определение N_{крит} (сходимость процесса к мат. ожиданию)",
                      size: 16);

            // Данные
            double[] x = Enumerable.Range(0, cmlAvg.Count).Select(i => (double)i).ToArray();
            double[] y = cmlAvg.ToArray();

            // Основной график
            var scatter = plt.Add.Scatter(x, y);
            scatter.Color = Color.FromColor(System.Drawing.Color.Cyan);
            scatter.LineWidth = 2;
            scatter.LegendText = "CML_AVG (Сходимость)";

            // Горизонтальные линии
            var line1 = plt.Add.HorizontalLine(1.1);
            line1.LegendText = "Верхняя граница (1.1)";

            var line2 = plt.Add.HorizontalLine(0.9);
            line2.LegendText = "Нижняя граница (0.9)";

            var line3 = plt.Add.HorizontalLine(1);
            line3.LegendText = "Идеальный центр";


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
                    nCrit,
                    cmlAvg[nCrit] + 0.09
                );
            }

            // Плашка с результатами
            string infoText = $"РЕЗУЛЬТАТЫ АНАЛИЗА:\n" +
                             $"Точка входа в коридор: N_крит = {nCrit}\n" +
                             $"Удвоенное значение: 2 * N_крит = {nCrit * 2}";


            plt.Add.Annotation(infoText, Alignment.UpperCenter);

            plt.SaveSvg("../../../../../ncrit_vis.svg", 1400, 700);
        }

        public static void GenerateCarsPlot(int n2Crit, List<Car> cars)
        {
            var plt = new Plot();

            Dictionary<string, double> yLevels = new Dictionary<string, double>()
            {
                {"DOS", 100},
                {"SERVICE", 200 },
                {"KO 2", 300 },
                {"KO 1", 400 },
                {"CARS", 500 }
            };

            foreach (var level in yLevels.Values)
            {
                var levelLine = plt.Add.HorizontalLine(level);
                levelLine.Color = Colors.Black;
            }

            for (int i = 0; i < 21; i++)
            {
                var hourLine = plt.Add.VerticalLine(i);
                hourLine.Color = Colors.Black;
            }

            int colorsCounter = -1;
            int deniedCars = 0;

            int figWidth = Math.Max(4000, n2Crit * 200);

            for (int i = 0; i < n2Crit; i++) 
            {
                var car = cars[i];
                colorsCounter++;

                if (colorsCounter >= Colors.Category10.Length)
                    colorsCounter = 0;

                var color = Colors.Category10[colorsCounter];

                var scatter = plt.Add.Scatter(car.ArrivalTime, yLevels["CARS"]);

                //Оффсеты потому что ScottPlot по рофлу делает свои оффсет всему тексту
                var carArrivalText = plt.Add.Text($"#{i + 1}", car.ArrivalTime - 0.005f, yLevels["CARS"] + 20f);
                carArrivalText.LabelFontSize = 15;

                scatter.Color = color;
                carArrivalText.LabelFontColor = color;

                //Машину отклонили
                if (car.StationNumber == -1)
                {
                    deniedCars++;

                    var denialArrow = plt.Add.Arrow(new Coordinates(car.ArrivalTime, yLevels["CARS"]), 
                        new Coordinates(car.ArrivalTime, yLevels["DOS"]));

                    denialArrow.ArrowFillColor = color;
                    denialArrow.ArrowLineColor = color;

                    var xMark = plt.Add.Text($"x", car.ArrivalTime - 0.003f, yLevels["DOS"] + 10f);
                    xMark.LabelFontColor = color;
                    xMark.LabelFontSize = 20;
                }
                //Машину приняли
                else
                {
                    string yLevel = car.StationNumber == 1 ? "KO 1" : "KO 2";

                    var arrowToStation = plt.Add.Arrow(new Coordinates(car.ArrivalTime, yLevels["CARS"]),
                        new Coordinates(car.ArrivalTime, yLevels[yLevel]));

                    arrowToStation.ArrowFillColor = color;
                    arrowToStation.ArrowLineColor = color;

                    var progressArrow = plt.Add.Arrow(new Coordinates(car.ArrivalTime, yLevels[yLevel]),
                        new Coordinates(car.LeavingTime, yLevels[yLevel]));

                    progressArrow.ArrowFillColor = color;
                    progressArrow.ArrowLineColor = color;

                    var arrowToService = plt.Add.Arrow(new Coordinates(car.LeavingTime, yLevels[yLevel]),
                        new Coordinates(car.LeavingTime, yLevels["SERVICE"]));

                    arrowToService.ArrowFillColor = color;
                    arrowToService.ArrowLineColor = color;
                }
            }

            plt.XLabel("Время (часы)");

            plt.Axes.SetLimitsX(0, 21);

            plt.Axes.Left.SetTicks(yLevels.Values.ToArray(), yLevels.Keys.ToArray());

            plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(1.0);

            string title = $"Полная симуляция: {n2Crit} машин. Шаг сетки: 15 минут. " +
                $"Выполнил: студент гр. РИС2-23-3б Герасимов Максим Николаевич. " +
                $"λ={GasStationSimulation.Lambda}, μ1={GasStationSimulation.Mu1}, μ2={GasStationSimulation.Mu2}, ε=10%, " +
                $"2Nкрит={n2Crit}. " +
                $"В среднем {((double) deniedCars / n2Crit).ToString("F2")} отказов в час, всего отказов - {deniedCars}";

            plt.Title(title, 22);

            plt.SaveSvg("../../../../../cars_plot.svg", figWidth, 500);
        }
    }
}

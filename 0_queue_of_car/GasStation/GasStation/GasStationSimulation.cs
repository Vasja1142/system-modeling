using CsvHelper;
using GasStation.Models;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace GasStation
{
    public static class GasStationSimulation
    {
        private const int Lambda = 9;

        private const int Mu1 = 6;

        private const int Mu2 = 3;

        private const int Seed = 2;

        private const int SampleOfCars = 200;

        private static List<double> CmlAvr = new List<double>();

        private static Random rng;

        private static double StartWindow;

        private static double EndWindow;

        //Список с временами приезда машин
        public static List<Car> Cars { get; set; } = new List<Car>();

        public static bool RunSimulation(out Exception exception, out List<Car> cars)
        {
            exception = null;
            cars = null;

            try
            {
                InitRng();
                GenerateCars();
                ModelServiceProcess();

                int doubleNCrytical = 2 * CalculateNCrytical();

                StartWindow = 1;
                EndWindow = StartWindow + Cars[doubleNCrytical - 1].ArrivalTime;

                double probabilityServed = Cars.Count(i => i.StationNumber != -1) / Cars.Count;
                double probabilityDenial = Cars.Count(i => i.StationNumber == -1) / Cars.Count;

                double gasStationCapacity = GasStationCapacity();
                (double probabilityBusy1, double probabilityBusy2) = ProbabilityBusyStation();

                double nColumnsAvarage = probabilityBusy1 + 2 * probabilityBusy2;

                (double probabilityIdle1, double probabilityIdle2) = ProbabilityIdleStations();

                double avarageTimeInStations = AvarageTimeInStations(nColumnsAvarage, gasStationCapacity);

                cars = Cars;

                SaveAsCsv(gasStationCapacity, probabilityServed, probabilityDenial, 
                    probabilityBusy1, probabilityBusy2, 
                    nColumnsAvarage, probabilityIdle1, probabilityIdle2, avarageTimeInStations);

                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            } 
        }

        private static void SaveAsCsv(double gasStationCapacity, double probabilityServed, double probabilityDenied, 
            double probabilityBusySt1, double probabilityBusySt2, double nColumnsAvarage, double probabilityIdleStation1, 
            double probabilityIdleStation2, double avarageTimeInStation)
        {
            string executablePath = Assembly.GetExecutingAssembly().Location;
            string projectDirectory = Path.GetDirectoryName(executablePath);

            string projectRoot = Directory.GetParent(projectDirectory).Parent.Parent.FullName;

            string filePath = "results.csv";

            var results = new[]
            {
                new SimulationResult { Parameter = "a", Value = gasStationCapacity, Unit = "car/hour" },
                new SimulationResult { Parameter = "p_service", Value = probabilityServed, Unit = "probability" },
                new SimulationResult { Parameter = "p_reject", Value = probabilityDenied, Unit = "probability" },
                new SimulationResult { Parameter = "p_busy_ko1", Value = probabilityBusySt1, Unit = "probability" },
                new SimulationResult { Parameter = "p_busy_ko2", Value = probabilityBusySt2, Unit = "probability" },
                new SimulationResult { Parameter = "n_columns_avg", Value = nColumnsAvarage, Unit = "cars" },
                new SimulationResult { Parameter = "p_idle_ko1", Value = 1 - probabilityIdleStation1, Unit = "probability" },
                new SimulationResult { Parameter = "p_idle_ko2", Value = 1 - probabilityIdleStation2, Unit = "probability" },
                new SimulationResult { Parameter = "r_avg", Value = 0, Unit = "cars" }, //Ну... у меня очередй нет :)
                new SimulationResult { Parameter = "t_wait_avg", Value = 0, Unit = "hour" },
                new SimulationResult { Parameter = "t_service_avg", Value = avarageTimeInStation, Unit = "hour" },
                new SimulationResult { Parameter = "t_system_avg", Value = avarageTimeInStation, Unit = "hour" },
                new SimulationResult { Parameter = "n_avg", Value = nColumnsAvarage, Unit = "cars" }
            };

            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.WriteField("Параметер");
            csv.WriteField("Значение");
            csv.WriteField("Единица измерения");
            csv.NextRecord();

            foreach (var result in results)
            {
                csv.WriteField(result.Parameter);
                csv.WriteField(result.Value);
                csv.WriteField(result.Unit);
                csv.NextRecord();
            }
        }

        private static void InitRng()
        {
            rng = new Random(Seed);
        }

        private static void GenerateCars()
        {
            double counterTaus = 0;
            List<double> taus = new List<double>();

            for (int i = 0; i < SampleOfCars; i++)
            {
                double tau = -1 * Math.Log(rng.NextDouble()) / Lambda;
                counterTaus += tau;

                CmlAvr.Add(9 * counterTaus / (i + 1));

                Car car = new Car(counterTaus);

                Cars.Add(car);
                taus.Add(tau);
            }
        }

        private static void ModelServiceProcess()
        {
            foreach (Car car in Cars) 
            {
                //Попытка отправить на первую бензаколонку
                if ((Cars.LastOrDefault(c => c.StationNumber == 1)?.LeavingTime ?? -1) <= car.ArrivalTime)
                {
                    car.StationNumber = 1;
                    car.LeavingTime = car.ArrivalTime + (-1 * Math.Log(rng.NextDouble()) / Mu1);
                }
                //Попытка отправить на вторую бензаколонку
                else if ((Cars.LastOrDefault(c => c.StationNumber == 2)?.LeavingTime ?? -1) <= car.ArrivalTime)
                {
                    car.StationNumber = 2;
                    car.LeavingTime = car.ArrivalTime + (-1 * Math.Log(rng.NextDouble()) / Mu2);
                }
                //Отказ
                else
                    car.StationNumber = -1;
            }
        }

        private static int CalculateNCrytical()
        {
            for (int i = CmlAvr.Count - 1; i < 0; i--) 
            {
                double value = CmlAvr[i];

                if (value < 0.9d || value > 1.1d)
                    return i + 1;
            }

            throw new Exception("Failed to calculate N crytical!");
        }

        private static double GasStationCapacity() => Cars.Count(c => c.StationNumber != -1 
        && c.LeavingTime > StartWindow && c.ArrivalTime < EndWindow) / (EndWindow - StartWindow);

        private static double AvarageTimeInStations(double avarage, double gasStationCapacity) => gasStationCapacity > 0 
            ? avarage / gasStationCapacity : 0;

        private static (double, double) ProbabilityBusyStation()
        {
            List<(double time, int stationsDelta)> events = new List<(double time, int stationsDelta)> ();

            foreach (var car in Cars.Where(c => c.StationNumber != -1 && 
            c.LeavingTime > StartWindow && c.ArrivalTime < EndWindow)) 
            {
                double normalizedStart = Math.Max(StartWindow, car.ArrivalTime);
                double normalizedEnd = Math.Min(EndWindow, car.LeavingTime);

                events.Add((normalizedStart, 1));
                events.Add((normalizedEnd, -1));
            }

            events.OrderBy(e => e.time);

            int busyStations = 0;
            double lastEventTime = StartWindow;

            Dictionary<int, double> busyStationsTime = new Dictionary<int, double>() 
            {
                {1, 0d},
                {2, 0d},
            };

            foreach (var @event in events) 
            {
                double delta = @event.time - lastEventTime;

                if (busyStations != 0)
                    busyStationsTime[busyStations] += delta;

                busyStations += @event.stationsDelta;

                lastEventTime = @event.time;
            }

            double totalTime = EndWindow - StartWindow;

            return (busyStationsTime[1] / totalTime, busyStationsTime[2] / totalTime);
        }

        private static (double, double) ProbabilityIdleStations()
        {
            double totalTime = EndWindow - StartWindow;

            double busyStationOne = Cars.Where(c => c.StationNumber == 1 
            && c.LeavingTime > StartWindow && c.ArrivalTime < EndWindow).Select(c => Math.Min(c.LeavingTime, EndWindow) - 
            Math.Max(c.ArrivalTime, StartWindow)).Sum();

            double busyStationTwo = Cars.Where(c => c.StationNumber == 2
            && c.LeavingTime > StartWindow && c.ArrivalTime < EndWindow).Select(c => Math.Min(c.LeavingTime, EndWindow) -
            Math.Max(c.ArrivalTime, StartWindow)).Sum();

            return (1 - busyStationOne / totalTime, 1 - busyStationTwo / totalTime);
        }
    }
}

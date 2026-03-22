using ConsoleApp1;

GasStationSimulation.RunSimulation(out var exception, out var cars);

if (exception != null)
    Console.WriteLine(exception);

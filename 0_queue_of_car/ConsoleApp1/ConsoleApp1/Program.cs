using ConsoleApp1;

GasStationSimulation.RunSimulation(out var cars, out int doubleNCrit);

FigureDrawer.GenerateNCritPlot(GasStationSimulation.CmlAvr, doubleNCrit / 2);
FigureDrawer.GenerateCarsPlot(doubleNCrit, cars);

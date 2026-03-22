namespace GasStation.Models
{
    public class Car
    {
        //Время прибытия на заправку
        public double ArrivalTime { get; set; }

        //Время покидания заправки
        public double LeavingTime { get; set; }

        //Номер колонки (0 - колонка еще не определена, -1 - машине отказали в обслуживании)
        public int StationNumber { get; set; }

        public Car(double arrivalTime) 
        {
            ArrivalTime = arrivalTime;
            StationNumber = 0;
        }
    }
}

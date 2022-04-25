using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using Excel = Microsoft.Office.Interop.Excel;
using System.Threading;


namespace SIMSrq3
{
    interface ITimeObservable {
        void AddTimer(IEnumerable<ITimeObserver> timers);
        void UpdateTime(double deltaTime);
    }
    interface ITimeObserver
    {
        void CorrectTime(double deltaTime);
    }
    class Model: ITimeObservable
    {
        Dictionary<int, List<double>> orbitTimes;
        private HashSet<ITimeObserver> timers = new HashSet<ITimeObserver>();
        string path = "iamlog.txt";
        string str;
        public bool isRunning;

        Input input;
        Server firstPhase;
        Server secondPhase;
        Orbit orbit;

        public double currentTime;

        public Dictionary<int,double> orbitLength;

        int inputCalls;
        int outputCalls;
        int orbitCalls;

        public double maxTime;
        double deltaTime;
        double callFromOrbitTime;

        public List<double> times;
        public int eventCount;

        int k;

        Excel.Application excelApp = new Excel.Application();
        Excel.Workbook workBook;
        Excel.Worksheet workSheet;

        public int simulationsCount;

        public Model()
        {
            workBook = excelApp.Workbooks.Add();
        }

        public void AddTimer(IEnumerable<ITimeObserver> times) {
            foreach (ITimeObserver time in times)
            {
                timers.Add(time);
            }
        }

        public void UpdateTime(double deltaTime)
        {
            foreach (ITimeObserver timer in timers)
            {
                timer.CorrectTime(deltaTime);
            }
        }
        
        public void StartSimulation()
        {
            isRunning = true;
            input.GenerateEvent();
            GetNewCall();
            while (currentTime < maxTime)
            {
                FindClosestTime();
            }
            EndSimulation();
        }

        public void EndSimulation()
        {
            foreach (var item in orbitTimes)
            {
                orbitLength[item.Key] = item.Value.Sum()/maxTime;
            }
            isRunning = false;
            File.WriteAllText(path, str);
            LogToExcel();
        }

        public void ResetModel(double lambda, double mu1, double mu2, double sigma)
        {
            times = new List<double>();

            input = new Input(lambda);
            firstPhase = new Server(mu1);
            secondPhase = new Server(mu2);
            orbit = new Orbit(sigma);
            AddTimer(new List<ITimeObserver>() { input, firstPhase, secondPhase, orbit });


            orbitLength = new Dictionary<int, double>();
            orbitTimes = new Dictionary<int, List<double>>();            

            inputCalls = 0;
            outputCalls = 0;
   
            currentTime = 0;
        }

        /// <summary>
        /// Переход к ближайшему событию
        /// </summary>
        public void FindClosestTime()
        {
            deltaTime = 10000;
            callFromOrbitTime = orbit.GetClosestTime();

            times.AddRange(new List<double> { input.newCallTime, firstPhase.servingTime, secondPhase.servingTime, callFromOrbitTime });
            times.RemoveAll(c => c == 0);
            deltaTime = times.Min();

            k =-1;
            if (deltaTime == input.newCallTime) k = 0;
            if (deltaTime == firstPhase.servingTime) k = 1;
            if (deltaTime == secondPhase.servingTime) k = 2;
            if (deltaTime == callFromOrbitTime) k = 3;
            
            currentTime += deltaTime;
            orbitTimes.TryAdd(orbit.currentOrbitTime.Count, new List<double>());
            orbitTimes[orbit.currentOrbitTime.Count].Add(deltaTime);

            UpdateTime(deltaTime);
            switch (k)
            {
                //Приход новой заявки
                case 0:
                    inputCalls++;
                    input.GenerateEvent();
                    GetNewCall();
                    LogToTxt(" Новая заявка");
                    break;
                //Конец обслуживания на первом приборе первой фазы
                case 1:
                    firstPhase.EndServing();                   
                    NewCallInSecondPhase();
                    LogToTxt(" Конец обслуживания на первом приборе 1 фазы");
                    break;                
                //Конец обслуживания на второй фазе
                case 2:
                    outputCalls++;
                    secondPhase.EndServing();
                    LogToTxt(" Конец обслуживания на 2 фазе");
                    break;
                //Обращение заявки с орбиты
                case 3:
                    orbit.RemoveCall();
                    GetNewCall();
                    LogToTxt(" Вызов с орбиты");
                    break;
                default:
                    LogToTxt(" Неизвестное событие");
                    break;
            }
            times.Clear();
        }

        public void GetNewCall()
        {
            if (firstPhase.isServing)
            {
                orbit.NewCall();
                orbit.NewCall();
                firstPhase.EndServing();
            }
            else if (secondPhase.isServing)
            {
                orbit.NewCall();
            }
            else
                firstPhase.GetCall();
        }

        public void NewCallInSecondPhase()
        {
            if (secondPhase.isServing)
            {
                orbit.NewCall();
            }
            else
                secondPhase.GetCall();
        }

        public void LogToExcel()
        {
            workSheet = (Excel.Worksheet)workBook.Worksheets.get_Item(1);
            int i = 1;
            foreach (var call in orbitLength)
            {
                i++;
                workSheet.Cells[i, 1] = call.Key;
                workSheet.Cells[i, 2] = call.Value;
            }
        }

        public void LogToTxt(string _event)
        {
            orbitCalls = orbit.currentOrbitTime.Count;
            eventCount++;
            str += eventCount.ToString() + _event;
            str += "\nПромежутки времени:";
            str += "\nПриход новой заявки: " + input.newCallTime;
            str += "\nКонец обслуживания на первом приборе 1 фазы: " + firstPhase.servingTime;
            str += "\nКонец обслуживания на 2: " + secondPhase.servingTime;
            str += "\nВремя обращения с орбиты: " + orbit.GetClosestTime();
            str += "\nВходящие: " + inputCalls.ToString() + " Покинувшие: " + outputCalls.ToString();
            str += "\nОрбита:";
            foreach (double call in orbit.currentOrbitTime)
            {
                str += "\n" + call.ToString();
            }
            str += "\n\n";
        }

        public void OpenTab()
        {
            excelApp.Visible = true;
            excelApp.UserControl = true;
        }     
    }
}

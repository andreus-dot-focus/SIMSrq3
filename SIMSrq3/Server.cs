using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SIMSrq3
{
    class Server: ITimeObserver
    {
        public double mu;
        public bool isServing;

        public double servingTime;

        public Server(double mu)
        {
            this.mu = mu;
            isServing = false;
            servingTime = 0;
        }
        public void GetCall()
        {
            isServing = true;
            servingTime = Calc.ExpDist(mu);            
        }
        public void EndServing()
        {
            isServing = false;
            servingTime = 0;
        }
        public void CorrectTime(double deltaTime)
        {
            if(servingTime!=0)
                servingTime -= deltaTime;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rerunner.Util
{
    public class DelayExecution
    {
        private readonly int _delayTime;
        private readonly Timer _timer;
        public DelayExecution(Action action, int delayTime)
        {
            _delayTime = delayTime;
            _timer = new Timer(new TimerCallback(_ =>
            {
                action();
            }));
        }

        public void Start()
        {
            _timer.Change(_delayTime, Timeout.Infinite);
        }
    }
}

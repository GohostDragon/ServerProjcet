using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using ServerCore;

namespace Server
{
    struct JobTimerElem : IComparable<JobTimerElem>
    {
        public int execTick; // 실행시간
        public Action action;

        public int CompareTo(JobTimerElem other)
        {
            return other.execTick - execTick;
        }
    }

    class JobTimer
    {
        PriorityQueue<JobTimerElem> _priQueue = new PriorityQueue<JobTimerElem>();
        object _lock = new object();

        public static JobTimer Instance { get; } = new JobTimer();
        
        public void Push(Action action, int tickAfter = 0)
        {
            JobTimerElem job;
            job.execTick = System.Environment.TickCount + tickAfter;
            job.action = action;

            lock (_lock) {
                _priQueue.Push(job);
            }
        }

        public void Flush()
        {
            while (true) {
                int now = System.Environment.TickCount;

                JobTimerElem job;

                lock (_lock) {
                    // While 나가자
                    if(_priQueue.Count == 0) {
                        break;
                    }

                    job = _priQueue.Peek();
                    if(job.execTick > now) {
                        break;
                    }

                    // 잡 실행
                    _priQueue.Pop();
                }

                job.action.Invoke();
            }
        }
    }
}

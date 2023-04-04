using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    public interface IJobQueue
    {
        void Push(Action job);
    }

    public class JobQueue : IJobQueue
    {
        // 내가 해야하는 일감들을 가지고 있는 큐
        Queue<Action> _jobQueue = new Queue<Action>();
        object _lock = new object();
        bool _flush = false;

        public void Push(Action job)
        {
            bool flush = false;

            lock (_lock) {
                _jobQueue.Enqueue(job);

                if(_flush == false) {
                    flush = _flush = true;
                }
            }

            if (flush) {
                Flush();
            }
        }

        private void Flush()
        {
            while (true) {
                // 팝이 락 처리 되어있으므로 멀티스레드에서도 안전
                Action action = Pop();
                if (action == null) {
                    return;
                }

                action.Invoke();
            }
        }

        private Action Pop()
        {
            lock (_lock) {
                if(_jobQueue.Count == 0) {
                    // 일 다 끝났으므로 다른 작업자가 할 수 있게 함
                    _flush = false;
                    return null;
                }
                return _jobQueue.Dequeue();
            }
        }
    }
}

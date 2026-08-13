using System.Collections.Generic;

namespace MabinogiMobileServer
{
    class JobManager
    {
        // singleton //
        private JobManager() { }
        private static JobManager? _inst;
        public static JobManager Instance
        {
            get
            {
                if (_inst is null)
                    _inst = new JobManager();
                return _inst;
            }
        }

        // manage job //
        private Queue<dynamic> jobQueue = new Queue<dynamic>();
        private object jobQueueLock = new object();
        public void EnqueueJob(dynamic job)
        {
            lock(jobQueueLock)
            {
                jobQueue.Enqueue(job);
            }
        }
        public void RunJob()
        {
            int runCount = 0;
            lock (jobQueue)
            {
                runCount = jobQueue.Count;
            }
            
            while (runCount > 0)
            {
                dynamic job = null!;
                lock (jobQueueLock)
                {
                    job = jobQueue.Dequeue();
                }
                
                job.Process();
                --runCount;
            }
        }
    }
}

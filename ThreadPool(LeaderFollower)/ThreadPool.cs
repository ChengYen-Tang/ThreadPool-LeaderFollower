using System.Collections.Generic;
using System.Windows.Threading;
using System.Diagnostics;
using ThreadPool.Test;

namespace System.Threading.LeaderFollower
{
    public delegate void DoWorkDelegate(object ThisThreadItem);
    public class EasyThreadPool : IDisposable
    {
        private ThreadpoolController Controller;
        private ThreadItem LeaderThread;
        private Queue<ThreadItem> IdleThreadQueue;
        private PriorityQueue<WorkItem> WorkQueue = new PriorityQueue<WorkItem>();
        private bool disposed = false;
        private object WorkQueueLock = new object();


        //Idle Time 60S 是參考以下網址
        //https://doc.zeroc.com/pages/viewpage.action?pageId=16716652#Ice.ThreadPool.*-Ice.ThreadPool.name.ThreadIdleTime
        /// <summary>
        /// 初始化物件
        /// </summary>
        /// <param name="MaxThread">最大Thread數量</param>
        /// <param name="MinThread">最小Thread數量</param>
        /// <param name="IdleTime">閒置時間，設置為0將不會自動減少Thread數量</param>
        public EasyThreadPool(int MaxThread,int MinThread = 5,int IdleTime = 60)
        {
            if (MaxThread < MinThread)
                throw new ArgumentException("Parameter Can not be less than MinThread", "MaxThread");
            if (MinThread < 1)
                throw new ArgumentException("Parameter Can not be less than 1", "MinThread");
            if (IdleTime < 0)
                throw new ArgumentException("Parameter Can not be negative", "IdleTime");

            Controller = new ThreadpoolController(MaxThread, MinThread, IdleTime);
            IdleThreadQueue = Controller.GetIdleThreadQueue;

            LeaderThread = Controller.CreateThread(new ParameterizedThreadStart(ThreadWork));
            Debug.WriteLine("Thread[{0}] is Leader Create Done", LeaderThread._Thread.ManagedThreadId);
            for (int i = 1; i < MinThread; i++)
            {
                ThreadItem threadItem = Controller.CreateThread(new ParameterizedThreadStart(ThreadWork));
                IdleThreadQueue.Enqueue(threadItem);
                Debug.WriteLine("Thread[{0}] Create Done", threadItem._Thread.ManagedThreadId);
            }
        }

        //Thread執行的框架
        private void ThreadWork(object ThisThreadItem)   
        {
            //IsRunning是控制Thread運行的開關
            while (Controller.IsRunning && ((ThreadItem)ThisThreadItem).IsRunning)
            {
                //等待被指派為新領導，如果自己是領導將不會進入等待狀態(上次工作完成發現沒有領導，自己會成為領導)
                while(LeaderThread != ThisThreadItem && Controller.IsRunning && ((ThreadItem)ThisThreadItem).IsRunning)
                {
                    Debug.WriteLine("Thread[{0}] WaitOne", ((ThreadItem)ThisThreadItem)._Thread.ManagedThreadId);
                    ((ThreadItem)ThisThreadItem)._AutoResetEvent.WaitOne();
                }

                Controller.SetEnqueueWorkTime = DateTime.Now;

                //LeaderThread等待工作
                SpinWait.SpinUntil(() => (WorkQueue.Count != 0 || !Controller.IsRunning || !((ThreadItem)ThisThreadItem).IsRunning));

                LeaderWork(ThisThreadItem);
            }

            //Thread結束時自我釋放
            Debug.WriteLine("Thread[{0}] Close Done", ((ThreadItem)ThisThreadItem)._Thread.ManagedThreadId);
            Controller.ReduceCurrentThreadQuantity();
            Controller.RemoveThreadAutoWait(((ThreadItem)ThisThreadItem)._AutoResetEvent);
            ((ThreadItem)ThisThreadItem)._AutoResetEvent.Dispose();
            ((ThreadItem)ThisThreadItem)._Thread = null;
        }

        //LeaderThread取工作->指派新的Leader->開始工作->回到idleQueue
        private int LeaderWork(object ThisThreadItem)
        {
            if (Controller.IsRunning && ((ThreadItem)ThisThreadItem).IsRunning)
            {
                //從WorkQueue取工作
                WorkItem Work;

                if (WorkQueue.Count != 0)
                {
                    lock (WorkQueueLock)
                        Work = WorkQueue.Dequeue();
                }
                else
                    return 0;

                //從IdleThread Queue指派Thread成為新領導，沒有Thread就新增Thread或設為null
                lock (Controller.ThreadVariableChangeLock)
                    if (IdleThreadQueue.Count != 0)
                    {
                        LeaderThread = IdleThreadQueue.Dequeue();
                        Debug.WriteLine("指派Thread[{0}]為新領導", LeaderThread._Thread.ManagedThreadId);
                        LeaderThread._AutoResetEvent.Set();
                    }
                    else
                    {
                        LeaderThread = Controller.CreateThread(new ParameterizedThreadStart(ThreadWork));
#if (LeaderThread != null)
    Debug.WriteLine("Thread[{0}] is Leader Create Done", LeaderThread._Thread.ManagedThreadId);
#else
                        Debug.WriteLine("沒有閒置的Thread");
#endif
                    }

                //做從WorkQueue取出的工作
                //如果工作的程式碼有Thread.CurrentThread.Abort，會把被Abort的Thread重置
                try
                {
                    switch (Work.GroupName)
                    {
                        case FunctionGroupName.TestFunctionGroup:
                            TestWork.TestFunctionGroup(Work);
                            break;
                        case FunctionGroupName.FeaturesFunctionGroup:
                            TestWork.FeaturesFunctionGroup(Work);
                            break;
                    }
                }
                catch
                {
                    if (Thread.CurrentThread.ThreadState == ThreadState.AbortRequested || Thread.CurrentThread.ThreadState.ToString() == "Background, AbortRequested")
                    {
                        Debug.WriteLine("Thread[{0}] 被Abort，重置Thread[{0}]", Thread.CurrentThread.ManagedThreadId);
                        Thread.ResetAbort();
                    }
                }

                //工作做完如果沒有LeaderThread會自動成為Leader，不然加入IdleThread Queue，
                lock (Controller.ThreadVariableChangeLock)
                    if (LeaderThread == null)
                    {
                        Debug.WriteLine("Thread[{0}] 成為新領導", Thread.CurrentThread.ManagedThreadId);
                        LeaderThread = ((ThreadItem)ThisThreadItem);
                    }
                    else
                    {
                        Debug.WriteLine("Thread[{0}] 進入閒置佇列", Thread.CurrentThread.ManagedThreadId);
                        IdleThreadQueue.Enqueue((ThreadItem)ThisThreadItem);
                    }
            }
            return 0;
        }

        /// <summary>
        /// 新增工作
        /// </summary>
        /// <param name="Work">工作</param>
        /// <param name="Priority">優先權</param>
        public void AddThreadWork(WorkItem Work, WorkPriority Priority = WorkPriority.Normal)
        {
            lock (WorkQueueLock)
                WorkQueue.Enqueue(Priority, Work);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            Controller.Dispose();

            if (disposing)
            {
                LeaderThread = null;
                IdleThreadQueue = null;
                WorkQueue = null;
                WorkQueueLock = null;
            }

            disposed = true;
        }

        ~EasyThreadPool()
        {
            Dispose(false);
        }
    }

    public class ThreadItem
    {
        public Thread _Thread { get; set; }
        public AutoResetEvent _AutoResetEvent { get; set; }
        public bool IsRunning { get; set; }
    }

    internal class ThreadpoolController : IDisposable
    {
        private Thread ControllerThread;
        private int CurrentThreadQuantity = 0;
        private int MaxThreadQuantity = 0;
        private int MinThreadQuantity = 0;
        private int IdleThreadTime = 0;
        private List<AutoResetEvent> AllThreadAutoWaitList = new List<AutoResetEvent>();
        private Queue<ThreadItem> IdleThreadQueue = new Queue<ThreadItem>();
        private DateTime EnqueueWorkTime = DateTime.Now;
        public bool IsRunning = true;
        private object CurrentThreadQuantityLock = new object();
        public object ThreadVariableChangeLock = new object();
        private bool disposed = false;


        public ThreadpoolController(int MaxThread,int MinThread,int IdleTime)
        {
            MaxThreadQuantity = MaxThread;
            MinThreadQuantity = MinThread;
            IdleThreadTime = IdleTime;
            ControllerThread = new Thread(ControllerWork) { IsBackground = true};
            ControllerThread.Start();
        }

        //ControllerThread的工作
        private void ControllerWork()
        {
            while(IsRunning)
            {
                SpinWait.SpinUntil(() => false, 1000);
                //釋放多餘的Thread
                if (EnqueueWorkTime.AddSeconds(IdleThreadTime) < DateTime.Now)
                    while (IdleThreadQueue.Count + 1 > MinThreadQuantity)
                    {
                        lock (CurrentThreadQuantityLock)
                            if (CurrentThreadQuantity <= MinThreadQuantity)
                                break;
                            else
                            {
                                ThreadItem threadItem;
                                lock (ThreadVariableChangeLock)
                                    threadItem = IdleThreadQueue.Dequeue();
                                threadItem.IsRunning = false;
                                threadItem._AutoResetEvent.Set();
                            }
                    }
            }
        }

        //結束所有的Thread
        private void CloseAllThread()
        {
            IsRunning = false;
            ControllerThread.Join();
            foreach (var q in AllThreadAutoWaitList)
                q.Set();

            //等待所有Thread結束
            SpinWait.SpinUntil(() => (CurrentThreadQuantity == 0));
        }

        /// <summary>
        /// 產生Thread申請
        /// 如果當前Thread數量在允許範圍內會產生新的Thread並回傳
        /// 不允許的範圍就會回傳null
        /// </summary>
        /// <param name="DoWork"></param>
        /// <returns>Thread的工作</returns>
        public ThreadItem CreateThread(ParameterizedThreadStart DoWork)
        {
            ThreadItem threadItem;
            lock (CurrentThreadQuantityLock)
                if(CurrentThreadQuantity < MaxThreadQuantity)
                {
                    threadItem = NewThreadItem(DoWork);
                    threadItem._Thread.Start(threadItem);
                }
                else
                {
                    threadItem = null;
                }
            return threadItem;
        }

        //產生Thread和一些設定
        private ThreadItem NewThreadItem(ParameterizedThreadStart DoWork)
        {
            ThreadItem NewThreadItem = new ThreadItem()
            {
                _AutoResetEvent = new AutoResetEvent(false),
                IsRunning = true,
                _Thread = new Thread(DoWork) { IsBackground = true}
            };

            AllThreadAutoWaitList.Add(NewThreadItem._AutoResetEvent);

            CurrentThreadQuantity++;
            return NewThreadItem;
        }

        //減少Thread計數器的數量
        public void ReduceCurrentThreadQuantity()
        {
            lock (CurrentThreadQuantityLock)
                CurrentThreadQuantity--;
        }

        //把指定的AutoResetEvent從AllThreadAutoWaitList移除
        public void RemoveThreadAutoWait(AutoResetEvent AutoWait)
        {
            AllThreadAutoWaitList.Remove(AutoWait);
        }

        //取得IdleThreadQueue
        public Queue<ThreadItem> GetIdleThreadQueue
        {
            get
            {
                return IdleThreadQueue;
            }
        }

        //給LeaderThread紀錄從WorkQueue拿工作的時間
        public DateTime SetEnqueueWorkTime
        {
            set
            {
                EnqueueWorkTime = value;
            }
        }

        public void Dispose()
        {
            CloseAllThread();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                ControllerThread = null;
                IdleThreadQueue.Clear();
                IdleThreadQueue = null;
                AllThreadAutoWaitList = null;
                CurrentThreadQuantityLock = null;
                ThreadVariableChangeLock = null;
            }

            disposed = true;
        }

        ~ThreadpoolController()
        {
            CloseAllThread();
            Dispose(false);
        }
    }
}

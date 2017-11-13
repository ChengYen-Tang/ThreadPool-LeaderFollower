using System;
using System.Threading.LeaderFollower;
using ThreadPool.Test;
using System.Windows.Threading;


namespace ThreadPool_LeaderFollower_
{
    class Program
    {
        static void Main(string[] args)
        {
            PriorityQueue<string> TestQueue = new PriorityQueue<string>();

            TestQueue.Enqueue(WorkPriority.Lowest, "Lowest");
            TestQueue.Enqueue(WorkPriority.BelowNormal, "BelowNormal");
            TestQueue.Enqueue(WorkPriority.Normal, "Normal");
            TestQueue.Enqueue(WorkPriority.AboveNormal, "AboveNormal");
            TestQueue.Enqueue(WorkPriority.Highest, "Highest");

            for (int i=0;i<5;i++)
            {
                Console.WriteLine(TestQueue.Dequeue());
            }

            WorkItem WorkItem0 = new WorkItem(FunctionGroupName.TestFunctionGroup, AsyncCallName.TestFunction0, (object)0,null,null);
            WorkItem WorkItem1 = new WorkItem(FunctionGroupName.TestFunctionGroup, AsyncCallName.TestFunction1, (object)1, null, null);
            WorkItem WorkItem2 = new WorkItem(FunctionGroupName.TestFunctionGroup, AsyncCallName.TestFunction2, (object)2, null, null);
            WorkItem WorkItem3 = new WorkItem(FunctionGroupName.TestFunctionGroup, AsyncCallName.TestFunction3, (object)3, null, null);
            WorkItem WorkItem4 = new WorkItem(FunctionGroupName.TestFunctionGroup, AsyncCallName.TestFunction4, (object)4, null, null);
            WorkItem WorkItem5 = new WorkItem(FunctionGroupName.TestFunctionGroup, AsyncCallName.TestFunction5, (object)5, null, null);
            WorkItem WorkItem6 = new WorkItem(FunctionGroupName.TestFunctionGroup, AsyncCallName.TestFunction6, (object)6, null, null);
            WorkItem WorkItem7 = new WorkItem(FunctionGroupName.TestFunctionGroup, AsyncCallName.TestFunction7, (object)7, null, null);
            WorkItem WorkItem8 = new WorkItem(FunctionGroupName.TestFunctionGroup, AsyncCallName.TestFunction8, (object)8, null, null);
            WorkItem WorkItem9 = new WorkItem(FunctionGroupName.TestFunctionGroup, AsyncCallName.TestFunction9, (object)9, null, null);


            using (EasyThreadPool threadpool = new EasyThreadPool(8))
            {
                threadpool.AddThreadWork(WorkItem0, System.Windows.Threading.WorkPriority.Highest);
                threadpool.AddThreadWork(WorkItem1, System.Windows.Threading.WorkPriority.AboveNormal);
                threadpool.AddThreadWork(WorkItem2, System.Windows.Threading.WorkPriority.Normal);
                threadpool.AddThreadWork(WorkItem3, System.Windows.Threading.WorkPriority.BelowNormal);
                threadpool.AddThreadWork(WorkItem4, System.Windows.Threading.WorkPriority.Lowest);

                WorkItem0.AsyncWaitHandle.WaitOne();
                Console.WriteLine((string)WorkItem0.OutputParameters);
                WorkItem1.AsyncWaitHandle.WaitOne();
                Console.WriteLine((string)WorkItem1.OutputParameters);
                WorkItem2.AsyncWaitHandle.WaitOne();
                Console.WriteLine((string)WorkItem2.OutputParameters);
                WorkItem3.AsyncWaitHandle.WaitOne();
                Console.WriteLine((string)WorkItem3.OutputParameters);
                WorkItem4.AsyncWaitHandle.WaitOne();
                Console.WriteLine((string)WorkItem4.OutputParameters);

                threadpool.AddThreadWork(new WorkItem(FunctionGroupName.FeaturesFunctionGroup, AsyncCallName.Delay, (object)10000, null, null), System.Windows.Threading.WorkPriority.Highest);
                threadpool.AddThreadWork(new WorkItem(FunctionGroupName.FeaturesFunctionGroup, AsyncCallName.Delay, (object)10500, null, null), System.Windows.Threading.WorkPriority.Highest);
                threadpool.AddThreadWork(new WorkItem(FunctionGroupName.FeaturesFunctionGroup, AsyncCallName.Delay, (object)11000, null, null), System.Windows.Threading.WorkPriority.Highest);
                threadpool.AddThreadWork(new WorkItem(FunctionGroupName.FeaturesFunctionGroup, AsyncCallName.Delay, (object)11500, null, null), System.Windows.Threading.WorkPriority.Highest);
                threadpool.AddThreadWork(new WorkItem(FunctionGroupName.FeaturesFunctionGroup, AsyncCallName.Delay, (object)12000, null, null), System.Windows.Threading.WorkPriority.Highest);
                threadpool.AddThreadWork(new WorkItem(FunctionGroupName.FeaturesFunctionGroup, AsyncCallName.Delay, (object)12500, null, null), System.Windows.Threading.WorkPriority.Highest);
                threadpool.AddThreadWork(new WorkItem(FunctionGroupName.FeaturesFunctionGroup, AsyncCallName.Delay, (object)13000, null, null), System.Windows.Threading.WorkPriority.Highest);
                threadpool.AddThreadWork(new WorkItem(FunctionGroupName.FeaturesFunctionGroup, AsyncCallName.Delay, (object)13500, null, null), System.Windows.Threading.WorkPriority.Highest);

                threadpool.AddThreadWork(WorkItem5, System.Windows.Threading.WorkPriority.Lowest);
                threadpool.AddThreadWork(WorkItem6, System.Windows.Threading.WorkPriority.BelowNormal);
                threadpool.AddThreadWork(WorkItem7, System.Windows.Threading.WorkPriority.Normal);
                threadpool.AddThreadWork(WorkItem8, System.Windows.Threading.WorkPriority.AboveNormal);
                threadpool.AddThreadWork(WorkItem9, System.Windows.Threading.WorkPriority.Highest);

                threadpool.AddThreadWork(new WorkItem(FunctionGroupName.FeaturesFunctionGroup, AsyncCallName.Abort, null, null, null), System.Windows.Threading.WorkPriority.Lowest);




                Console.ReadLine();
            }

        }
    }
}

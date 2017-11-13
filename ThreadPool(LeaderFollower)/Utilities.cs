using System.Collections.Generic;
using System.Diagnostics;

//Microsoft .net framework PriorityQueue code
namespace System.Windows.Threading
{
    public enum WorkPriority
    {
        Invalid = -1,
        Inactive = 0,
        Lowest,
        BelowNormal,
        Normal,
        AboveNormal,
        Highest
    }

    public class PriorityQueue<T>
    {
        public PriorityQueue()
        {
            // Build the collection of priority chains.
            _priorityChains = new SortedList<int, PriorityChain<T>>(); // NOTE: should be Priority
            _cacheReusableChains = new Stack<PriorityChain<T>>(5);

            _head = _tail = null;
            _count = 0;
        }

        // NOTE: not used
        // public int Count {get{return _count;}}

        public WorkPriority MaxPriority // NOTE: should be Priority
        {
            get
            {
                int count = _priorityChains.Count;

                if (count > 0)
                {
                    return (WorkPriority)_priorityChains.Keys[count - 1];
                }
                else
                {
                    return WorkPriority.Invalid; // NOTE: should be Priority.Invalid;
                }
            }
        }

        public PriorityItem<T> Enqueue(WorkPriority priority, T data) // NOTE: should be Priority
        {
            // Find the existing chain for this priority, or create a new one
            // if one does not exist.
            PriorityChain<T> chain = GetChain(priority);

            // Wrap the item in a PriorityItem so we can put it in our
            // linked list.
            PriorityItem<T> priorityItem = new PriorityItem<T>(data);

            // Step 1: Append this to the end of the "sequential" linked list.
            InsertItemInSequentialChain(priorityItem, _tail);

            // Step 2: Append the item into the priority chain.
            InsertItemInPriorityChain(priorityItem, chain, chain.Tail);

            return priorityItem;
        }

        public T Dequeue()
        {
            // Get the max-priority chain.
            int count = _priorityChains.Count;
            if (count > 0)
            {
                PriorityChain<T> chain = _priorityChains.Values[count - 1];
                Debug.Assert(chain != null, "PriorityQueue.Dequeue: a chain should exist.");

                PriorityItem<T> item = chain.Head;
                Debug.Assert(item != null, "PriorityQueue.Dequeue: a priority item should exist.");

                RemoveItem(item);

                return item.Data;
            }
            else
            {
                throw new InvalidOperationException();

            }
        }

        public T Peek()
        {
            T data = default(T);

            // Get the max-priority chain.
            int count = _priorityChains.Count;
            if (count > 0)
            {
                PriorityChain<T> chain = _priorityChains.Values[count - 1];
                Debug.Assert(chain != null, "PriorityQueue.Peek: a chain should exist.");

                PriorityItem<T> item = chain.Head;
                Debug.Assert(item != null, "PriorityQueue.Peek: a priority item should exist.");

                data = item.Data;
            }

            return data;
        }

        public void RemoveItem(PriorityItem<T> item)
        {
            Debug.Assert(item != null, "PriorityQueue.RemoveItem: invalid item.");
            Debug.Assert(item.Chain != null, "PriorityQueue.RemoveItem: a chain should exist.");

            PriorityChain<T> chain = item.Chain;

            // Step 1: Remove the item from its priority chain.
            RemoveItemFromPriorityChain(item);

            // Step 2: Remove the item from the sequential chain.
            RemoveItemFromSequentialChain(item);

            // Note: we do not clean up empty chains on purpose to reduce churn.
        }

        public void ChangeItemPriority(PriorityItem<T> item, WorkPriority priority) // NOTE: should be Priority
        {
            // Remove the item from its current priority and insert it into
            // the new priority chain.  Note that this does not change the
            // sequential ordering.

            // Step 1: Remove the item from the priority chain.
            RemoveItemFromPriorityChain(item);

            // Step 2: Insert the item into the new priority chain.
            // Find the existing chain for this priority, or create a new one
            // if one does not exist.
            PriorityChain<T> chain = GetChain(priority);
            InsertItemInPriorityChain(item, chain);
        }

        private PriorityChain<T> GetChain(WorkPriority priority) // NOTE: should be Priority
        {
            PriorityChain<T> chain = null;

            int count = _priorityChains.Count;
            if (count > 0)
            {
                if (priority == (WorkPriority)_priorityChains.Keys[0])
                {
                    chain = _priorityChains.Values[0];
                }
                else if (priority == (WorkPriority)_priorityChains.Keys[count - 1])
                {
                    chain = _priorityChains.Values[count - 1];
                }
                else if ((priority > (WorkPriority)_priorityChains.Keys[0]) &&
                        (priority < (WorkPriority)_priorityChains.Keys[count - 1]))
                {
                    _priorityChains.TryGetValue((int)priority, out chain);
                }
            }

            if (chain == null)
            {
                if (_cacheReusableChains.Count > 0)
                {
                    chain = _cacheReusableChains.Pop();
                    chain.Priority = priority;
                }
                else
                {
                    chain = new PriorityChain<T>(priority);
                }

                _priorityChains.Add((int)priority, chain);
            }

            return chain;
        }

        private void InsertItemInPriorityChain(PriorityItem<T> item, PriorityChain<T> chain)
        {
            // Scan along the sequential chain, in the previous direction,
            // looking for an item that is already in the new chain.  We will
            // insert ourselves after the item we found.  We can short-circuit
            // this search if the new chain is empty.
            if (chain.Head == null)
            {
                Debug.Assert(chain.Tail == null, "PriorityQueue.InsertItemInPriorityChain: both the head and the tail should be null.");
                InsertItemInPriorityChain(item, chain, null);
            }
            else
            {
                Debug.Assert(chain.Tail != null, "PriorityQueue.InsertItemInPriorityChain: both the head and the tail should not be null.");

                PriorityItem<T> after = null;

                // Search backwards along the sequential chain looking for an
                // item already in this list.
                for (after = item.SequentialPrev; after != null; after = after.SequentialPrev)
                {
                    if (after.Chain == chain)
                    {
                        break;
                    }
                }

                InsertItemInPriorityChain(item, chain, after);
            }
        }

        internal void InsertItemInPriorityChain(PriorityItem<T> item, PriorityChain<T> chain, PriorityItem<T> after)
        {
            Debug.Assert(chain != null, "PriorityQueue.InsertItemInPriorityChain: a chain must be provided.");
            Debug.Assert(item.Chain == null && item.PriorityPrev == null && item.PriorityNext == null, "PriorityQueue.InsertItemInPriorityChain: item must not already be in a priority chain.");

            item.Chain = chain;

            if (after == null)
            {
                // Note: passing null for after means insert at the head.

                if (chain.Head != null)
                {
                    Debug.Assert(chain.Tail != null, "PriorityQueue.InsertItemInPriorityChain: both the head and the tail should not be null.");

                    chain.Head.PriorityPrev = item;
                    item.PriorityNext = chain.Head;
                    chain.Head = item;
                }
                else
                {
                    Debug.Assert(chain.Tail == null, "PriorityQueue.InsertItemInPriorityChain: both the head and the tail should be null.");

                    chain.Head = chain.Tail = item;
                }
            }
            else
            {
                item.PriorityPrev = after;

                if (after.PriorityNext != null)
                {
                    item.PriorityNext = after.PriorityNext;
                    after.PriorityNext.PriorityPrev = item;
                    after.PriorityNext = item;
                }
                else
                {
                    Debug.Assert(item.Chain.Tail == after, "PriorityQueue.InsertItemInPriorityChain: the chain's tail should be the item we are inserting after.");
                    after.PriorityNext = item;
                    chain.Tail = item;
                }
            }

            chain.Count++;
        }

        private void RemoveItemFromPriorityChain(PriorityItem<T> item)
        {
            Debug.Assert(item != null, "PriorityQueue.RemoveItemFromPriorityChain: invalid item.");
            Debug.Assert(item.Chain != null, "PriorityQueue.RemoveItemFromPriorityChain: a chain should exist.");

            // Step 1: Fix up the previous link
            if (item.PriorityPrev != null)
            {
                Debug.Assert(item.Chain.Head != item, "PriorityQueue.RemoveItemFromPriorityChain: the head should not point to this item.");

                item.PriorityPrev.PriorityNext = item.PriorityNext;
            }
            else
            {
                Debug.Assert(item.Chain.Head == item, "PriorityQueue.RemoveItemFromPriorityChain: the head should point to this item.");

                item.Chain.Head = item.PriorityNext;
            }

            // Step 2: Fix up the next link
            if (item.PriorityNext != null)
            {
                Debug.Assert(item.Chain.Tail != item, "PriorityQueue.RemoveItemFromPriorityChain: the tail should not point to this item.");

                item.PriorityNext.PriorityPrev = item.PriorityPrev;
            }
            else
            {
                Debug.Assert(item.Chain.Tail == item, "PriorityQueue.RemoveItemFromPriorityChain: the tail should point to this item.");

                item.Chain.Tail = item.PriorityPrev;
            }

            // Step 3: cleanup
            item.PriorityPrev = item.PriorityNext = null;
            item.Chain.Count--;
            if (item.Chain.Count == 0)
            {
                if (item.Chain.Priority == (WorkPriority)_priorityChains.Keys[_priorityChains.Count - 1])
                {
                    _priorityChains.RemoveAt(_priorityChains.Count - 1);
                }
                else
                {
                    _priorityChains.Remove((int)item.Chain.Priority);
                }

                if (_cacheReusableChains.Count < 10)
                {
                    item.Chain.Priority = WorkPriority.Invalid; // NOTE: should be Priority.Invalid
                    _cacheReusableChains.Push(item.Chain);
                }
            }

            item.Chain = null;
        }

        internal void InsertItemInSequentialChain(PriorityItem<T> item, PriorityItem<T> after)
        {
            Debug.Assert(item.SequentialPrev == null && item.SequentialNext == null, "PriorityQueue.InsertItemInSequentialChain: item must not already be in the sequential chain.");

            if (after == null)
            {
                // Note: passing null for after means insert at the head.

                if (_head != null)
                {
                    Debug.Assert(_tail != null, "PriorityQueue.InsertItemInSequentialChain: both the head and the tail should not be null.");

                    _head.SequentialPrev = item;
                    item.SequentialNext = _head;
                    _head = item;
                }
                else
                {
                    Debug.Assert(_tail == null, "PriorityQueue.InsertItemInSequentialChain: both the head and the tail should be null.");

                    _head = _tail = item;
                }
            }
            else
            {
                item.SequentialPrev = after;

                if (after.SequentialNext != null)
                {
                    item.SequentialNext = after.SequentialNext;
                    after.SequentialNext.SequentialPrev = item;
                    after.SequentialNext = item;
                }
                else
                {
                    Debug.Assert(_tail == after, "PriorityQueue.InsertItemInSequentialChain: the tail should be the item we are inserting after.");
                    after.SequentialNext = item;
                    _tail = item;
                }
            }

            _count++;
        }

        private void RemoveItemFromSequentialChain(PriorityItem<T> item)
        {
            Debug.Assert(item != null, "PriorityQueue.RemoveItemFromSequentialChain: invalid item.");

            // Step 1: Fix up the previous link
            if (item.SequentialPrev != null)
            {
                Debug.Assert(_head != item, "PriorityQueue.RemoveItemFromSequentialChain: the head should not point to this item.");

                item.SequentialPrev.SequentialNext = item.SequentialNext;
            }
            else
            {
                Debug.Assert(_head == item, "PriorityQueue.RemoveItemFromSequentialChain: the head should point to this item.");

                _head = item.SequentialNext;
            }

            // Step 2: Fix up the next link
            if (item.SequentialNext != null)
            {
                Debug.Assert(_tail != item, "PriorityQueue.RemoveItemFromSequentialChain: the tail should not point to this item.");

                item.SequentialNext.SequentialPrev = item.SequentialPrev;
            }
            else
            {
                Debug.Assert(_tail == item, "PriorityQueue.RemoveItemFromSequentialChain: the tail should point to this item.");

                _tail = item.SequentialPrev;
            }

            // Step 3: cleanup
            item.SequentialPrev = item.SequentialNext = null;
            _count--;
        }

        public int Count { get { return _priorityChains.Count; } }

        // Priority chains...
        private SortedList<int, PriorityChain<T>> _priorityChains; // NOTE: should be Priority
        private Stack<PriorityChain<T>> _cacheReusableChains;

        // Sequential chain...
        private PriorityItem<T> _head;
        private PriorityItem<T> _tail;
        private int _count;
    }

    internal class PriorityChain<T>
    {
        public PriorityChain(WorkPriority priority) // NOTE: should be Priority
        {
            _priority = priority;
        }

        public WorkPriority Priority { get { return _priority; } set { _priority = value; } } // NOTE: should be Priority
        public int Count { get { return _count; } set { _count = value; } }
        public PriorityItem<T> Head { get { return _head; } set { _head = value; } }
        public PriorityItem<T> Tail { get { return _tail; } set { _tail = value; } }

        private PriorityItem<T> _head;
        private PriorityItem<T> _tail;
        private WorkPriority _priority;
        private int _count;
    }

    public class PriorityItem<T>
    {
        public PriorityItem(T data)
        {
            _data = data;
        }

        public T Data { get { return _data; } }
        public bool IsQueued { get { return _chain != null; } }

        // Note: not used
        // public WorkPriority Priority { get { return _chain.Priority; } } // NOTE: should be Priority

        internal PriorityItem<T> SequentialPrev { get { return _sequentialPrev; } set { _sequentialPrev = value; } }
        internal PriorityItem<T> SequentialNext { get { return _sequentialNext; } set { _sequentialNext = value; } }

        internal PriorityChain<T> Chain { get { return _chain; } set { _chain = value; } }
        internal PriorityItem<T> PriorityPrev { get { return _priorityPrev; } set { _priorityPrev = value; } }
        internal PriorityItem<T> PriorityNext { get { return _priorityNext; } set { _priorityNext = value; } }

        private T _data;

        private PriorityItem<T> _sequentialPrev;
        private PriorityItem<T> _sequentialNext;

        private PriorityChain<T> _chain;
        private PriorityItem<T> _priorityPrev;
        private PriorityItem<T> _priorityNext;
    }
}

//學長的code+測試Function
namespace ThreadPool.Test
{
    using System.Threading;
    using System;
    using System.Runtime.InteropServices;

    public enum FunctionGroupName
    {
        TestFunctionGroup = 0,
        FeaturesFunctionGroup
    }

    public enum AsyncCallName
    {
        TestFunction0 = 1,
        TestFunction1,
        TestFunction2,
        TestFunction3,
        TestFunction4,
        TestFunction5,
        TestFunction6,
        TestFunction7,
        TestFunction8,
        TestFunction9,
        Delay,
        Abort
    }

    public enum ErrorAndExceptionCode
    {
        NoError = 0,
        ErrorInvalidHandle,
        ErrorNotInitialized,
        ErrorModuleIsNotReady,
        NoException,
        UnknownException,
        InvalidWorkItemException,
        InvalidArgumentException
    };

    public class WorkItem : IAsyncResult, IDisposable
    {
        // private members used for implementation of IAsyncResult interface
        private Object asyncState;
        private ManualResetEvent asyncCompletedEvent;
        private Boolean completedSynchronously;
        private Boolean isCompleted;

        // Private member used for implementation of IDisposable interface
        private bool isDisposed = false;

        // private members used for processing asynchronous call
        private FunctionGroupName groupName;
        private AsyncCallName functionName;
        private Delegate workFunctionDelegate; // For functions not included in
                                               // WorkFunction enumerator
        private AsyncCallback callBackFunction;
        private Object inputParameters;  // Parameters for work function
        private Object outputParameters; // Parameters for call back function
        private ErrorAndExceptionCode exceptionCode;
        private String exceptionMessage;
        private Exception exception;
        private AutoResetEvent moduleAsyncAPICompletedEvent;
        private IntPtr moduleAsyncAPIResult;// Returning results

        // Constructor using WorkFunction enumerator
        public WorkItem(FunctionGroupName GroupName,
                        AsyncCallName AsyncCallName,
                        Object Parameters,
                        AsyncCallback CallBackFunction,
                        Object AsyncStates)
        {
            // Initialize members
            asyncState = AsyncState;
            asyncCompletedEvent = new ManualResetEvent(false);
            completedSynchronously = false;
            isCompleted = false;
            isDisposed = false;
            callBackFunction = CallBackFunction;
            groupName = GroupName;
            functionName = AsyncCallName;
            inputParameters = Parameters;
            outputParameters = null;
            exceptionCode = ErrorAndExceptionCode.NoException;
            exception = null;
            moduleAsyncAPICompletedEvent = new AutoResetEvent(false);
            moduleAsyncAPIResult = IntPtr.Zero;
        }

        public void Complete()
        {
            Debug.Assert(!isCompleted, "iNuCWorkItem re-complete");
            Debug.Assert(!isDisposed, "iNuCWorkItem alread disposed");
            isCompleted = true;
            asyncCompletedEvent.Set();
            if (callBackFunction != null)
            {
                callBackFunction(this);
            }
        }

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(Boolean disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    asyncCompletedEvent.Close();
                    moduleAsyncAPICompletedEvent.Close();
                    asyncCompletedEvent = null;
                    moduleAsyncAPICompletedEvent = null;
                }

                // Dispose unmanaged resources
                if (moduleAsyncAPIResult != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(moduleAsyncAPIResult);
                    moduleAsyncAPIResult = IntPtr.Zero;
                }

                // Note disposing has been done
                isDisposed = true;
            }
        }

        ~WorkItem()
        {
            Dispose(false);
        }

        #region Properties of IAsyncResult

        public Object AsyncState
        {
            get
            {
                return asyncState;
            }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                return (WaitHandle)asyncCompletedEvent;
            }
        }

        public Boolean CompletedSynchronously
        {
            get
            {
                return completedSynchronously;
            }
        }

        public Boolean IsCompleted
        {
            get
            {
                return isCompleted;
            }
        }

        #endregion

        #region Properties

        public FunctionGroupName GroupName
        {
            get
            {
                return groupName;
            }
        }

        public AsyncCallName AsyncCallName
        {
            get
            {
                return functionName;
            }
        }

        public Object InputParameters
        {
            get
            {
                return inputParameters;
            }
        }

        public Object OutputParameters
        {
            get
            {
                return outputParameters;
            }

            set
            {
                outputParameters = value;
            }
        }

        public ErrorAndExceptionCode ExceptionCode
        {
            get
            {
                return exceptionCode;
            }

            set
            {
                exceptionCode = value;
            }
        }

        public String ExceptionMessage
        {
            get
            {
                return exceptionMessage;
            }

            set
            {
                exceptionMessage = value;
            }
        }

        public Exception Exception
        {
            get
            {
                return exception;
            }

            set
            {
                exception = value;
            }
        }

        public AutoResetEvent ModuleAsyncAPICompletedEvent
        {
            get
            {
                return moduleAsyncAPICompletedEvent;
            }
        }

        public IntPtr ModuleAsyncAPIResult
        {
            get
            {
                return moduleAsyncAPIResult;
            }

            set
            {
                moduleAsyncAPIResult = value;
            }
        }

        #endregion
    }

    public static class TestWork
    {
        public static void TestFunctionGroup(WorkItem workItem)
        {
            int Input;
            string Output;
            switch (workItem.AsyncCallName)
            {
                case AsyncCallName.TestFunction0:
                    Input = (int)workItem.InputParameters;
                    Output = "TestFunction0 讀入: " + Input;
                    workItem.OutputParameters = (object)Output;
                    workItem.Complete();
                    break;
                case AsyncCallName.TestFunction1:
                    Input = (int)workItem.InputParameters;
                    Output = "TestFunction1 讀入: " + Input;
                    workItem.OutputParameters = (object)Output;
                    workItem.Complete();
                    break;
                case AsyncCallName.TestFunction2:
                    Input = (int)workItem.InputParameters;
                    Output = "TestFunction2 讀入: " + Input;
                    workItem.OutputParameters = (object)Output;
                    workItem.Complete();
                    break;
                case AsyncCallName.TestFunction3:
                    Input = (int)workItem.InputParameters;
                    Output = "TestFunction3 讀入: " + Input;
                    workItem.OutputParameters = (object)Output;
                    workItem.Complete();
                    break;
                case AsyncCallName.TestFunction4:
                    Input = (int)workItem.InputParameters;
                    Output = "TestFunction4 讀入: " + Input;
                    workItem.OutputParameters = (object)Output;
                    workItem.Complete();
                    break;
                case AsyncCallName.TestFunction5:
                    Input = (int)workItem.InputParameters;
                    Console.WriteLine("TestFunction5 讀入: " + Input);
                    workItem.Complete();
                    break;
                case AsyncCallName.TestFunction6:
                    Input = (int)workItem.InputParameters;
                    Console.WriteLine("TestFunction6 讀入: " + Input);
                    workItem.Complete();
                    break;
                case AsyncCallName.TestFunction7:
                    Input = (int)workItem.InputParameters;
                    Console.WriteLine("TestFunction7 讀入: " + Input);
                    workItem.Complete();
                    break;
                case AsyncCallName.TestFunction8:
                    Input = (int)workItem.InputParameters;
                    Console.WriteLine("TestFunction8 讀入: " + Input);
                    workItem.Complete();
                    break;
                case AsyncCallName.TestFunction9:
                    Input = (int)workItem.InputParameters;
                    Console.WriteLine("TestFunction9 讀入: " + Input);
                    workItem.Complete();
                    break;
                default:
                    workItem.OutputParameters = (object)"找不到Function";
                    workItem.Complete();
                    break;
            }
        }
        public static void FeaturesFunctionGroup(WorkItem workItem)
        {
            int Input;
            switch (workItem.AsyncCallName)
            {
                case AsyncCallName.Delay:
                    Input = (int)workItem.InputParameters;
                    Console.WriteLine("Delay {0}S", Input);
                    SpinWait.SpinUntil(() => false, Input);
                    break;
                case AsyncCallName.Abort:
                    Console.WriteLine("Thread[{0}] Abort", Thread.CurrentThread.ManagedThreadId);
                    Thread.CurrentThread.Abort();
                    break;
                default:
                    workItem.OutputParameters = (object)"找不到Function";
                    workItem.Complete();
                    break;
            }
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace EventsExperiment.EventSystem
{
    public sealed class EventBus
    {
        public event Action<EventData> OnReceivedEvent;
        public event Action Started;
        public event Action<State> Stopped;

        private ConcurrentQueue<EventData> eventsQueue;
        private BlockingCollection<EventData> eventsBlockingQueue;
        private bool continueReading = true;
        private Thread threadProducing;
        private Thread threadConsuming;
        private Thread threadProduceMore;

        private int enqueued;
        private int dequeued;
        private CancellationTokenSource cts;

        public void Start()
        {
            eventsQueue = new ConcurrentQueue<EventData>();
            eventsBlockingQueue = new BlockingCollection<EventData>(eventsQueue);

            cts = new CancellationTokenSource();
            continueReading = true;

            threadProducing = new Thread(new ParameterizedThreadStart(StartProducing))
            {
                IsBackground = true,
                Name = "Producing"
            };

            threadConsuming = new Thread(StartConsuming)
            {
                IsBackground = true,
                Name = "Consuming"
            };

            threadProducing.Start(cts.Token);
            threadConsuming.Start(cts.Token);
            Started?.Invoke();
        }

        public void ProduceMore()
        {
            threadProduceMore = new Thread(new ParameterizedThreadStart(StartProducing))
            {
                IsBackground = true,
                Name = "Producing More"
            };

            threadProduceMore.Start(cts.Token);
        }

        public void Stop()
        {
            continueReading = false;
            cts.Cancel();
            if (threadProducing != null && threadProducing.IsAlive)
            {
                threadProducing.Join(1000);
                threadProducing = null;
            }

            if (threadConsuming != null && threadConsuming.IsAlive)
            {
                threadConsuming.Join(1000);
                threadConsuming = null;
            }

            Stopped?.Invoke(new State { Dequeued = dequeued, Enqueued = enqueued });
        }

        private void StartProducing(object obj)
        {
            CancellationToken cancellationToken = (CancellationToken)obj;
            for (int i = 0; i < 10_000; i++)
            {
                if (!continueReading || cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                EventData eventData = new EventData();
                eventData.Sequence = i;
                eventData.Content = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 };
                eventsBlockingQueue.Add(eventData);
                enqueued += 1;

                Thread.Sleep(2);
            }
        }

        private void StartConsuming(object obj)
        {
            CancellationToken cancellationToken = (CancellationToken)obj;
            int timeout = 1000;
            while (continueReading || !cancellationToken.IsCancellationRequested)
            {
                bool succeed = eventsBlockingQueue.TryTake(out EventData eventData, timeout);
                if (!succeed)
                {
                    // log
                }
                else
                {
                    dequeued += 1;
                    int sum = 0;
                    foreach (byte item in eventData.Content)
                    {
                        sum += item;
                    }

                    eventData.ContentSum = sum;
                    OnReceivedEvent.Invoke(eventData);
                }
            }
        }

    }

    public class State
    {
        public int Enqueued { get; set; }
        public int Dequeued { get; set; }
    }


}

using EventsExperiment.EventSystem;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace EventsExperiment
{
    sealed class MainWindowViewModel : BindableBase
    {
        private readonly EventBus eventBus;

        public DelegateCommand StartCommand { get; set; }
        public DelegateCommand StopCommand { get; set; }

        public DelegateCommand ProduceMoreCommand { get; set; }

        private int enqueued;
        public int Enqueued
        {
            get => enqueued;
            set => SetProperty(ref enqueued, value);
        }

        private int dequeued;
        public int Dequeued
        {
            get => dequeued;
            set => SetProperty(ref dequeued, value);
        }

        private string message;
        public string Message
        {
            get => message;
            set => SetProperty(ref message, value);
        }

        private bool started;
        public bool Started
        {
            get => started;
            set => SetProperty(ref started, value, UpdateCommands);
        }

        private int processed;

        private ObservableCollection<EventData> events;
        public ObservableCollection<EventData> Events { get => events; set => SetProperty(ref events, value); }


        public MainWindowViewModel()
        {
            Events = new ObservableCollection<EventData>();
            eventBus = new EventBus();
            eventBus.Started += EventBus_Started;
            eventBus.Stopped += EventBus_Stopped;
            eventBus.OnReceivedEvent += EventBus_OnReceivedEvent;
            SetupCommands();
        }

        private void EventBus_OnReceivedEvent(EventData data)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Events.Add(data);
                processed += 1;
            });
        }

        private void EventBus_Stopped(State args)
        {
            Enqueued = args.Enqueued;
            Dequeued = args.Dequeued;
            Message = $"Stopped. Enqueued {Enqueued}, Dequeued {Dequeued}, Processed {processed}";
            Started = false;
        }

        private void EventBus_Started()
        {
            Message = "Started";
        }

        private void Start()
        {
            processed = 0;
            Events.Clear();
            eventBus.Start();
            Started = true;
        }

        private bool CanStart()
        {
            return !Started;
        }

        private void Stop()
        {
            eventBus.Stop();
            Started = false;
        }

        private bool CanStop()
        {
            return Started;
        }

        private void SetupCommands()
        {
            StartCommand = new DelegateCommand(Start, CanStart);
            StopCommand = new DelegateCommand(Stop, CanStop);
            ProduceMoreCommand = new DelegateCommand(() => eventBus.ProduceMore());
        }

        private void UpdateCommands()
        {
            StartCommand?.RaiseCanExecuteChanged();
            StopCommand?.RaiseCanExecuteChanged();
        }
    }
}

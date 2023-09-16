namespace EventsExperiment.EventSystem
{
    public class EventData
    {
        public int Sequence { get; set; }
        public byte[] Content { get; set; } = new byte[8];
        public int ContentSum { get; set; }
    }
}

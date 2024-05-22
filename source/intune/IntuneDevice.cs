namespace Maestro
{
    public class IntuneDevice
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public IntuneDevice(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
namespace BlazorDeviceControl.SystemServices
{
    internal interface ISystemService
    {
        void Start();

        bool Running { get; }

        void Stop();
    }
}

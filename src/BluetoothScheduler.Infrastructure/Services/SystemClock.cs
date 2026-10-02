using BluetoothScheduler.Core.Interfaces;

namespace BluetoothScheduler.Infrastructure.Services;

public class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
}

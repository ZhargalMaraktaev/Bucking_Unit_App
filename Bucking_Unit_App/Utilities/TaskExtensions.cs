using System;
using System.Threading.Tasks;

namespace Bucking_Unit_App.Utilities
{
    public static class TaskExtensions
    {
        public static async Task<T> TimeoutAfter<T>(this Task<T> task, int milliseconds)
        {
            if (await Task.WhenAny(task, Task.Delay(milliseconds)) == task)
                return await task;
            throw new TimeoutException();
        }
    }
}
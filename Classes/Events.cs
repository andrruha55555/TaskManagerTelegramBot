using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagerTelegramBot_Pikulev.Classes
{
    public class Events
    {
        public DateTime Time { get; set; }
        public string Message { get; set; }
        public bool IsRecurring { get; set; }
        public List<DayOfWeek> RecurringDays { get; set; }

        public Events(DateTime time, string message, bool isRecurring = false, List<DayOfWeek> days = null)
        {
            Time = time;
            Message = message;
            IsRecurring = isRecurring;
            RecurringDays = days ?? new List<DayOfWeek>();
        }
    }
}

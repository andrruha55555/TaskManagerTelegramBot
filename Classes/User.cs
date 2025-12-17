using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagerTelegramBot_Pikulev.Classes
{
    public class User
    {
        public long IdUser { get; set; }
        public List<Events> Events { get; set; }
        public User(long idUser)
        {
            IdUser = idUser;
            Events = new List<Events>();
        }
    }
}

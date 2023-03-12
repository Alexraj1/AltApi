using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class Users
    {
        [Key]
        public int UserId { get; set; }
        public string UserLogon { get; set; }
        public string EmailAddress { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class CardDetails
    {
        [Key]
        public int CardId { get; set; }
        public string CardNumber { get; set; }
        [ForeignKey("Users")]
        public int UserId { get; set; }
        public virtual Users  Users { get; set; }

    }
}

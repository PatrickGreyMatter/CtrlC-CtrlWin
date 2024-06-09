using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace CtrlWin.Models
{
    public class ImageItem
    {
        [Key]
        public int Id { get; set; }
        public string FilePath { get; set; } = string.Empty; // Initialize to default value
        public DateTime DateSaved { get; set; }
        public long Size { get; set; }
        public string Name { get; set; } = string.Empty; // Initialize to default value
    }
}

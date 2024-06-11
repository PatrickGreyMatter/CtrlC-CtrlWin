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
        public int Id { get; set; }
        public required string FilePath { get; set; }
        public DateTime DateSaved { get; set; }
        public long Size { get; set; }
        public required string Name { get; set; }
    }

}

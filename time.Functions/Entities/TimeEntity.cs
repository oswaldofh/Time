using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace time.Functions.Entities
{
   public  class TimeEntity:TableEntity
    {
        public string IdEmployee { get; set; }
        public DateTime Date { get; set; }
        public int Type { get; set; }
        public bool Consolidate { get; set; }
    }
}

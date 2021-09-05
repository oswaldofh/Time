using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace time.Functions.Entities
{
   public class ConsolidatedEntity: TableEntity
    {
        public string IdEmployee { get; set; }
        public DateTime Date { get; set; }
       public int TimeWorked { get; set; }
    }
}

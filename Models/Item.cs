using System;
using System.Collections.Generic;

#nullable disable

namespace ItemPriceCalculator.Models
{
    public partial class Item
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public double ItemPrice { get; set; }
    }
}

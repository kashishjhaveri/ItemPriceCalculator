using System;
using System.Collections.Generic;

#nullable disable

namespace ItemPriceCalculator.Models
{
    public partial class Customer
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPostal { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerCity { get; set; }
        public string CustomerProvince { get; set; }
        public string CustomerCountry { get; set; }
    }
}

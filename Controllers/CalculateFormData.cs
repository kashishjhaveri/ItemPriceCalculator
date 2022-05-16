using System.Collections.Generic;

namespace ItemPriceCalculator.Controllers
{
    //Form Object which will be populated on calculate button Clicked
    internal class CalculateFormData
    {
        public int CustomerID { get; set; }
        public List<ItemFormData> Items { get; set; }
    }
}
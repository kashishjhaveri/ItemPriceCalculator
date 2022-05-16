using ItemPriceCalculator.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Taxjar;

namespace ItemPriceCalculator.Controllers
{
    public class HomeController : Controller
    {
        private readonly ItemPriceCalculatorContext _context;
        private readonly ILogger<HomeController> _logger;
        public IConfiguration Configuration { get; }
        
        public HomeController(ItemPriceCalculatorContext context, IConfiguration configuration,ILogger<HomeController> logger)
        {
            _logger = logger;
            _context = context;
            Configuration = configuration;
        }

        public IActionResult Index()
        {
            var items = _context.Items.ToList();
            ViewBag.Items = items;
            return View();
        }

        //Post Method called on calculate button click and succsessful validation of data by front-end
        [HttpPost]
        public JsonResult PostCalculate()
        {
            //Initialization of variables
            Dictionary<String, object> response = new Dictionary<string, object>(); // Root Response object which will contain other necessary objects and sent to front-end in Json format
            Dictionary<String, object> data = new Dictionary<string, object>(); // Data object meant to contain data such as customer and item data to be sent to the front end. In later cases can hold different data. Name has been generalized.
            Models.Customer cust = new Models.Customer(); //Customer Object to return along with the calculated values to print for the end user
            List<ItemFormData> items = new List<ItemFormData>(); // List of Items to return with calculated values to print for the end user
            String result = "Fail"; //Flag to represent if the backend execution has been successful
            double sub_total = 0; //Holds the subtotal before adding taxes. Meant to be pushed into the data object.
            double tax_rate = 0; //Holds the tax rate in terms of percentage. Meant to be pushed into the data object.
            double tax_amt = 0;//Holds the tax amount in terms of dollar amount. Meant to be pushed into the data object.
            double total_amt = 0;//Holds total amount to be paid including the tax amount in terms of dollar amount. Meant to be pushed into the data object.
            bool tax_rate_found = false;

            //Decipher the response and convert it into a class object from a Json object
            MemoryStream stream = new MemoryStream();
            Request.Body.CopyTo(stream);
            stream.Position = 0;
            using (StreamReader reader = new StreamReader(stream))
            {
                string requestBody = reader.ReadToEnd();
                if (requestBody.Length > 0)
                {
                    var obj = JsonConvert.DeserializeObject<CalculateFormData>(requestBody);
                    if (obj != null)
                    {
                        try
                        {
                            cust = _context.Customers.Find(obj.CustomerID);
                            var taxClient = new TaxjarApi("f0164a053aa1d3404f41e1fc45bf1201");
                            
                            //Fetch the tax rate from the api
                            try
                            {
                                var rates = taxClient.RatesForLocation(cust.CustomerPostal, new
                                {
                                    street = cust.CustomerAddress,
                                    city = cust.CustomerCity,
                                    state = cust.CustomerProvince,
                                    country = cust.CustomerCountry
                                });
                                tax_rate = (double)rates.CombinedRate;
                                tax_rate_found = true;
                            }catch(Exception ex)
                            {
                                //If the rate for the specific address are not found.
                                result = "Fail";
                                response["message"] = "Tax rate not available for the customer's address"; //Populating message object with a failed message.
                            }
                            //Iterate through each item added by the user for calculation and pushing end result into another list if tax rates found.
                            if (tax_rate_found == true)
                            {
                                foreach (var item in obj.Items)
                                {
                                    //Initiate empty item object to populate and push into the list
                                    ItemFormData single_item = new ItemFormData();
                                    //Find the item
                                    var it = _context.Items.Find(item.ItemID);
                                    //Since it came from a select input we are sure it exists, but just in case DOM manipulation has taken place, we check its existence anyways.
                                    if (it != null)
                                    {
                                        //Calculate the total cost for the item in the requested quantity.
                                        double final_item_cost = item.Quantity * it.ItemPrice;
                                        sub_total += final_item_cost;
                                        //Populate the custom item object with item details and calculated data
                                        single_item.ItemID = it.ItemId;
                                        single_item.ItemName = it.ItemName;
                                        single_item.Quantity = item.Quantity;
                                        single_item.Price = final_item_cost;
                                        items.Add(single_item);
                                    }
                                }
                                tax_amt = sub_total * tax_rate; //calculate the tax-payable
                                total_amt = sub_total + tax_amt; //calculate the total amount to be paid including taxes
                                result = "Success"; // Change the flag to show a successful execution of the post method
                            }
                        }
                        catch(Exception ex)
                        {
                            //Re-initialize the data meant to be pushed into data objects in case of any errors
                            cust = new Models.Customer();
                            items = new List<ItemFormData>();
                            result = "Fail"; // Change the flag to show a failed execution of the post method
                        }
                    }
                }
            }
            //Populate response object before converting it into json object and returning it.
            data["customer"] = cust; //Populating data object with customer data.
            data["items"] = items; //Populating data object with items data.
            data["sub_total"] = sub_total; // Populate the data object with the sub-total amount to be paid before including taxes
            data["tax_rate"] = Math.Round(tax_rate * 100, 2); //Populate data object with the tax rate in terms of percentage.
            data["tax_amt"] = tax_amt; // Populate the data object with the amount of taxes to be paid
            data["total_amt"] = total_amt; // Populate the data object with the total amount to be paid
            response["result"] = result; //Populating response object with result of this execution.
            response["data"] = data; //Populating response object with the data object.
            return new JsonResult(response);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

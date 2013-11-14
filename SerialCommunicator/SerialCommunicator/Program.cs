using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace SerialTest
{
    public class Product
    {
        public string name { get; set; }
        public string barcode { get; set; }
        public float price { get; set; }

    }

    public class PriceDisplay
    {
        public string barcode { get; set; }
        public string id { get; set; }
    }


    class Program
    {
        static Dictionary<string, Product> products = new Dictionary<string, Product>();
        static Dictionary<string, PriceDisplay> priceDisplays = new Dictionary<string, PriceDisplay>();
        static void Main(string[] args)
        {

            Console.WriteLine("Reading Products\n");
            readProducts(products);
            Console.WriteLine("Reading LCDs\n");
            readPriceDisplays(priceDisplays);
            Console.WriteLine("Starting communications");
            communicate();
        }



        static void readProducts(Dictionary<string, Product> products)
        {
            Console.WriteLine("Reading prices in new thread");
            string url = "http://pizza-bakery.azurewebsites.net/serial/Product";
            var request = WebRequest.Create(url);
            request.ContentType = "application/json; charset=utf-8";
            string text;
            var response = (HttpWebResponse)request.GetResponse();

            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                text = sr.ReadToEnd();
            }

            JObject raw = JObject.Parse(text);
            JArray productArray = (JArray)raw["products"];

            foreach (var p in productArray)
            {
                Product product = new Product();
                product.barcode = (string)p["barcode"];

                product.name = (string)p["productName"];

                product.price = (float)p["sellingPrice"];

                if(!products.ContainsKey(product.barcode))
                    products.Add(product.barcode, product);

            }
        }

        static void readPriceDisplays(Dictionary<string, PriceDisplay> priceDisplays)
        {
            
            string url = "http://pizza-bakery.azurewebsites.net/serial/priceDisplays";
            var request = WebRequest.Create(url);
            request.ContentType = "application/json; charset=utf-8";
            string text;
            var response = (HttpWebResponse)request.GetResponse();

            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                text = sr.ReadToEnd();
            }

            JObject raw = JObject.Parse(text);
            JArray priceArray = (JArray)raw["priceDisplayUnits"];

            foreach (var p in priceArray)
            {
                PriceDisplay lcd = new PriceDisplay();
                lcd.barcode = (string)p["barcode"];

                lcd.id = "L"+(string)p["priceDisplayID"];

                if (!priceDisplays.ContainsKey(lcd.id)) 
                    priceDisplays.Add(lcd.id, lcd);

            }
        }

        static SerialPort p;
        static int flag = 0;
        static void communicate()
        {
            string[] names = SerialPort.GetPortNames();
            Console.WriteLine("Serial ports:");
            foreach (string name in names)
                Console.WriteLine(name);
            Console.Write("Choose one:");
            p = new SerialPort(Console.ReadLine());
            p.DataReceived += new SerialDataReceivedEventHandler(p_DataReceived);
            p.Open();
            while (true)
            {
                if (flag == 0)
                {
                    p.Write("[L3002]");
                    flag = 1;
                }
                Thread thread1 = new Thread(() => readProducts(products));
                thread1.Start();
                thread1.Join();
                //sleep for 10 secs. this should be replaced with polling mocked up cash registers and LCDs
                System.Threading.Thread.Sleep(10000);
               // readProducts(products);
                //readPriceDisplays(priceDisplays);
            }
            /*string line;
            do
            {
                line = Console.ReadLine();
                p.Write(line);
            } while (line != "quit");
            */
             
            p.Close();
        }

        static void p_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if ((sender as SerialPort).ReadExisting() == "*1*")
            {
                Console.WriteLine("LCD Found");
                Product pr = products[priceDisplays["L3002"].barcode];
                string outs = "<" + pr.name + ">" + "{" + pr.price + "}";

                p.Write(outs);
                flag = 0;


            }
        }
    }
}
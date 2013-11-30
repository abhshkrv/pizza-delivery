using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Collections.Specialized;

namespace SerialTest
{
    public class Product
    {
        public string name { get; set; }
        public string barcode { get; set; }
        public double price { get; set; }
        public int qty { get; set; }
        public double discount { get; set; }

    }

    public class Transaction
    {
        public List<Product> items { get; set; }
        public List<int> qtyList { get; set; }
        public double totalPrice { get; set; }
        public Transaction()
        {
            items = new List<Product>();
            qtyList = new List<int>();
        }
    }

    public enum Status
    {
        OFFLINE, ONLINE, IN_TRANS
    }

    public class CashRegister
    {
        public string id { get; set; }
        public Transaction transaction { get; set; }
        public Status status { get; set; }

    }

    public class PriceDisplay
    {
        public string barcode { get; set; }
        public string id { get; set; }
        public string lastMsg { get; set; }
    }

    public class MockCashRegister
    {
        public string id { get; set; }

        public string generateTransactionString()
        {
            Random rnd = new Random();
            int itemQty = rnd.Next(1, 10);

            return "";
        }
    }



    class Program
    {
        static Dictionary<string, Product> products = new Dictionary<string, Product>();
        static List<CashRegister> cashRegisters = new List<CashRegister>();

        static Dictionary<string, PriceDisplay> priceDisplays = new Dictionary<string, PriceDisplay>();

        static void Main(string[] args)
        {

            Console.WriteLine("Reading Products\n");
            readProducts(products);
            Console.WriteLine("Reading CashRegisters");
            readCashRegisters(cashRegisters);
            Console.WriteLine("Reading LCDs\n");
            readPriceDisplays(priceDisplays);
            Console.WriteLine("Starting communications");
            communicate();
        }

        private static void readCashRegisters(List<CashRegister> cashRegisters)
        {
            CashRegister cr = new CashRegister();
            cr.id = "C2271";
            cr.status = Status.OFFLINE;


            cashRegisters.Add(cr);
        }



        static void readProducts(Dictionary<string, Product> products)
        {
            Console.WriteLine("Reading prices in new thread");
            string url = "http://localhost:1824/serial/Product";
            var request = WebRequest.Create(url);
            request.ContentType = "application/json; charset=utf-8";
            string text = ""; ;

            try
            {
                var response = (HttpWebResponse)request.GetResponse();


                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    text = sr.ReadToEnd();
                }

            }
            catch
            {
                Console.WriteLine("Network Error");
            }
            JObject raw = JObject.Parse(text);
            JArray productArray = (JArray)raw["products"];

            foreach (var p in productArray)
            {
                Product product = new Product();
                product.barcode = (string)p["barcode"];
                product.name = (string)p["productName"];
                product.price = (double)p["sellingPrice"];
                product.qty = (int)p["currentStock"];
                product.discount = (double)p["discountPercentage"];


                if (!products.ContainsKey(product.barcode))
                    products.Add(product.barcode, product);

                else
                {
                    products[product.barcode] = product;
                }

            }



            Console.WriteLine("Read complete");
        }

        static void readPriceDisplays(Dictionary<string, PriceDisplay> priceDisplays)
        {

            string url = "http://localhost:1824/serial/priceDisplays";
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

                lcd.id = "L" + (string)p["priceDisplayID"];

                if (!priceDisplays.ContainsKey(lcd.id))
                    priceDisplays.Add(lcd.id, lcd);

            }
        }

        static SerialPort p;
        static int cflag = 0;
        static int state = 1;
        static void communicate()
        {
            string[] names = SerialPort.GetPortNames();
            Console.WriteLine("Serial ports:");
            foreach (string name in names)
                Console.WriteLine(name);
            Console.Write("Choose one:");
            p = new SerialPort(Console.ReadLine(), 9600, Parity.None, 8, StopBits.Two);
            p.DataReceived += new SerialDataReceivedEventHandler(p_DataReceived);
            p.Open();
            int update = 0;
            while (true)
            {
                /*if (flag == 0)
                {
                    Console.WriteLine("Sending ID...");
                    p.Write("[L3002]");
                    flag = 1;
                }*/

                if (cflag == 0)
                {
                    if (state == 0)
                    {
                        CashRegister cr = cashRegisters[0];
                        Console.WriteLine("Sending ID...[" + cr.id + "]");
                        p.Write("[" + cr.id + "]");
                        currentCR = cr;
                        cflag = 1;
                    }

                    if (state == 1)
                    {
                        Product pr = products[priceDisplays["L3002"].barcode];
                        string outs;
                        if (pr.discount == 0.0)
                            outs = "Price:" + pr.price + "$  " + "Discount:" + "No Discount currently";
                        else
                            outs = "Price:" + pr.price + "$  " + "Discount:" + pr.discount + "%";

                        PriceDisplay pd = priceDisplays["L3002"];

                        if (pd.lastMsg != outs)
                        {
                            Console.WriteLine("Sending ID...");
                            p.Write("[L3002]");
                            pd.lastMsg = outs;
                            cflag = 1;
                        }
                        else
                        {
                            Console.WriteLine("No price update");
                            state = 0;
                            cflag = 0;
                            buffer = "";
                        }
                    }
                }

                if (update == 10000)
                {
                    Thread thread1 = new Thread(() => readProducts(products));
                    //thread1.Start();
                    // thread1.Join();
                    if (thread1.ThreadState != ThreadState.Running)
                    {
                        thread1.Start();
                    }
                    //Thread.Sleep(5000);
                    thread1.Join();
                    update = 0;
                }
                update++;

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

        static string buffer;
        static CashRegister currentCR;
        static void p_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string line = (sender as SerialPort).ReadExisting();
            buffer += line;
            if (buffer.Length > 24)
                buffer = "";
            Console.WriteLine("Data received = " + line);

            if (state == 0)
            {

                //add item
                if (buffer.Contains("[") && buffer.Contains("]"))
                {

                    Console.WriteLine("Cash Register found Found");
                    string barcode = buffer.Substring(buffer.IndexOf('[') + 1, 8);
                    string qty = buffer.Substring(buffer.IndexOf(';') + 1, buffer.IndexOf(']') - (buffer.IndexOf(';') + 1));

                    Product pr = new Product();

                    try
                    {
                        pr = products[barcode];
                    }
                    catch
                    {
                        pr = null;
                    }
                    if (pr == null)
                    {
                        string outs = "(D;0)";
                        p.Write(outs);
                    }
                    else if (pr.qty < Int16.Parse(qty))
                    {
                        string outs = "(E;" + qty.ToString() + ")";
                        p.Write(outs);
                    }
                    else if (currentCR.transaction.totalPrice > 999999.99)
                    {
                        string outs = "(D;0)";
                        p.Write(outs);
                    }
                    else
                    {
                        double cost = pr.price * Int16.Parse(qty);
                        if (currentCR.transaction == null)
                        {
                            currentCR.transaction = new Transaction();
                        }
                        currentCR.transaction.items.Add(pr);
                        currentCR.transaction.qtyList.Add(Int16.Parse(qty));


                        string outs = "(" + (int)Math.Floor(Math.Log10(cost) + 1) + ";" + cost.ToString("0.00").TrimStart('0') + ")";

                        p.Write(outs);
                        Console.WriteLine("Sent data : " + outs);
                    }
                    cflag = 0;
                    state = 1;
                    buffer = "";
                }

                if (buffer == "*4*")
                {
                    string tstring = "2271:";
                    for (int i = 0; i < cashRegisters[0].transaction.items.Count; i++)
                    {
                        Product p = cashRegisters[0].transaction.items[i];
                        int qty = cashRegisters[0].transaction.qtyList[i];
                        tstring += p.barcode;
                        tstring += "#" + qty;
                        if (i != cashRegisters[0].transaction.items.Count - 1)
                            tstring += ";";
                    }

                    using (var wb = new WebClient())
                    {
                        string url = "http://localhost:1824/transaction/addTransaction";
                        var data = new NameValueCollection();
                        data["transactionString"] = tstring;
                        //data["password"] = "myPassword";

                        var response = wb.UploadValues(url, "POST", data);
                    }


                    cashRegisters[0].status = Status.ONLINE;
                    cashRegisters[0].transaction = null;

                    buffer = "";
                    Console.WriteLine("Transaction Complete");
                    cflag = 0;
                    state = 1;

                }

                // authenctication
                else if (buffer.Contains("(") && buffer.Contains(")"))
                {
                    Console.WriteLine("Cash Register found Found");
                    string username = buffer.Substring(buffer.IndexOf('(') + 1, 6);
                    string password = buffer.Substring(buffer.IndexOf(';') + 1, buffer.IndexOf(')') - (buffer.IndexOf(';') + 1));

                    if (username == "123456" && decrypt(password) == "987654")
                    {
                        Console.WriteLine("Authentication Success");
                        buffer = "";
                        cashRegisters[0].status = Status.ONLINE;
                        p.Write("(A;0)");
                    }
                    else
                    {
                        Console.WriteLine("Authentication Failed");
                        p.Write("(B;0)");
                    }

                    buffer = "";
                    Console.WriteLine("Authentication check complete");
                    cflag = 0;
                    state = 1;
                }

                else if (buffer == "*0*")
                {
                    Console.WriteLine("CR: Nothing to send");
                    buffer = "";
                    cflag = 0;
                    state = 1;
                }

                 //cancel item
                else if (buffer.Contains("<") && buffer.Contains(">"))
                {

                    int index = Int16.Parse(buffer.Substring(buffer.IndexOf('(') + 1, 1));
                    Product pr = cashRegisters[0].transaction.items[index - 1];


                    int qty = cashRegisters[0].transaction.qtyList[index - 1];
                    cashRegisters[0].transaction.items.RemoveAt(index - 1);
                    cashRegisters[0].transaction.qtyList.RemoveAt(index - 1);

                    double cost = pr.price * qty;

                    string outs = "(" + (int)Math.Floor(Math.Log10(cost) + 1) + ";" + cost.ToString("0.00").TrimStart('0') + ")";
                    cflag = 0;
                    state = 1;
                }

                //cancel transaction
                else if (buffer.Contains("*3*"))
                {
                    currentCR.transaction = null;
                    cflag = 0;
                    state = 1;
                }

                //logout
                else if (buffer.Contains("*2*"))
                {
                    currentCR.status = Status.OFFLINE;
                    currentCR.transaction = null;
                    Console.WriteLine("Logged Out");
                    cflag = 0;
                    state = 1;
                }

                //email
                else if (buffer.Contains("$") && buffer.Contains(">"))
                {
                    if (buffer.Contains("2468"))
                    {
                        String outs = "(D;0)";
                        p.Write(outs);
                    }
                    else
                    {
                        Console.WriteLine("Sending Email and SMS ...");
                        String outs = "(E;0)";
                        p.Write(outs);
                    }
                    cflag = 0;
                    state = 1;
                }

                 // modify qty
                else if (buffer.Contains("#") && buffer.Contains("]"))
                {
                    //#12345678;qty]
                    string barcode = buffer.Substring(buffer.IndexOf('#') + 1, 8);
                    string qty = buffer.Substring(buffer.IndexOf(';') + 1, buffer.IndexOf(']') - (buffer.IndexOf(';') + 1));
                    int newQty = Int16.Parse(qty);
                    Product pr;
                    pr = products[barcode];
                    try
                    {
                        pr = products[barcode];
                    }
                    catch
                    {
                        pr = null;
                    }
                    if (pr == null)
                    {
                        string outs = "(D;0)";
                        p.Write(outs);
                    }
                    else if (pr.qty < Int16.Parse(qty))
                    {
                        string outs = "(E;" + qty.ToString() + ")";
                        p.Write(outs);
                    }
                    else
                    {
                        int index = currentCR.transaction.items.IndexOf(pr);
                        //currentCR.transaction.items.RemoveAt(pr);
                        int oldQty = currentCR.transaction.qtyList[index];
                        double oldCost = oldQty * pr.price;

                        currentCR.transaction.qtyList[index] = newQty;

                        double newCost = newQty * pr.price;

                        string newOuts = "(" + (int)Math.Floor(Math.Log10(newCost) + 1) + ";" + newCost.ToString("0.00").TrimStart('0') + ")";
                        string outs = "(" + (int)Math.Floor(Math.Log10(oldCost) + 1) + ";" + oldCost.ToString("0.00").TrimStart('0') + ")" + newOuts;
                        p.Write(outs);
                    }
                    cflag = 0;
                    state = 1;
                }
            }

            else
            {

                if (buffer == "*1*")
                {
                    buffer = "";
                    Console.WriteLine("LCD Found");
                    // Product pr = products[priceDisplays["L3002"].barcode];
                    // string outs = "<" + pr.name + ">" + "{" + pr.price + "}";

                    PriceDisplay pd = priceDisplays["L3002"];

                    p.Write(pd.lastMsg);
                    Console.WriteLine("Sent data : " + pd.lastMsg);
                    cflag = 0;
                    state = 0;
                }
            }
        }

        private static string decrypt(string password)
        {
            //15

            string value = "";
            for (int i = 0; i < 6; i++)
            {
                value += (char)(password[i] - 15 - i);
            }

            return value;
        }
    }
}
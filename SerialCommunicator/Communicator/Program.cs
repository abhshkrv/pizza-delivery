﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Collections.Specialized;
using System.Diagnostics;

namespace SerialTest
{
    public class Product
    {
        public string name { get; set; }
        public string barcode { get; set; }
        public double price { get; set; }
        public int qty { get; set; }
        public double discount { get; set; }


        public int bundleUnit { get; set; }
    }

    public class Employee
    {
        public string userID { get; set; }
        public string password { get; set; }
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

        public int status { get; set; }
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
        public Employee employee { get; set; }
        public Int32 lastTransactionID { get; set; }
    }

    public class PriceDisplay
    {
        public string barcode { get; set; }
        public string id { get; set; }
        public string lastMsg { get; set; }
        public string lastBarcode {get;set;}
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
        static Dictionary<string, Employee> employees = new Dictionary<string, Employee>();
        static List<CashRegister> cashRegisters = new List<CashRegister>();

        static Dictionary<string, PriceDisplay> priceDisplays = new Dictionary<string, PriceDisplay>();

        static void Main(string[] args)
        {

            Console.WriteLine("Reading Products");
            readProducts(products);
            Console.WriteLine("\nReading Cash Registers");
            readCashRegisters(cashRegisters);
            Console.WriteLine("Reading LCDs");
            readPriceDisplays(priceDisplays);
            Console.WriteLine("Reading Employee Details");
            readEmployees(employees);
            Console.WriteLine("Starting Communications...\n");
            communicate();
        }

        private static void readCashRegisters(List<CashRegister> cashRegisters)
        {
            string url = "http://localhost:1824/serial/CashRegisters";
            var request = WebRequest.Create(url);
            request.ContentType = "application/json; charset=utf-8";
            string text = "";

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
            JArray cashRegisterArray = (JArray)raw["CashRegisters"];

            foreach (var c_reg in cashRegisterArray)
            {
                CashRegister cr = new CashRegister();
                cr.id = (string)c_reg["cashRegisterID"];
                cr.status = Status.OFFLINE;
                int flag = 0;
                foreach (var item in cashRegisters) 
                {
                    if (item.id == cr.id)
                        flag = 1;
                }
                if(flag==0)
                    cashRegisters.Add(cr);

            }

            for (int i = 0; i < cashRegisters.Count; i++)
            {
                if (cashRegisters[i].id == "2271")
                {
                    var temp = cashRegisters[0];
                    cashRegisters[0] = cashRegisters[i];
                    cashRegisters[i] = temp;
                }
            }

            Console.WriteLine("Read complete");           
        }

        private static void login(string username, string password, string cashRegister)
        {
            using (var wb = new WebClient())
            {
                string url = "http://localhost:1824/session/login";
                var data = new NameValueCollection();
                data["username"] = username;
                data["password"] = password;
                data["cashRegister"] = cashRegister;

                var response = wb.UploadValues(url, "POST", data);
            }

            Console.WriteLine("Logged in");
        }

        private static void logout(string username, string password, string cashRegister)
        {
            using (var wb = new WebClient())
            {
                string url = "http://localhost:1824/session/logout";
                var data = new NameValueCollection();
                data["username"] = username;
                data["password"] = password;
                data["cashRegister"] = cashRegister;

                var response = wb.UploadValues(url, "POST", data);
            }

            Console.WriteLine("Logged out");

        }

        static void readProducts(Dictionary<string, Product> products)
        {
            //Thread.Sleep(10000);
            Console.WriteLine("Reading prices in new thread\n");
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
                product.bundleUnit = (int)p["bundleUnit"];

                if (!products.ContainsKey(product.barcode))
                    products.Add(product.barcode, product);

                else
                {
                    products[product.barcode] = product;
                }

            }



            Console.WriteLine("Read complete");

            readPriceDisplays(priceDisplays);
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
                else
                {

                    priceDisplays[lcd.id].barcode = lcd.barcode;
                    //priceDisplays[lcd.id] = lcd.barcode;

                }

            }
        }

        static void readEmployees(Dictionary<string, Employee> employees)
        {
            string url = "http://localhost:1824/session/employees";
            var request = WebRequest.Create(url);
            request.ContentType = "application/json; charset=utf-8";
            string text;
            var response = (HttpWebResponse)request.GetResponse();

            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                text = sr.ReadToEnd();
            }

            JObject raw = JObject.Parse(text);
            JArray employeeArray = (JArray)raw["Employees"];

            foreach (var p in employeeArray)
            {
                Employee employee = new Employee();
                employee.userID = (string)p["userID"];

                employee.password = (string)p["password"];

                if (!employees.ContainsKey(employee.userID))
                    employees.Add(employee.userID, employee);
                else
                {
                    employees[employee.userID] = employee;
                }

            }
        }

        static SerialPort p;
        static int cflag = 0;
        static int state = 1;
        static void communicate()
        {
            string[] names = SerialPort.GetPortNames();
            Console.WriteLine("Serial ports:-");
            foreach (string name in names)
                Console.WriteLine(name);
            Console.Write("\nChoose one: ");
            p = new SerialPort(Console.ReadLine(), 9600, Parity.None, 8, StopBits.Two);
            p.ReadTimeout = 1000;
            p.WriteTimeout = 1000;
            p.DataReceived += new SerialDataReceivedEventHandler(p_DataReceived);
            p.Open();
            int update = 0;
            Thread thread1 = new Thread(() => readProducts(products));

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            int count = 0;
            while (true)
            {
                /*if (flag == 0)
                {
                    Console.WriteLine("Sending ID...");
                    p.Write("[L3002]");
                    flag = 1;
                }*/
                Thread.Sleep(400);
                if (cflag == 1)
                {
                    //Timeout
                    
                    
                    //Thread.Sleep(5000);
                    if (stopWatch.ElapsedMilliseconds >= 5000)
                    {
                        //priceDisplays["L3002"].lastMsg = "";
                        stopWatch.Stop();
                        if (state == 0)
                        {
                            state = 1;
                           
                        }
                        else if (state == 1)
                        {
                            state = 0;
                            priceDisplays["L3002"].lastMsg = "";
                        }
                        cflag = 0;
                        stopWatch.Start();
                    }
                }

                if (cflag == 0)
                {
                    if (state == 0)
                    {

                            for (int i = 1; i < cashRegisters.Count; i++)
                            {
                                Console.WriteLine("\nSending ID [C" + cashRegisters[i].id + "]");
                                Thread.Sleep(20);
                            }
                            CashRegister cr = cashRegisters[0];
                            Console.WriteLine("\nSending ID [C" + cr.id + "]");
                            p.Write("[C" + cr.id + "]");
                            currentCR = cr;
                            cflag = 1;                                           
                    }

                    if (state == 1)
                    {
                        Product pr = products[priceDisplays["L3002"].barcode];
                        string outs;
                        if (pr.discount == 0.0)
                            outs = "<" + pr.name + ">" + "{" + "Price:" + pr.price + "$  " + "No Discount Currently}";
                        else
                            outs = "<" + pr.name + ">" + "{" + "Price:" + pr.price + "$  " + "Discount:" + pr.discount + "%}";

                        PriceDisplay pd = priceDisplays["L3002"];

                        if (pd.lastMsg != outs || pd.lastBarcode != pr.barcode)
                        {
                            Console.WriteLine("\nSending ID [L3002]");
                            p.Write("[L3002]");
                            pd.lastMsg = outs;
                            priceDisplays["L3002"].lastBarcode = pr.barcode;

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

                if (update == 1)
                {

                    //thread1.Start();
                    // thread1.Join();
                    if (!thread1.ThreadState.Equals(System.Threading.ThreadState.Running))
                    {
                        thread1 = new Thread(() => readProducts(products));
                        thread1.Start();
                    }
                    //Thread.Sleep(5000);
                    //thread1.Join();
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

         //   p.Close();
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

                    Console.WriteLine("Cash Register Found");
                    string barcode = buffer.Substring(buffer.IndexOf('[') + 1, 8);
                    string qty = buffer.Substring(buffer.IndexOf(';') + 1, buffer.IndexOf(']') - (buffer.IndexOf(';') + 1));

                    if (currentCR.transaction == null)
                    {
                        currentCR.transaction = new Transaction();
                    }

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
                    else if (Int32.Parse(qty) <= 0)
                    {
                        string outs = "(D;0)";
                        p.Write(outs);
                    }
                    else if (pr.qty < Int32.Parse(qty))
                    {
                        string outs = "(E;" + pr.qty.ToString() + ")";
                        p.Write(outs);
                    }
                    else
                    {
                        double cost = pr.price * Int32.Parse(qty);
                        cost = cost * (1 - (pr.discount / 100.0));
                        if (pr.bundleUnit <= Int32.Parse(qty) && pr.bundleUnit > 0)
                            cost = 0.9 * cost;

                        if (currentCR.transaction.totalPrice + cost > 999999.99 || Int32.Parse(qty) <= 0)
                        {
                            string outs = "(D;0)";
                            p.Write(outs);
                        }
                        else
                        {
                            currentCR.transaction.totalPrice += cost;
                            if (currentCR.transaction == null)
                            {
                                currentCR.transaction = new Transaction();
                            }
                            int check = 0, i = 0, skipCheck = 0;
                            foreach (var product in currentCR.transaction.items)
                            {
                                if (product.barcode == barcode)
                                {
                                    if ((currentCR.transaction.qtyList[i] + Int32.Parse(qty)) > pr.qty)
                                    {
                                        skipCheck = 1;
                                        currentCR.transaction.totalPrice -= cost;
                                        string temp_outs = "(E;" + pr.qty.ToString() + ")";
                                        p.Write(temp_outs);
                                        break;
                                    }
                                    else
                                    {
                                        currentCR.transaction.qtyList[i] += Int32.Parse(qty);
                                        check = 1;
                                        break;
                                    }
                                }
                                i++;
                            }
                            if (skipCheck == 0)
                            {
                                if (check == 0)
                                {
                                    currentCR.transaction.items.Add(pr);
                                    currentCR.transaction.qtyList.Add(Int32.Parse(qty));
                                    currentCR.transaction.status = 1;
                                }

                                string outs = "(" + (int)Math.Floor(Math.Log10(cost) + 1) + ";" + cost.ToString("0.00").TrimStart('0') + ")";
                                p.Write(outs);
                                Console.WriteLine("Sent data : " + outs);
                            }
                        }
                    }
                    cflag = 0;
                    state = 1;
                    buffer = "";
                }

                //save transaction
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

                        currentCR.lastTransactionID = Int32.Parse(Encoding.ASCII.GetString(response));
                    }

                    cashRegisters[0].transaction.status = 2;
                    cashRegisters[0].status = Status.ONLINE;
                    cashRegisters[0].transaction = null;
                    currentCR = cashRegisters[0];
                    buffer = "";
                    Console.WriteLine("Transaction Complete");
                    cflag = 0;
                    state = 1;

                }

                // authenctication
                else if (buffer.Contains("(") && buffer.Contains(")"))
                {
                    Console.WriteLine("Cash Register Found");
                    string username = buffer.Substring(buffer.IndexOf('(') + 1, 6);
                    string password = buffer.Substring(buffer.IndexOf(';') + 1, buffer.IndexOf(')') - (buffer.IndexOf(';') + 1));

                    if (!employees.ContainsKey(username))
                    {
                        Console.WriteLine("Authentication Failed");
                        p.Write("(B;0)");
                    }
                    else if (employees[username].password == decrypt(password))
                    {
                        Console.WriteLine("Authentication Success");
                        buffer = "";
                        cashRegisters[0].status = Status.ONLINE;
                        p.Write("(A;0)");
                        currentCR.employee = new Employee();
                        currentCR.employee.userID = username;
                        currentCR.employee.password = password;
                        Thread thread3 = new Thread(() => login(username, decrypt(password), cashRegisters[0].id));
                        thread3.Start();
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
                    string barcode = buffer.Substring(1, 8);
                    int index = -1;// = Int32.Parse(buffer.Substring(buffer.IndexOf('(') + 1, 1));

                    Product pr = null;
                    foreach (var product in currentCR.transaction.items)
                    {
                        if (product.barcode == barcode)
                        {
                            pr = product;
                            index = currentCR.transaction.items.IndexOf(product);
                        }

                    }

                    if (index == -1)
                    {
                        Console.WriteLine("Product not in transactions");
                        p.Write("(D;0)");
                        state = 1;
                        cflag = 0;
                    }
                    else
                    {
                        int qty = cashRegisters[0].transaction.qtyList[index];
                        cashRegisters[0].transaction.items.RemoveAt(index);
                        cashRegisters[0].transaction.qtyList.RemoveAt(index);

                        double cost = pr.price * qty;
                        cost = cost * (1 - (pr.discount / 100.0));
                        if (pr.bundleUnit <= qty && pr.bundleUnit > 0)
                            cost = 0.9 * cost;
                        currentCR.transaction.totalPrice -= cost;

                        string outs = "(" + (int)Math.Floor(Math.Log10(cost) + 1) + ";" + cost.ToString("0.00").TrimStart('0') + ")";
                        p.Write(outs);
                    }
                    cflag = 0;
                    state = 1;
                }

                //cancel transaction
                else if (buffer.Contains("*3*") && currentCR.transaction != null)
                {
                    if (currentCR.transaction.status == 1 || currentCR.transaction.status == 0)
                    {
                        currentCR.transaction = null;
                        cflag = 0;
                        state = 1;
                    }
                }

                //cancel last transaction
                else if (buffer.Contains("*3*"))
                {
                    if (currentCR.transaction==null)
                    {
                        string url = "http://localhost:1824/transaction/deleteTransaction?transactionID=" + currentCR.lastTransactionID.ToString();
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
                            Console.WriteLine("Error");
                        }
                        currentCR.lastTransactionID = 0;
                        currentCR.transaction = null;
                        cflag = 0;
                        state = 1;
                    }
                }

                //logout
                else if (buffer.Contains("*2*"))
                {
                    currentCR.status = Status.OFFLINE;
                    currentCR.transaction = null;

                    if (currentCR != null&&cashRegisters[0].id!=null)
                    {
                        var tmp_username = " ";

                        try
                        {
                            tmp_username = currentCR.employee.userID;
                        }
                        catch
                        {
                            tmp_username = null;
                        }

                        if (tmp_username != null)
                        {
                            Thread thread2 = new Thread(() => logout(currentCR.employee.userID, currentCR.employee.password, cashRegisters[0].id));
                            thread2.Start();
                        }
                    }
                    cflag = 0;
                    state = 1;
                }

                //refund transaction
                else if (buffer.Contains("$") && buffer.Contains(">"))
                {
                    string id = buffer.Substring(1, 8);
                    string url =  "http://localhost:1824/transaction/deleteTransaction?transactionID=" +  id;
                    var request = WebRequest.Create(url);
                    request.ContentType = "application/json; charset=utf-8";
                    string text = "";

                    try
                    {
                        var response = (HttpWebResponse)request.GetResponse();
                        using (var sr = new
                        StreamReader(response.GetResponseStream()))
                        {
                            text = sr.ReadToEnd();
                        }
                    }
                    catch
                    {
                        p.Write("(D;0)");
                        Console.WriteLine("Error in Refunding Transaction");
                    }

                    if (text == "Fail")
                    {
                        p.Write("(D;0)");
                        Console.WriteLine("Given Transaction ID does not exist");
                    }
                    else if (text == "Success")
                        p.Write("(D;1)");

                    currentCR.lastTransactionID = 0;
                    currentCR.transaction = null;
                    cflag = 0;
                    state = 1;
                }

                 // modify qty
                else if (buffer.Contains("#") && buffer.Contains("]"))
                {
                    //#12345678;qty]
                    string barcode = buffer.Substring(buffer.IndexOf('#') + 1, 8);
                    int index = -1;// = Int32.Parse(buffer.Substring(buffer.IndexOf('(') + 1, 1));

                    Product pr = null;
                    foreach (var product in currentCR.transaction.items)
                    {
                        if (product.barcode == barcode)
                        {
                            pr = product;
                            index = currentCR.transaction.items.IndexOf(product);
                        }

                    }

                    if (index == -1)
                    {
                        Console.WriteLine("Product not in transactions");
                        p.Write("(D;0)");
                        cflag = 0;
                        state = 1;
                    }
                    else
                    {
                        string qty = buffer.Substring(buffer.IndexOf(';') + 1, buffer.IndexOf(']') - (buffer.IndexOf(';') + 1));
                        int newQty = Int32.Parse(qty);
                        //Product pr;
                        // pr = products[barcode];

                        if (pr.qty < Int32.Parse(qty))
                        {
                            string outs = "(E;" + pr.qty.ToString() + ")";
                            p.Write(outs);
                        }
                        else if (Int32.Parse(qty) <= 0)
                        {
                            string outs = "(D;0)";
                            p.Write(outs);
                        }
                        else
                        {
                            int oldQty = currentCR.transaction.qtyList[index];
                            double oldCost = oldQty * pr.price;

                            index = currentCR.transaction.items.IndexOf(pr);
                            //currentCR.transaction.items.RemoveAt(pr);

                            currentCR.transaction.qtyList[index] = newQty;

                            double newCost = newQty * pr.price;
                            newCost = newCost * (1 - (pr.discount / 100.0));
                            if (pr.bundleUnit <= newQty && pr.bundleUnit > 0)
                                newCost = 0.9 * newCost;

                            oldCost = oldCost * (1 - (pr.discount / 100.0));
                            if (pr.bundleUnit <= oldQty && pr.bundleUnit > 0)
                                oldCost = 0.9 * oldCost;

                            if ((newCost + currentCR.transaction.totalPrice - oldCost) > 999999.99)
                            {
                                string outs = "(D;0)";
                                p.Write(outs);
                            }
                            else
                            {
                                currentCR.transaction.totalPrice -= oldCost;
                                currentCR.transaction.totalPrice += newCost;
                                string newOuts = "(" + (int)Math.Floor(Math.Log10(newCost) + 1) + ";" + newCost.ToString("0.00").TrimStart('0') + ")";
                                string outs = "(" + (int)Math.Floor(Math.Log10(oldCost) + 1) + ";" + oldCost.ToString("0.00").TrimStart('0') + ")" + newOuts;
                                p.Write(outs);
                            }
                        }
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
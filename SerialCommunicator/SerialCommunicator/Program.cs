using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;

namespace SerialTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] names = SerialPort.GetPortNames();
            Console.WriteLine("Serial ports:");
            foreach (string name in names)
                Console.WriteLine(name);
            Console.Write("Choose one:");
            SerialPort p = new SerialPort(Console.ReadLine());
            p.DataReceived += new SerialDataReceivedEventHandler(p_DataReceived);
            p.Open();
            string line;
            do
            {
                line = Console.ReadLine();
                p.Write(line);
            } while (line != "quit");
            p.Close();
        }

        static void p_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Console.WriteLine(
                (sender as SerialPort).ReadExisting());
        }
    }
}
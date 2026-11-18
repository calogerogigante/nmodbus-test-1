using System;
using System.IO.Ports;
using System.Threading;
using NModbus;
using NModbus.Serial;

namespace ModbusClient
{
    internal class Program
    {
        private const string PortName = "COM12";
        private const int BaudRate = 9600;
        private const byte SlaveId = 1;

        static void Main()
        {
            var factory = new ModbusFactory();

            using (var port = new SerialPort(PortName, BaudRate, Parity.None, 8, StopBits.One))
            {
                port.WriteTimeout = 500;
                port.Open();

                var adapter = new SerialPortAdapter(port);

                var master = factory.CreateRtuMaster(adapter);
                master.Transport.ReadTimeout = 1000;
                master.Transport.Retries = 0;

                Console.WriteLine("Client Modbus RTU sur " + PortName + ". Appuie sur une touche pour quitter.");
                Console.WriteLine("----------------------=====-------------------------------------");

                while (!Console.KeyAvailable)
                {
                    try
                    {
                        ushort[] regs = master.ReadHoldingRegisters(SlaveId, 0, 4);
                        Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "  " + string.Join(", ", regs));
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine("Timeout : pas de réponse du serveur.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Erreur : " + ex.Message);
                    }

                    Thread.Sleep(1000);
                }

                Console.ReadKey(true);
            }
        }
    }
}

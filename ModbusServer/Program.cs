using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using NModbus;
using NModbus.Serial;

namespace ModbusServer
{
    internal class Program
    {
        private const string PortName = "COM14";
        private const int BaudRate = 9600;
        private const byte SlaveId = 1;

        static void Main()
        {
            Console.WriteLine("Démarrage du serveur sur " + PortName + "...");
            Console.WriteLine("-------------------------=====---");

            var factory = new ModbusFactory();

            using (var port = new SerialPort(PortName, BaudRate, Parity.None, 8, StopBits.One))
            {
                // Timeouts courts : permettent à NModbus d'abandonner une trame
                // incomplète et de se resynchroniser sur la requête suivante.
                port.ReadTimeout = 100;
                port.WriteTimeout = 500;

                port.Open();
                Console.WriteLine("Port " + PortName + " ouvert.");

                var adapter = new SerialPortAdapter(port);
                var network = factory.CreateRtuSlaveNetwork(adapter);
                var slave = factory.CreateSlave(SlaveId);
                network.AddSlave(slave);

                slave.DataStore.HoldingRegisters.WritePoints(0, new ushort[] { 10, 20, 30, 40 });

                var cts = new CancellationTokenSource();
                var listenTask = Task.Run(() => network.ListenAsync(cts.Token));

                Console.WriteLine("Serveur Modbus RTU en écoute sur " + PortName + " (esclave " + SlaveId + ").");
                Console.WriteLine("Appuie sur une touche pour quitter.");

                // Le registre 0 est incrémenté chaque seconde pour voir la valeur évoluer côté client.
                ushort counter = 0;
                while (!Console.KeyAvailable)
                {
                    counter++;
                    slave.DataStore.HoldingRegisters.WritePoints(0, new ushort[] { counter });
                    Thread.Sleep(1000);
                }

                Console.ReadKey(true);

                try
                {
                    cts.Cancel();
                    listenTask.Wait(1000);
                }
                catch (AggregateException)
                {
                    // Arrêt volontaire : on ignore l'exception d'annulation.
                }

                network.Dispose();
            }
        }
    }
}

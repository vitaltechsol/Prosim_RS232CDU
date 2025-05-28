using ProSimSDK;
using System;
using System.IO.Ports;
using System.Windows.Forms;

namespace Prosim_RS232CDU
{

    public partial class Form1 : Form
    {
        static SerialPort serialPort;

        ProSimConnect prosimConnect = new ProSimConnect();
        string prosimIP = "192.168.1.142";
        string comPort = "COM3";
        int baudRate = 9600;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            prosimConnect.onConnect += connection_onConnect;
            prosimConnect.onDisconnect += connection_onDisconnect;
            ConnectToProSim(prosimIP);
            Invoke(new MethodInvoker(StartCOMPort));

            DataRef dataRef1 = new DataRef("system.indicators.I_CDU1_CALL", 100, prosimConnect);
            DataRef dataRef2 = new DataRef("system.indicators.I_CDU1_EXEC", 100, prosimConnect);
            DataRef dataRef3 = new DataRef("system.indicators.I_CDU1_FAIL", 100, prosimConnect);

            dataRef1.onDataChange += AnnunciatorHandler;
            dataRef2.onDataChange += AnnunciatorHandler;
            dataRef3.onDataChange += AnnunciatorHandler;

        }

        void connection_onConnect()
        {
            Invoke(new MethodInvoker(StartConnection));
        }

        void connection_onDisconnect()
        {
            Invoke(new MethodInvoker(EndConnection));
        }

        void StartCOMPort()
        {
            serialPort = new SerialPort(comPort, baudRate, Parity.None, 8, StopBits.One);
            serialPort.DataReceived += SerialPort_DataReceived;
            serialPort.Open();
            lblPort.Text = "Listening on " + comPort;
            Console.WriteLine("Listening on COM8...");
        }

        //
        void ConnectToProSim(string prosimIP)
        {

            try
            {
                prosimConnect.Connect(prosimIP);
            }
            catch (Exception ex)
            {
            }
        }

        void StartConnection()
        {
            lblConnection.Text = "Connected";

    
        }

        void EndConnection()
        {
            lblConnection.Text = "Not Connected";
        }

        void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (serialPort.BytesToRead >= 2)
                {
                    byte[] buffer = new byte[2];
                    int bytesRead = serialPort.Read(buffer, 0, 2);

                    if (bytesRead == 2)
                    {
                        string hex = $"{buffer[0]:X2}{buffer[1]:X2}";
                        Console.WriteLine($"Received HEX: {hex}");

                        // There should a value for on and a value for off I think?
                        // Also need to convert to switch instead of if statements

                        //switch (hex.ToUpperInvariant())
                        //{
                        //    case "F070":
                        //        // Your logic here
                        //        break;

                        if (hex.Equals("F070", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("CDU MENU button press detected. Sending to ProSim...");
                            //lblKeyPressed.Invoke(new MethodInvoker(delegate
                            //{
                            //    lblKeyPressed.Text = "MENU";
                            //}));

                            DataRef dataRef = new DataRef("system.switches.S_CDU2_KEY_MENU", 100, prosimConnect, true);
                            // ON
                            dataRef.value = 1;

                            // Trigger CDU MENU button press
                            System.Threading.Thread.Sleep(100);

                            // OFF
                            dataRef.value = 0;
                            Console.WriteLine("CDU MENU button press sent to ProSim.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SerialPort DataReceived Error: {ex.Message}");
            }
        }

        // Handler for ProSim dataref change

        private void AnnunciatorHandler(DataRef dataRef)
        {
            var value = Convert.ToInt32(dataRef.value);

            Console.WriteLine($"ProSim MSG Annunciator DataRef {dataRef.name} changed to: {value}");

            switch(dataRef.name)
            {
                case "system.indicators.I_CDU1_CALL":
                    if (value == 1)
                    {
                        Console.WriteLine("CALL annunciator ON - sending D3 01 to COM PORT...");
                        SendHexMessage(new byte[] { 0xD3, 0x01 }); // <-- Add correct value
                        lblL1.Invoke(new MethodInvoker(delegate
                        {
                            lblL1.Text = "ON";
                        }));
                    }

                    if (value == 0)
                    {
                        Console.WriteLine("CALL annunciator OFF - sending D3 01 to COM PORT...");
                        SendHexMessage(new byte[] { 0xD3, 0x01 }); // <-- Add correct value
                        lblL1.Invoke(new MethodInvoker(delegate
                        {
                            lblL1.Text = "OFF";
                        }));
                    }
                    break;
                case "system.indicators.I_CDU1_EXEC":
                    if (value == 1)
                    {
                        Console.WriteLine("EXEC annunciator ON - sending D3 01 to COM PORT...");
                        SendHexMessage(new byte[] { 0xD3, 0x01 }); // <-- Add correct value
                        lblL2.Invoke(new MethodInvoker(delegate
                        {
                            lblL1.Text = "ON";
                        }));
                    }

                    if (value == 0)
                    {
                        Console.WriteLine("EXEC annunciator OFF - sending D3 01 to COM PORT...");
                        SendHexMessage(new byte[] { 0xD3, 0x01 }); // <-- Add correct value
                        lblL2.Invoke(new MethodInvoker(delegate
                        {
                            lblL2.Text = "OFF";
                        }));
                    }
                    break;
            }

        }


        // Write D3 01 to COM8
        private static void SendHexMessage(byte[] data)
        {
            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    serialPort.Write(data, 0, data.Length);
                    Console.WriteLine("D3 01 sent to COM PORT.");
                }
                else
                {
                    Console.WriteLine("Serial port is not open.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to PORT: {ex.Message}");
            }
        }

       
    }
}

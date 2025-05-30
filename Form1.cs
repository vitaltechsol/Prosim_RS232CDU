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

        // Control flags 
        const ushort CTRL_MSG = 0x0001;
        const ushort CTRL_EXEC = 0x0002;
        const ushort CTRL_OFST = 0x0004;
        const ushort CTRL_CALL = 0x0010;
        const ushort CTRL_FAIL = 0x0020;

        static ushort message = 0xD300;


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

            DataRef dataRef1 = new DataRef("system.indicators.I_CDU1_MSG", 100, prosimConnect);
            DataRef dataRef2 = new DataRef("system.indicators.I_CDU1_EXEC", 100, prosimConnect);
            DataRef dataRef3 = new DataRef("system.indicators.I_CDU1_FAIL", 100, prosimConnect);
            DataRef dataRef4 = new DataRef("system.indicators.I_CDU1_OFFSET", 100, prosimConnect);
            DataRef dataRef5 = new DataRef("system.indicators.I_CDU1_CALL", 100, prosimConnect);

            dataRef1.onDataChange += AnnunciatorHandler;
            dataRef2.onDataChange += AnnunciatorHandler;
            dataRef3.onDataChange += AnnunciatorHandler;
            dataRef4.onDataChange += AnnunciatorHandler;
            dataRef5.onDataChange += AnnunciatorHandler;
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
            Console.WriteLine("Listening on" + comPort);
        }

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

                            DataRef dataRef = new DataRef("system.switches.S_CDU1_KEY_MENU", 100, prosimConnect, true);

                            dataRef.value = 1; // ON

                            System.Threading.Thread.Sleep(100); // Trigger utton press

                            dataRef.value = 0; // OFF
                            Console.WriteLine("CDU MENU button press sent to ProSim.");
                        }

                        if (hex.Equals("F029", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("CDU MENU button press detected. Sending to ProSim...");

                            DataRef dataRef = new DataRef("system.switches.S_CDU1_KEY_LSK1L", 100, prosimConnect, true);

                            dataRef.value = 1; // ON

                            System.Threading.Thread.Sleep(100); // Trigger button press

                            dataRef.value = 0; // OFF
                            Console.WriteLine("CDU MENU button press sent to ProSim.");
                        }

                        if (hex.Equals("F028", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("CDU MENU button press detected. Sending to ProSim...");

                            DataRef dataRef = new DataRef("system.switches.S_CDU1_KEY_LSK2L", 100, prosimConnect, true);

                            dataRef.value = 1; // ON

                            System.Threading.Thread.Sleep(100); // Trigger button press

                            dataRef.value = 0; // OFF
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


        // Annunciator handler using bitwise TurnOn/TurnOff
        private void AnnunciatorHandler(DataRef dataRef)
        {
            var value = Convert.ToInt32(dataRef.value);
            Console.WriteLine($"DataRef {dataRef.name} changed to: {value}");

            ushort flag = 0;
            Label targetLabel = lblL1;

            switch (dataRef.name)
            {
                case "system.indicators.I_CDU1_MSG":
                    flag = CTRL_MSG;
                    targetLabel = lblL1;
                    break;
                case "system.indicators.I_CDU1_EXEC":
                    flag = CTRL_EXEC;
                    targetLabel = lblL2;
                    break;
                case "system.indicators.I_CDU1_OFFSET":
                    flag = CTRL_OFST;
                    targetLabel = lblL3;
                    break;
                case "system.indicators.I_CDU1_CALL":
                    flag = CTRL_CALL;
                    targetLabel = lblL4;
                    break;
                case "system.indicators.I_CDU1_FAIL":
                    flag = CTRL_FAIL;
                    targetLabel = lblL5;
                    break;
            }

            if (flag != 0)
            {
                if (value == 2) // ON
                {
                    TurnOn(flag);
                    targetLabel.Invoke(new MethodInvoker(() => targetLabel.Text = "ON"));
                }
                else if (value == 0) // OFF
                {
                    TurnOff(flag);
                    targetLabel.Invoke(new MethodInvoker(() => targetLabel.Text = "OFF"));
                }
            }
        }


        /*		// Handler for ProSim dataref change

				private void AnnunciatorHandler(DataRef dataRef)
				{
					var value = Convert.ToInt32(dataRef.value);
					// 2 is on
					// 0 is off

					Console.WriteLine($"ProSim MSG Annunciator DataRef {dataRef.name} changed to: {value}");

					switch (dataRef.name)
					{

						case "system.indicators.I_CDU1_MSG":
							if (value == 2)
							{
								Console.WriteLine("MSG annunciator ON - sending D3 01 to COM PORT...");
								SendHexMessage(new byte[] { 0xD3, 0x01 }); // <-- Add correct value
								lblL1.Invoke(new MethodInvoker(delegate
								{
									lblL1.Text = "ON";
								}));
							}

							if (value == 0)
							{
								Console.WriteLine("MSG annunciator OFF - sending D3 01 to COM PORT...");
								SendHexMessage(new byte[] { 0xD3, 0x00 }); // <-- Add correct value
								lblL1.Invoke(new MethodInvoker(delegate
								{
									lblL1.Text = "OFF";
								}));
							}
							break;

						case "system.indicators.I_CDU1_EXEC":
							if (value == 2)
							{
								Console.WriteLine("EXEC annunciator ON - sending D3 02 to COM PORT...");
								SendHexMessage(new byte[] { 0xD3, 0x02 }); // <-- Add correct value
								lblL2.Invoke(new MethodInvoker(delegate
								{
									lblL2.Text = "ON";
								}));
							}

							if (value == 0)
							{
								Console.WriteLine("EXEC annunciator OFF - sending D3 02 to COM PORT...");
								SendHexMessage(new byte[] { 0xD3, 0x00 }); // <-- Add correct value
								lblL2.Invoke(new MethodInvoker(delegate
								{
									lblL2.Text = "OFF";
								}));
							}
							break;

						case "system.indicators.I_CDU1_OFFSET":
							if (value == 2)
							{
								Console.WriteLine("OFFSET annunciator ON - sending D3 04 to COM PORT...");
								SendHexMessage(new byte[] { 0xD3, 0x04 }); // <-- Add correct value
								lblL2.Invoke(new MethodInvoker(delegate
								{
									lblL3.Text = "ON";
								}));
							}

							if (value == 0)
							{
								Console.WriteLine("OFFSET annunciator OFF - sending D3 04 to COM PORT...");
								SendHexMessage(new byte[] { 0xD3, 0x00 }); // <-- Add correct value
								lblL2.Invoke(new MethodInvoker(delegate
								{
									lblL3.Text = "OFF";
								}));
							}
							break;

						case "system.indicators.I_CDU1_CALL":
							if (value == 2)
							{
								Console.WriteLine("CALL annunciator ON - sending D3 10 to COM PORT...");
								SendHexMessage(new byte[] { 0xD3, 0x10 }); // <-- Add correct value
								lblL2.Invoke(new MethodInvoker(delegate
								{
									lblL4.Text = "ON";
								}));
							}

							if (value == 0)
							{
								Console.WriteLine("CALL annunciator OFF - sending D3 10 to COM PORT...");
								SendHexMessage(new byte[] { 0xD3, 0x00 }); // <-- Add correct value
								lblL2.Invoke(new MethodInvoker(delegate
								{
									lblL4.Text = "OFF";
								}));
							}
							break;

						case "system.indicators.I_CDU1_FAIL":
							if (value == 2)
							{
								Console.WriteLine("FAIL annunciator ON - sending D3 20 to COM PORT...");
								SendHexMessage(new byte[] { 0xD3, 0x20 }); // <-- Add correct value
								lblL2.Invoke(new MethodInvoker(delegate
								{
									lblL4.Text = "ON";
								}));
							}

							if (value == 0)
							{
								Console.WriteLine("FAIL annunciator OFF - sending D3 20 to COM PORT...");
								SendHexMessage(new byte[] { 0xD3, 0x00 }); // <-- Add correct value
								lblL2.Invoke(new MethodInvoker(delegate
								{
									lblL4.Text = "OFF";
								}));
							}
							break;
					}

				}
		*/

        // Write to COM8
        private static void Send()
        {
            byte[] bytes = new byte[]
            {
                (byte)((message >> 8) & 0xFF),
                (byte)(message & 0xFF)
            };

            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    serialPort.Write(bytes, 0, 2);
                    Console.WriteLine($"Sent: 0x{message:X4} as {bytes[0]:X2}-{bytes[1]:X2}");
                }
                else
                {
                    Console.WriteLine("Serial port not open.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to port: {ex.Message}");
            }
        }

        // Turn ON flag (set bit)
        private static void TurnOn(ushort flag)
        {
            message |= flag;
            Send();
        }

        // Turn OFF flag (clear bit)
        private static void TurnOff(ushort flag)
        {
            message &= (ushort)~flag;
            Send();
        }

    }
}

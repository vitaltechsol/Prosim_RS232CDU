using ProSimSDK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Prosim_RS232CDU
{

    public partial class Form1 : Form
    {
        static SerialPort serialPort;
        private Dictionary<string, EntryData> dataMap = new Dictionary<string, EntryData>();

        ProSimConnect prosimConnect = new ProSimConnect();
        string prosimIP = "127.0.0.1";  //will be overwritten when config.xml loads
        string comPort = "COM1";        //will be overwritten when config.xml loads
		int baudRate = 9600;            //will be overwritten when config.xml loads
        string cduId = "CP";            //will be overwritten when config.xml loads

		// Control flags for Annunciators
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
			LoadConfig();
            LoadXml("CDU_Buttons.xml");
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

		// Load IP, COM, ID from config.xml
		private void LoadConfig()
		{
			try
			{
				XDocument doc = XDocument.Load("config.xml");
				foreach (XElement entry in doc.Descendants("Configuration"))
				{
					string ip = entry.Element("IP")?.Value.Trim();
					string port = entry.Element("RS232")?.Value.Trim();
					string baudStr = entry.Element("BAUD")?.Value.Trim();
					string cduid = entry.Element("CDUID")?.Value.Trim();

					if (!string.IsNullOrEmpty(ip))
					{
						prosimIP = ip;
						Console.WriteLine($"Loaded ProSim IP: {prosimIP}");
                        label9.Text = ip;
					}

					if (!string.IsNullOrEmpty(port))
					{
						comPort = port;
						Console.WriteLine($"Loaded COM Port: {comPort}");
					}

					if (!string.IsNullOrEmpty(cduid))
					{
						cduId = cduid;
						Console.WriteLine($"CDU ID: {cduId}");
                        this.Text = cduId + " - CDU Interface";
					}

					if (!string.IsNullOrEmpty(baudStr) && int.TryParse(baudStr, out int baud))
					{
						baudRate = baud;
						Console.WriteLine($"Loaded COM Port: {baudRate}");
						label6.Text = baudStr;
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error loading config.xml: " + ex.Message);
			}
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
                        // This has a space between the buffers to match the XML hex value
                        string hexToFind = $"{buffer[0]:X2} {buffer[1]:X2}".ToUpper();
                        Console.WriteLine($"Received HEX: {hexToFind}");

                        if (dataMap.TryGetValue(hexToFind, out EntryData entry))
                        {
                            Console.WriteLine($" FOUND HEX: Desc: {entry.Desc}\nDataref: {entry.Dataref}\nValue: {entry.Value}");
                            DataRef dataRef = new DataRef($"system.switches.S_CDU1_KEY_{entry.Dataref}", 100, prosimConnect, true);
                            dataRef.value = entry.Value;
                            lblKeyPressed.Text = entry.Desc;
                        }
                        else
                        {
                            Console.WriteLine($"Hex '{hexToFind}' not found.");
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

        private void LoadXml(string filePath)  // CDU Buttons data
        {
            try
            {
                XDocument doc = XDocument.Load(filePath);
                foreach (var entry in doc.Descendants("Entry"))
                {
                    string hex = entry.Element("Hex")?.Value.Trim();
                    string desc = entry.Element("Desc")?.Value.Trim();
                    string dataref = entry.Element("Dataref")?.Value.Trim();
                    Int32 value = Convert.ToInt32(entry.Element("Value")?.Value.Trim());

                    if (!string.IsNullOrEmpty(hex))
                    {
                        dataMap[hex] = new EntryData
                        {
                            Desc = desc,
                            Dataref = dataref,
                            Value = value
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading XML: " + ex.Message);
            }
        }


      
        // Write to COM to turn on Annunciators
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
    public class EntryData
    {
        public string Desc { get; set; }
        public string Dataref { get; set; }
        public int Value { get; set; }
    }
}

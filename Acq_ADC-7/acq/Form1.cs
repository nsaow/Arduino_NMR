using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;

namespace acq
{
    public partial class FormACQ : Form
    {
        public struct TemBuffer
        {
            public int readBytes;               // for data readout from Serial buffer
            public int byteToRead;              // for data readout from Serial buffer
            public int totalByte;               // total bytes;
            public byte n_seg;                  // number od data segments
            public byte i_seg;                  // current reading segment
            public byte[] header;               // |datatype|n_seg|i_seg|Length(2 bytes)|
            public int serialDataType;          // 0:text; 1:data
            public byte[] Buf;                  // reading data from Serial port
        }
        public struct IQsignal
        {
            public ushort[] I, Q;
        }
        public struct ProcData
        {
            public double x, y;
        }
        public struct NMRData
        {
            public byte dataType;               // 0 = FIDs ; 1 = echoes;
            public int ncols, nrows;
            public int icol, irow;
            public int elements, count;
            public int displayPeriod;
            public int dataLength;              // data length in points
            public int dataBytes;               // data length in bytes
            public Boolean realtimeProc;        // 
            public IQsignal[,] signal;
            public ProcData[] procData;
        }
        public struct TecnicalParams
        {
            public double Tmin;                 // the shortest RF pulse = 2.0 us
            public double Tdeadtime;            // free time before data acquisition
            public double T_transfer_factor;    // us/IQ
        }
        public struct AD9959Params
        {
            public double f0, f1, f2, f3;       // frequencies of the 4 channels    (not used so far)
            public int amp0, amp1, amp2, amp3;  // amplitudes of the 4 channels
            public double ph0, ph1, ph2, ph3;   // phases of the 4 channels
        }
        public struct PulseParams
        {
            public double refFreq;              // reference frequency (MHz)
            public AD9959Params DDS;            // DDS
            public double p90;                  // us
            public double p180;                 // us
        }
        public struct AcqParams
        {
            public int runMode;                 // 0 = interactive ; -1 = data collection
            public int nAvg;
            public double Ttr;                  // us
            public double TreadoutF;            // us
            public double TreadoutE;            // us
            public int FID_samples;             // points
            public int Echo_samples;            // points
            public int nFIDs;                   // !!used only in PC program (not need for Arduino)
            public int nEchoes;                 // !!used only in PC program (not need for Arduino)
            public double I_offset;
            public double Q_offset;
        }
        public struct DoubleParam
        {
            public double value;
            public String name;
            public String cmd;
            public String unit;
        }
        public struct IntParam
        {
            public int value;
            public String name;
            public String cmd;
            public String unit;
        }
        public struct CalibParams
        {
            public DoubleParam Tpw0;            // starting pulse width
            public DoubleParam Tdpw;            // stepping pulse width
            public IntParam npw;                // number of stepping
            public IntParam IorQ;               // select I or Q (I = 0 Q = 1)
        }
        public struct IRParams
        {
            public DoubleParam IRtau;          // ms
            public IntParam nIR;                // number of stepping
        }
        public struct CPMGParams
        {
            public DoubleParam Tte;            // stepping pulse width
            public IntParam ne;                 // number of stepping
        }

        const int memDepth = 128 * 1024;        // temporary buffer size
        TemBuffer tbuf;
        NMRData Data;                           // for data storage
        TecnicalParams techParam;
        PulseParams pulseParam;
        AcqParams acqParam;
        CalibParams seq_calib;
        IRParams seq_IR;
        CPMGParams seq_CPMG;
        Boolean seq_Running = false;
        int rawSampling = 1;
        public string activeSeq = "none";        // {"none", "tuning", "calib", "cpmg"."ir"}
        List<string> FIDCollectionCom = new List<string>();   // list of sequence that need FIDs data storage
        List<string> echoCollectionCom = new List<string>();   // list of sequence that need echoes data storage

        public FormACQ()
        {
            InitializeComponent();
        }
        private void refreshParams()
        {
            textBoxP90.Text = pulseParam.p90.ToString();
            textBoxP180.Text = pulseParam.p180.ToString();
            textBoxFreq.Text = pulseParam.refFreq.ToString();
            textBoxF0.Text = pulseParam.DDS.f0.ToString();
            textBoxF1.Text = pulseParam.DDS.f1.ToString();
            textBoxF2.Text = pulseParam.DDS.f2.ToString();
            textBoxF3.Text = pulseParam.DDS.f3.ToString();
            textBoxDDSAmp0.Text = pulseParam.DDS.amp0.ToString();
            textBoxDDSAmp1.Text = pulseParam.DDS.amp1.ToString();
            textBoxDDSAmp2.Text = pulseParam.DDS.amp2.ToString();
            textBoxDDSAmp3.Text = pulseParam.DDS.amp3.ToString();
            textBoxDDSPh0.Text = pulseParam.DDS.ph0.ToString();
            textBoxDDSPh1.Text = pulseParam.DDS.ph1.ToString();
            textBoxDDSPh2.Text = pulseParam.DDS.ph2.ToString();
            textBoxDDSPh3.Text = pulseParam.DDS.ph3.ToString();
            textBoxAcqNavg.Text = acqParam.nAvg.ToString();
            textBoxAcqTR.Text = acqParam.Ttr.ToString();
            textBoxAcqFIDR.Text = acqParam.TreadoutF.ToString();
            textBoxAcqEchoR.Text = acqParam.TreadoutE.ToString();
            textBoxAcqFIDSam.Text = acqParam.FID_samples.ToString();
            textBoxAcqEchoSam.Text = acqParam.Echo_samples.ToString();
            textBoxAcqIOffset.Text = acqParam.I_offset.ToString();
            textBoxAcqQOffset.Text = acqParam.Q_offset.ToString();
        }
        private void uploadParams()
        {
            if (!MCUPort.IsOpen) return;
            try
            {
                MCUPort.Write("p90:" + pulseParam.p90.ToString());
                MCUPort.Write("p180:" + pulseParam.p180.ToString());
                MCUPort.Write("freq:" + pulseParam.refFreq.ToString());
                MCUPort.Write("f0:" + pulseParam.DDS.f0.ToString());
                MCUPort.Write("f1:" + pulseParam.DDS.f1.ToString());
                MCUPort.Write("f2:" + pulseParam.DDS.f2.ToString());
                MCUPort.Write("f3:" + pulseParam.DDS.f3.ToString());
                MCUPort.Write("amp0:" + pulseParam.DDS.amp0.ToString());
                MCUPort.Write("amp1:" + pulseParam.DDS.amp1.ToString());
                MCUPort.Write("amp2:" + pulseParam.DDS.amp2.ToString());
                MCUPort.Write("amp3:" + pulseParam.DDS.amp3.ToString());
                MCUPort.Write("ph0:" + pulseParam.DDS.ph0.ToString());
                MCUPort.Write("ph1:" + pulseParam.DDS.ph1.ToString());
                MCUPort.Write("ph2:" + pulseParam.DDS.ph2.ToString());
                MCUPort.Write("ph3:" + pulseParam.DDS.ph3.ToString());
                MCUPort.Write("mode:" + acqParam.runMode.ToString());
                MCUPort.Write("navg:" + acqParam.nAvg.ToString());
                MCUPort.Write("tr:" + acqParam.Ttr.ToString());
                MCUPort.Write("trdf:" + acqParam.TreadoutF.ToString());
                MCUPort.Write("trde:" + acqParam.TreadoutE.ToString());
                MCUPort.Write("fsam:" + acqParam.FID_samples.ToString());
                MCUPort.Write("esam:" + acqParam.Echo_samples.ToString());
            }
            catch (Exception exc)
            {
                textBoxMCUmsg.AppendText("Error: " + exc.Message);
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
            string[] ports = SerialPort.GetPortNames();
            toolStripComboBoxMCUport.Items.AddRange(ports);
            connectToolStripMenuItem.Enabled = true;
            disconnectToolStripMenuItem.Enabled = false;
            // list of commands 
            FIDCollectionCom.Add("tuning");
            FIDCollectionCom.Add("calib");
            FIDCollectionCom.Add("ir");
            echoCollectionCom.Add("cpmg");
            // initialize parameters
            techParam.Tmin = 2.0;                   // us
            techParam.Tdeadtime = 2;                // us
            techParam.T_transfer_factor = 2.0;      // us/IQ
            // pulse and dds paramters
            pulseParam.p90 = techParam.Tmin;        // us
            pulseParam.p180 = 2 * techParam.Tmin;   // us
            pulseParam.refFreq = 1.0;               // MHz
            pulseParam.DDS = new AD9959Params { f0 = 1, f1 = 1, f2 = 1, f3 = 1, amp0 = 500, amp1 = 500, amp2 = 500, amp3 = 500, ph0 = 0, ph1 = 90, ph2 = 0, ph3 = 90 };
            // data acquisition parameters
            acqParam.runMode = 0;                   // 0 = interactive ; -1 = data collection
            acqParam.nAvg = 1;
            acqParam.Ttr = 100;                     // ms
            acqParam.TreadoutF = 90;                // ms
            acqParam.TreadoutE = 0.5;               // ms
            acqParam.FID_samples = 1000;            // 1000 points
            acqParam.Echo_samples = 10;             // 10 points
            acqParam.nFIDs = 1;
            acqParam.nEchoes = 10;
            acqParam.I_offset = 0;
            acqParam.Q_offset = 0;
            // sequence parameters  = new DoubleParam { value = , name = , cmd = , unit =  };
            seq_calib.Tpw0 = new DoubleParam {  value = techParam.Tmin, name = "Starting width",    cmd = "pw0:",   unit = "us" };
            seq_calib.Tdpw = new DoubleParam {  value = 0.2,            name = "Stepping width",    cmd = "dpw:",   unit = "us" };
            seq_calib.npw = new IntParam {      value = 20,             name = "No. steps",         cmd = "npw:",   unit = "steps" };
            seq_calib.IorQ = new IntParam {     value = 0,              name = "I = 0 or Q = 1",    cmd = "iq:",    unit = " " };
            seq_IR.IRtau = new DoubleParam {    value = 10,             name = "Tau",               cmd = "irtau:", unit = "ms" };
            seq_IR.nIR = new IntParam {         value = 20,             name = "No. points",        cmd = "nir:",   unit = "points" };
            seq_CPMG.Tte = new DoubleParam {    value = 2,              name = "Echo time",         cmd = "te:",    unit = "ms" };
            seq_CPMG.ne = new IntParam {        value = 10,             name = "No. echoes",        cmd = "ne:",    unit = "echoes" };
            // initialize temporaly buffer
            tbuf.readBytes = 0;
            tbuf.byteToRead = 0;
            tbuf.header = new byte[3];
            tbuf.serialDataType = 0;
            tbuf.Buf = new byte[memDepth];
            // initialize chart
            chartRaw.Series["I"].Points.Clear();
            chartRaw.Series["Q"].Points.Clear();
            int i;
            for (i = 0; i < 500; i++)
            {
                chartRaw.Series["I"].Points.AddXY((double)i, 0 - acqParam.I_offset);
                chartRaw.Series["Q"].Points.AddXY((double)i, 0 - acqParam.Q_offset);
            }
            chartRaw.Update();
            // add parameters to Textboxes
            refreshParams();
            // ...
            toolStripComboBoxMode.SelectedIndex = 0;
            toolStripComboBoxSeq.SelectedIndex = 0;
            toolStripButtonRunStatus.Image = imageList1.Images[0];
            toolStripButtonRun.Image = imageList1.Images[2];
            toolStripButtonStop.Image = imageList1.Images[3];
            toolStripButtonProc.Image = imageList1.Images[5];
            toolStripButtonComp.Image = imageList1.Images[6];
            //buttonClearMCUMsg.Image = imageList1.Images[4];
            textBoxMCUmsg.Clear();
        }
        private void toolStripButtonExit_Click(object sender, EventArgs e)
        {
            if (seq_Running)
            {
                textBoxMCUmsg.AppendText("Please stop the running Sequence before close Application!! \r\n");
                return;
            }
            if (MCUPort.IsOpen)
                MCUPort.Close();
            Application.Exit();
        }
        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!MCUPort.IsOpen)
            {
                MCUPort.PortName = toolStripComboBoxMCUport.Text;
                MCUPort.DtrEnable = true;           // !! important
                MCUPort.ReadBufferSize = 32 * 1024;
                try
                {
                    MCUPort.Open();
                    connectToolStripMenuItem.Enabled = false;
                    disconnectToolStripMenuItem.Enabled = true;
                    toolStripDropDownButtonPort.Text = "Port:connected";
                    timerReadout.Enabled = true;
                }
                catch (Exception ex)
                {
                    textBoxMCUmsg.AppendText("error in [connectToolStripMenuItem_Click]" + " \r\n");
                    textBoxMCUmsg.AppendText(ex.Message + "\r\n");
                }
            }
        }
        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MCUPort.IsOpen)
            {
                try
                {
                    MCUPort.Close();
                    connectToolStripMenuItem.Enabled = true;
                    disconnectToolStripMenuItem.Enabled = false;
                    toolStripDropDownButtonPort.Text = "Port:disconnected";
                    timerReadout.Enabled = false;
                }
                catch (Exception ex)
                {
                    textBoxMCUmsg.AppendText("error in [disconnectToolStripMenuItem_Click]" + "\r\n");
                    textBoxMCUmsg.AppendText(ex.Message + "\r\n");
                }
            }
        }
        private void buttonClearMCUMsg_Click(object sender, EventArgs e)
        {
            textBoxMCUmsg.Clear();
        }
        private void prepareDataStorage(byte dtype)
        {
            int i, j;
            if (dtype == 0)  // series of FID 
            {
                Data.nrows = acqParam.nFIDs;
                Data.dataLength = acqParam.FID_samples;
                Data.displayPeriod = 2;
            }
            else if (dtype == 1)  //  echoes train
            {
                Data.nrows = acqParam.nEchoes;
                Data.dataLength = acqParam.Echo_samples;
                Data.displayPeriod = 2 * acqParam.nEchoes;

            }
            Data.ncols = acqParam.nAvg;
            Data.elements = Data.ncols * Data.nrows;
            Data.signal = new IQsignal[Data.ncols, Data.nrows];
            for (j = 0; j < Data.ncols; j++)          // allocate memmory for IQ signals
                for (i = 0; i < Data.nrows; i++)
                {
                    Data.signal[j, i].I = new ushort[Data.dataLength];
                    Data.signal[j, i].Q = new ushort[Data.dataLength];
                }
            Data.procData = new ProcData[Data.nrows];
            Data.count = 0;
            Data.icol = 0;
            Data.irow = 0;
            Data.dataBytes = 2 * Data.dataLength;
            Data.dataType = dtype;
            Data.realtimeProc = checkBoxRealtimeProc.Checked;
            tbuf.byteToRead = 0;
            tbuf.readBytes = 0;
            chartProcess.Series["S1"].Points.Clear();
        }
        private void timerReadout_Tick(object sender, EventArgs e)
        {
            if (!MCUPort.IsOpen) return;
            while (MCUPort.BytesToRead > 0)
            {
                if (tbuf.byteToRead == 0)           // new data packet -> read header
                {
                    int hrb = 0;
                    while (hrb < 3)
                        hrb = hrb + MCUPort.Read(tbuf.header, hrb, 3 - hrb);
                    tbuf.serialDataType = tbuf.header[0];       // data type
                    tbuf.byteToRead = tbuf.header[1] + (tbuf.header[2] << 8);
                    if (tbuf.serialDataType > 0)
                        tbuf.readBytes = 0;
                }
                if (tbuf.serialDataType == 0)       // data is a text message
                {
                    textBoxMCUmsg.AppendText(MCUPort.ReadLine() + "\r\n");
                }
                else                                // data is IQ signals
                {
                    // read data 1 packet
                    while ((tbuf.readBytes < tbuf.byteToRead) && (MCUPort.BytesToRead > 0))
                        tbuf.readBytes = tbuf.readBytes + MCUPort.Read(tbuf.Buf, tbuf.readBytes, tbuf.byteToRead - tbuf.readBytes);

                    if ((tbuf.byteToRead == tbuf.readBytes) && (tbuf.readBytes > 0))   // data packet reading completed -> conversion
                    {
                        // data conversion
                        Data.icol = (Data.count / 2) / Data.nrows;
                        Data.irow = (Data.count / 2) % Data.nrows;
                        if ((Data.count % 2) == 0)    // I signal : event number
                            Buffer.BlockCopy(tbuf.Buf, 0, Data.signal[Data.icol, Data.irow].I, 0, tbuf.byteToRead);
                        else                          // Q signal : odd number
                            Buffer.BlockCopy(tbuf.Buf, 0, Data.signal[Data.icol, Data.irow].Q, 0, tbuf.byteToRead);
                        // next signal
                        Data.count = Data.count + 1;
                        // update display (this process wastes time a lot ; need improvement)
                        if ((Data.count % Data.displayPeriod) == 0)
                            chart_update();
                        // Finishing
                        if (Data.count == Data.elements * 2)    // read completed
                        {
                            if (acqParam.runMode == -1)        // data collection mode 
                            {
                                toolStripButtonRunStatus.Image = imageList1.Images[0];
                                textBoxAcqFIDSam.Enabled = true;
                                textBoxAcqEchoSam.Enabled = true;
                                seq_Running = false;
                                textBoxMCUmsg.AppendText(" Data collection :: COMPLETED... \r\n");
                            }
                            Data.count = 0;
                        }
                        tbuf.byteToRead = 0;
                    }
                }
            }
        }
        private void chart_update()
        {
            int i, j;
            chartRaw.Series["I"].Points.Clear();
            chartRaw.Series["Q"].Points.Clear();
            if (Data.dataType == 0)   // single signal per seq
            {
                int n = Data.dataLength / rawSampling;
                for (i = 0; i < n; i++)
                {
                    chartRaw.Series["I"].Points.AddXY((double)i * rawSampling, Data.signal[Data.icol, Data.irow].I[i * rawSampling] - acqParam.I_offset);
                    chartRaw.Series["Q"].Points.AddXY((double)i * rawSampling, Data.signal[Data.icol, Data.irow].Q[i * rawSampling] - acqParam.Q_offset);
                }
            }
            else if (Data.dataType == 1)  // multi signals per seq
            {
                int n = Data.dataLength / rawSampling;
                for (j = 0; j < Data.nrows; j++)
                    for (i = 0; i < n; i++)
                    {
                        chartRaw.Series["I"].Points.AddXY((double)(j * Data.dataLength + i * rawSampling), Data.signal[Data.icol, j].I[i * rawSampling] - acqParam.I_offset);
                        chartRaw.Series["Q"].Points.AddXY((double)(j * Data.dataLength + i * rawSampling), Data.signal[Data.icol, j].Q[i * rawSampling] - acqParam.Q_offset);
                    }
            }
            //chartRaw.Update();
            if (checkBoxRealtimeProc.Checked)
            {
                if (activeSeq == "calib")
                {
                    Data.procData[Data.irow].x = (seq_calib.Tpw0.value + seq_calib.Tdpw.value * (double)Data.irow) / 1000.0;    // time in ms
                    Data.procData[Data.irow].y = getTestPoint(Data.irow);
                    chartProcess.Series["S1"].Points.AddXY(Data.procData[Data.irow].x, Data.procData[Data.irow].y);
                }
                else if (activeSeq == "cpmg")
                {
                    chartProcess.Series["S1"].Points.Clear();
                    for (j = 0; j < Data.nrows; j++)
                    {
                        Data.procData[j].x = seq_CPMG.Tte.value * (double)(j + 1);
                        Data.procData[j].y = getTestPoint(j);
                        chartProcess.Series["S1"].Points.AddXY(Data.procData[j].x, Data.procData[j].y);
                    }
                }
                else if (activeSeq == "ir")
                {
                    Data.procData[Data.irow].x = seq_IR.IRtau.value * (double)(Data.irow + 1);
                    Data.procData[Data.irow].y = getTestPoint(Data.irow);
                    chartProcess.Series["S1"].Points.AddXY(Data.procData[Data.irow].x, Data.procData[Data.irow].y);
                }
            }
        }

        private void checkBoxZoomX_CheckedChanged(object sender, EventArgs e)
        {
            chartRaw.ChartAreas[0].CursorX.IsUserSelectionEnabled = checkBoxZoomX.Checked;
        }
        private void checkBoxZoomY_CheckedChanged(object sender, EventArgs e)
        {
            chartRaw.ChartAreas[0].CursorY.IsUserSelectionEnabled = checkBoxZoomY.Checked;
        }
        private void textBoxCommand_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                try
                {
                    int colonPos = textBoxCommand.Text.IndexOf(":");
                    if (colonPos > 0)   // this is a command to change parameter
                    {
                        string command = textBoxCommand.Text.Remove(colonPos);
                        string param = textBoxCommand.Text.Remove(0, colonPos + 1);
                        // Technical parameters
                        if (command == "dt")
                            techParam.Tdeadtime = Double.Parse(param);
                        // Pulse and DDS parameters
                        else if (command == "p90")
                        {
                            textBoxP90.Text = param;
                            pulseParam.p90 = Double.Parse(param);
                        }
                        else if (command == "p180")
                        {
                            textBoxP180.Text = param;
                            pulseParam.p180 = Double.Parse(param);
                        }
                        else if (command == "freq")
                        {
                            textBoxFreq.Text = param;
                            textBoxF0.Text = param;
                            textBoxF1.Text = param;
                            textBoxF2.Text = param;
                            textBoxF3.Text = param;
                            pulseParam.refFreq = Double.Parse(param);
                            pulseParam.DDS.f0 = pulseParam.refFreq;
                            pulseParam.DDS.f1 = pulseParam.refFreq;
                            pulseParam.DDS.f2 = pulseParam.refFreq;
                            pulseParam.DDS.f3 = pulseParam.refFreq;
                        }
                        else if (command == "f0")
                        {
                            textBoxF0.Text = param;
                            pulseParam.DDS.f0 = Double.Parse(param);
                        }
                        else if (command == "f1")
                        {
                            textBoxF1.Text = param;
                            pulseParam.DDS.f1 = Double.Parse(param);
                        }
                        else if (command == "f2")
                        {
                            textBoxF2.Text = param;
                            pulseParam.DDS.f2 = Double.Parse(param);
                        }
                        else if (command == "f3")
                        {
                            textBoxF3.Text = param;
                            pulseParam.DDS.f3 = Double.Parse(param);
                        }
                        else if (command == "amp0")
                        {
                            textBoxDDSAmp0.Text = param;
                            pulseParam.DDS.amp0 = Int32.Parse(param);
                        }
                        else if (command == "amp1")
                        {
                            textBoxDDSAmp1.Text = param;
                            pulseParam.DDS.amp1 = Int32.Parse(param);
                        }
                        else if (command == "amp2")
                        {
                            textBoxDDSAmp2.Text = param;
                            pulseParam.DDS.amp2 = Int32.Parse(param);
                        }
                        else if (command == "amp3")
                        {
                            textBoxDDSAmp3.Text = param;
                            pulseParam.DDS.amp3 = Int32.Parse(param);
                        }
                        else if (command == "ph0")
                        {
                            textBoxDDSPh0.Text = param;
                            pulseParam.DDS.ph0 = Double.Parse(param);
                        }
                        else if (command == "ph1")
                        {
                            textBoxDDSPh1.Text = param;
                            pulseParam.DDS.ph1 = Double.Parse(param);
                        }
                        else if (command == "ph2")
                        {
                            textBoxDDSPh2.Text = param;
                            pulseParam.DDS.ph2 = Double.Parse(param);
                        }
                        else if (command == "ph3")
                        {
                            textBoxDDSPh3.Text = param;
                            pulseParam.DDS.ph3 = Double.Parse(param);
                        }
                        // Acquisition parameters
                        else if (command == "mode")
                        {
                            acqParam.runMode = Int32.Parse(param);
                            if (acqParam.runMode == 0)
                                toolStripComboBoxMode.SelectedIndex = 0;
                            else if (acqParam.runMode == -1)
                                toolStripComboBoxMode.SelectedIndex = 1;
                        }
                        else if (command == "navg")
                        {
                            textBoxAcqNavg.Text = param;
                            acqParam.nAvg = Int32.Parse(param);
                        }
                        else if (command == "tr")
                        {
                            textBoxAcqTR.Text = param;
                            acqParam.Ttr = Double.Parse(param);

                        }
                        else if (command == "trdf")
                        {
                            textBoxAcqFIDR.Text = param;
                            acqParam.TreadoutF = Double.Parse(param);
                        }
                        else if (command == "trde")
                        {
                            textBoxAcqEchoR.Text = param;
                            acqParam.TreadoutE = Double.Parse(param);
                        }
                        else if (command == "fsam")
                        {
                            textBoxAcqFIDSam.Text = param;
                            acqParam.FID_samples = Int32.Parse(param);
                        }
                        else if (command == "esam")
                        {
                            textBoxAcqEchoSam.Text = param;
                            acqParam.Echo_samples = Int32.Parse(param);
                        }
                        // tuning parameters
                        // calibration parameters
                        else if (command == seq_calib.Tpw0.cmd)
                            seq_calib.Tpw0.value = Double.Parse(param);
                        else if (command == seq_calib.Tdpw.cmd)
                            seq_calib.Tdpw.value = Double.Parse(param);
                        else if (command == seq_calib.npw.cmd)
                            seq_calib.npw.value = Int32.Parse(param);
                        // CPMG parameters
                        else if (command == seq_CPMG.Tte.cmd)
                            seq_CPMG.Tte.value = Double.Parse(param);
                        else if (command == seq_CPMG.ne.cmd)
                            seq_CPMG.ne.value = Int32.Parse(param);
                        // IR parameters
                        else if (command == seq_IR.IRtau.cmd)
                            seq_IR.IRtau.value = Double.Parse(param);
                        else if (command == seq_IR.nIR.cmd)
                            seq_IR.nIR.value = Int32.Parse(param);
                    }
                    else
                    {
                        if (FIDCollectionCom.Contains(textBoxCommand.Text)) // a seqeunce that need FIDs collection 
                        {
                            groupBoxACtiveSeq.Text = "Active Sequence : " + textBoxCommand.Text;
                            activeSeq = textBoxCommand.Text;
                            if (textBoxCommand.Text == "tuning")
                            {
                                acqParam.nFIDs = 1;
                            }
                            else if (textBoxCommand.Text == "calib")
                            {
                                acqParam.nFIDs = seq_calib.npw.value;        // neccessary to prepare data storage
                            }
                            else if (textBoxCommand.Text == "ir")
                            {
                                acqParam.nFIDs = seq_IR.nIR.value;           // neccessary to prepare data storage
                            }
                            prepareDataStorage(0);
                        }
                        else if (echoCollectionCom.Contains(textBoxCommand.Text)) // a seqeunce that need echoes collection
                        {

                            groupBoxACtiveSeq.Text = "Active Sequence : " + textBoxCommand.Text;
                            activeSeq = textBoxCommand.Text;
                            if (textBoxCommand.Text == "cpmg")
                            {
                                acqParam.nEchoes = seq_CPMG.ne.value;
                            }
                            prepareDataStorage(1);
                        }
                        else
                        {
                            if (textBoxCommand.Text == "clr")
                                textBoxMCUmsg.Clear();
                        }
                    }
                    MCUPort.Write(textBoxCommand.Text);
                    textBoxCommand.Text = "";
                }
                catch (Exception ex)
                {
                    textBoxMCUmsg.AppendText("error in [textBoxCommand_KeyPress]" + " \r\n");
                    textBoxMCUmsg.AppendText(ex.Message + "\r\n");
                }
            }
        }
        private void toolStripMenuItemSaveRaw_Click(object sender, EventArgs e)
        {
            try
            {
                double t, I, Q;
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    using (StreamWriter writer = new StreamWriter(saveFileDialog1.FileName))
                    {
                        writer.WriteLine("Raw data");
                        for (int j = 0; j < Data.ncols; j++)
                        {
                            writer.WriteLine("Data set #" + j.ToString());
                            for (int i = 0; i < Data.nrows; i++)
                            {
                                writer.WriteLine("Signal #" + i.ToString());
                                writer.WriteLine("ms, I, Q");
                                for (int k = 0; k < Data.dataLength; k++)
                                {
                                    t = k * acqParam.TreadoutF / acqParam.FID_samples;
                                    I = Data.signal[i, j].I[k] - acqParam.I_offset;
                                    Q = Data.signal[i, j].Q[k] - acqParam.Q_offset;
                                    writer.WriteLine(t.ToString("F6") + (char)9 + I.ToString("F6") + (char)9 + Q.ToString("F6"));   // (char)9 is TAB character
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                textBoxMCUmsg.AppendText("[toolStripMenuItemSaveRaw_Click] " + exc.Message);
            }
        }
        private void toolStripComboBoxMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (toolStripComboBoxMode.SelectedIndex == 0)    // setup mode
            {
                acqParam.runMode = 0;
                if (MCUPort.IsOpen)
                    MCUPort.Write("mode:0");

            }
            else if (toolStripComboBoxMode.SelectedIndex == 1)   // data collection mode
            {
                acqParam.runMode = -1;
                if (MCUPort.IsOpen)
                    MCUPort.Write("mode:-1");
            }
        }
        private void toolStripButtonRun_Click(object sender, EventArgs e)
        {
            if (activeSeq == "none")
            {
                textBoxMCUmsg.AppendText("No sequence to run. please complie a sequence.\r\n");
                return;
            }
            if (MCUPort.IsOpen)
            {
                MCUPort.Write("run");
                toolStripButtonRunStatus.Image = imageList1.Images[1];
                textBoxAcqFIDSam.Enabled = false;
                textBoxAcqEchoSam.Enabled = false;
                seq_Running = true;
            }
        }
        private void toolStripButtonStop_Click(object sender, EventArgs e)
        {
            if (!seq_Running) return;
            if (MCUPort.IsOpen)
            {
                MCUPort.Write("stop");
                toolStripButtonRunStatus.Image = imageList1.Images[0];
                textBoxAcqFIDSam.Enabled = true;
                textBoxAcqEchoSam.Enabled = true;
                seq_Running = false;
            }
        }
        private void textBoxParamEdit_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                try
                {
                    TextBox tb = (TextBox)sender;
                    if (tb == textBoxP90)
                    {
                        pulseParam.p90 = Convert.ToDouble(tb.Text);
                        MCUPort.Write("p90:" + tb.Text);
                    }
                    else if (tb == textBoxP180)
                    {
                        pulseParam.p180 = Convert.ToDouble(tb.Text);
                        MCUPort.Write("p180:" + tb.Text);
                    }
                    else if (tb == textBoxFreq)
                    {
                        pulseParam.refFreq = Convert.ToDouble(tb.Text);
                        pulseParam.DDS.f0 = pulseParam.refFreq;
                        pulseParam.DDS.f1 = pulseParam.refFreq;
                        pulseParam.DDS.f2 = pulseParam.refFreq;
                        pulseParam.DDS.f3 = pulseParam.refFreq;
                        textBoxF0.Text = pulseParam.DDS.f0.ToString();
                        textBoxF1.Text = pulseParam.DDS.f1.ToString();
                        textBoxF2.Text = pulseParam.DDS.f2.ToString();
                        textBoxF3.Text = pulseParam.DDS.f3.ToString();
                        MCUPort.Write("freq:" + tb.Text);
                    }
                    else if (tb == textBoxF0)
                    {
                        pulseParam.DDS.f0 = Convert.ToDouble(tb.Text);
                        MCUPort.Write("f0:" + tb.Text);
                    }
                    else if (tb == textBoxF1)
                    {
                        pulseParam.DDS.f1 = Convert.ToDouble(tb.Text);
                        MCUPort.Write("f1:" + tb.Text);
                    }
                    else if (tb == textBoxF2)
                    {
                        pulseParam.DDS.f2 = Convert.ToDouble(tb.Text);
                        MCUPort.Write("f2:" + tb.Text);
                    }
                    else if (tb == textBoxF3)
                    {
                        pulseParam.DDS.f3 = Convert.ToDouble(tb.Text);
                        MCUPort.Write("f3:" + tb.Text);
                    }
                    else if (tb == textBoxDDSAmp0)
                    {
                        pulseParam.DDS.amp0 = Convert.ToInt32(tb.Text);
                        MCUPort.Write("amp0:" + tb.Text);
                    }
                    else if (tb == textBoxDDSAmp1)
                    {
                        pulseParam.DDS.amp1 = Convert.ToInt32(tb.Text);
                        MCUPort.Write("amp1:" + tb.Text);
                    }
                    else if (tb == textBoxDDSAmp2)
                    {
                        pulseParam.DDS.amp2 = Convert.ToInt32(tb.Text);
                        MCUPort.Write("amp2:" + tb.Text);
                    }
                    else if (tb == textBoxDDSAmp3)
                    {
                        pulseParam.DDS.amp3 = Convert.ToInt32(tb.Text);
                        MCUPort.Write("amp3:" + tb.Text);
                    }
                    else if (tb == textBoxDDSPh0)
                    {
                        pulseParam.DDS.ph0 = Convert.ToDouble(tb.Text);
                        MCUPort.Write("ph0:" + tb.Text);
                    }
                    else if (tb == textBoxDDSPh1)
                    {
                        pulseParam.DDS.ph1 = Convert.ToDouble(tb.Text);
                        MCUPort.Write("ph1:" + tb.Text);
                    }
                    else if (tb == textBoxDDSPh2)
                    {
                        pulseParam.DDS.ph2 = Convert.ToDouble(tb.Text);
                        MCUPort.Write("ph2:" + tb.Text);
                    }
                    else if (tb == textBoxDDSPh3)
                    {
                        pulseParam.DDS.ph3 = Convert.ToDouble(tb.Text);
                        MCUPort.Write("ph3:" + tb.Text);
                    }
                    else if (tb == textBoxAcqTR)
                    {
                        acqParam.Ttr = Convert.ToDouble(tb.Text);
                        MCUPort.Write("tr:" + tb.Text);
                    }
                    else if (tb == textBoxAcqNavg)
                    {
                        acqParam.nAvg = Convert.ToInt32(tb.Text);
                        MCUPort.Write("navg:" + tb.Text);
                    }
                    else if (tb == textBoxAcqFIDR)
                    {
                        acqParam.TreadoutF = Convert.ToDouble(tb.Text);
                        MCUPort.Write("trdf:" + tb.Text);
                    }
                    else if (tb == textBoxAcqEchoR)
                    {
                        acqParam.TreadoutE = Convert.ToDouble(tb.Text);
                        MCUPort.Write("trde:" + tb.Text);
                    }
                    else if (tb == textBoxAcqFIDSam)
                    {
                        acqParam.FID_samples = Convert.ToInt32(tb.Text);
                        MCUPort.Write("fsam:" + tb.Text);
                    }
                    else if (tb == textBoxAcqEchoSam)
                    {
                        acqParam.Echo_samples = Convert.ToInt32(tb.Text);
                        MCUPort.Write("esam:" + tb.Text);
                    }
                }
                catch (Exception ex)
                {
                    textBoxMCUmsg.AppendText("error in [toolStTextBoxAD9959_KeyPress]" + " \r\n");
                    textBoxMCUmsg.AppendText(ex.Message + "\r\n");
                }
            }
        }
        private void textBoxSeqParam_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                try
                {
                    String paramName = listViewSeqParams.SelectedItems[0].SubItems[0].Text;
                    String Command = "";
                    listViewSeqParams.SelectedItems[0].SubItems[1].Text = textBoxSeqParam.Text;
                    if (paramName == seq_calib.Tpw0.name)
                    {
                        seq_calib.Tpw0.value = Convert.ToDouble(textBoxSeqParam.Text);
                        Command = seq_calib.Tpw0.cmd + textBoxSeqParam.Text;
                    }
                    else if (paramName == seq_calib.Tdpw.name)
                    {
                        seq_calib.Tdpw.value = Convert.ToDouble(textBoxSeqParam.Text);
                        Command = seq_calib.Tdpw.cmd + textBoxSeqParam.Text;
                    }
                    else if (paramName == seq_calib.npw.name)
                    {
                        seq_calib.npw.value = Convert.ToInt32(textBoxSeqParam.Text);
                        Command = seq_calib.npw.cmd + textBoxSeqParam.Text;
                    }
                    else if (paramName == seq_calib.IorQ.name)
                    {
                        seq_calib.IorQ.value = Convert.ToInt32(textBoxSeqParam.Text);
                        Command = seq_calib.IorQ.cmd + textBoxSeqParam.Text;
                    }
                    else if (paramName == seq_CPMG.Tte.name)
                    {
                        seq_CPMG.Tte.value = Convert.ToDouble(textBoxSeqParam.Text);
                        Command = seq_CPMG.Tte.cmd + textBoxSeqParam.Text;
                    }
                    else if (paramName == seq_CPMG.ne.name)
                    {
                        seq_CPMG.ne.value = Convert.ToInt32(textBoxSeqParam.Text);
                        Command = seq_CPMG.ne.cmd + textBoxSeqParam.Text;
                    }
                    else if (paramName == seq_IR.IRtau.name)
                    {
                        seq_IR.IRtau.value = Convert.ToDouble(textBoxSeqParam.Text);
                        Command = seq_IR.IRtau.cmd + textBoxSeqParam.Text;
                    }
                    else if (paramName == seq_IR.nIR.name)
                    {
                        seq_IR.nIR.value = Convert.ToInt32(textBoxSeqParam.Text);
                        Command = seq_IR.nIR.cmd + textBoxSeqParam.Text;
                    }
                    if (Command != "")
                        MCUPort.Write(Command);
                }
                catch (Exception ex)
                {
                    textBoxMCUmsg.AppendText("error in [textBoxSeqParam_KeyPress]" + " \r\n");
                    textBoxMCUmsg.AppendText(ex.Message + "\r\n");
                }
            }
        }
        private void listViewSeqParams_MouseClick(object sender, MouseEventArgs e)
        {
            textBoxSeqParam.Text = listViewSeqParams.SelectedItems[0].SubItems[1].Text;
            labelParamName.Text = listViewSeqParams.SelectedItems[0].SubItems[0].Text;
        }
        private void toolStripButtonComp_Click(object sender, EventArgs e)
        {
            if (MCUPort.IsOpen)
            {
                if (activeSeq == "tuning")
                {
                    MCUPort.Write("tuning");
                    acqParam.nFIDs = 1;
                    prepareDataStorage(0);
                }
                else if (activeSeq == "calib")
                {
                    MCUPort.Write("calib");
                    acqParam.nFIDs = seq_calib.npw.value;
                    prepareDataStorage(0);
                }
                else if (activeSeq == "cpmg")
                {
                    MCUPort.Write("cpmg");
                    acqParam.nEchoes = seq_CPMG.ne.value;
                    prepareDataStorage(1);
                }
                else if (activeSeq == "ir")
                {
                    MCUPort.Write("ir");
                    acqParam.nFIDs = seq_IR.nIR.value;
                    prepareDataStorage(0);
                }
            }
        }
        private void addListviewSeqParam(ListView TheLV, object sender)
        {
            if (sender is DoubleParam)
            {
                DoubleParam p = (DoubleParam)sender;
                String[] row = { p.name, p.value.ToString(), p.unit };
                ListViewItem item = new ListViewItem(row);
                TheLV.Items.Add(item);
            }
            else if (sender is IntParam)
            {
                IntParam p = (IntParam)sender;
                String[] row = { p.name, p.value.ToString(), p.unit };
                ListViewItem item = new ListViewItem(row);
                TheLV.Items.Add(item);
            }
        }
        private void toolStripMenuItemSaveParams_Click(object sender, EventArgs e)
        {
            try
            {
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    using (StreamWriter sw = new StreamWriter(saveFileDialog1.FileName))
                    {
                        // pulse and dds paramters
                        sw.WriteLine("pulse and dds paramters");
                        sw.WriteLine("p90 = " + pulseParam.p90.ToString());           // us
                        sw.WriteLine("p180 = " + pulseParam.p180.ToString());         // us
                        sw.WriteLine("freq = " + pulseParam.refFreq.ToString());      // MHz
                        sw.WriteLine("f0 = " + pulseParam.DDS.f0.ToString());
                        sw.WriteLine("f1 = " + pulseParam.DDS.f1.ToString());
                        sw.WriteLine("f2 = " + pulseParam.DDS.f2.ToString());
                        sw.WriteLine("f3 = " + pulseParam.DDS.f3.ToString());
                        sw.WriteLine("ddsamp0 = " + pulseParam.DDS.amp0.ToString());  // max 1023 = 2.2 volt
                        sw.WriteLine("ddsamp1 = " + pulseParam.DDS.amp1.ToString());  // max 1023 = 2.2 volt
                        sw.WriteLine("ddsamp2 = " + pulseParam.DDS.amp2.ToString());  // max 1023 = 2.2 volt
                        sw.WriteLine("ddsamp3 = " + pulseParam.DDS.amp3.ToString());  // max 1023 = 2.2 volt
                        sw.WriteLine("ddsph0 = " + pulseParam.DDS.ph0.ToString());    // degree
                        sw.WriteLine("ddsph1 = " + pulseParam.DDS.ph1.ToString());    // degree
                        sw.WriteLine("ddsph2 = " + pulseParam.DDS.ph2.ToString());    // degree
                        sw.WriteLine("ddsph3 = " + pulseParam.DDS.ph3.ToString());    // degree
                        // data acquisition parameters
                        sw.WriteLine("acquisition parameters");
                        sw.WriteLine("mode = " + acqParam.runMode.ToString());         // 0 = interactive ; -1 = data collection
                        sw.WriteLine("navg = " + acqParam.nAvg.ToString());
                        sw.WriteLine("tr = " + acqParam.Ttr.ToString());               // ms
                        sw.WriteLine("trdf = " + acqParam.TreadoutF.ToString());       // ms
                        sw.WriteLine("trde = " + acqParam.TreadoutE.ToString());       // ms
                        sw.WriteLine("fsam = " + acqParam.FID_samples.ToString());     // points
                        sw.WriteLine("esam = " + acqParam.Echo_samples.ToString());    // points
                        sw.WriteLine("nfid = " + acqParam.nFIDs.ToString());
                        sw.WriteLine("nechoes = " + acqParam.nEchoes.ToString());
                        sw.WriteLine("I_offset = " + acqParam.I_offset.ToString());
                        sw.WriteLine("Q_offset = " + acqParam.Q_offset.ToString());
                    }
                }
            }
            catch (Exception exc)
            {
                textBoxMCUmsg.AppendText("Error: " + exc.Message);
            }
        }
        private void toolStripMenuItemLoadParams_Click(object sender, EventArgs e)
        {
            try
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    using (StreamReader sr = new StreamReader(openFileDialog1.FileName))
                    {
                        string[] ss;
                        // pulse and dds paramters
                        sr.ReadLine();                                  // read section title
                        ss = sr.ReadLine().Split('=');
                        pulseParam.p90 = double.Parse(ss[1]);           // us
                        ss = sr.ReadLine().Split('=');
                        pulseParam.p180 = double.Parse(ss[1]);          // us
                        ss = sr.ReadLine().Split('=');
                        pulseParam.refFreq = double.Parse(ss[1]);       // MHz
                        ss = sr.ReadLine().Split('=');
                        pulseParam.DDS.f0 = double.Parse(ss[1]);
                        ss = sr.ReadLine().Split('=');
                        pulseParam.DDS.f1 = double.Parse(ss[1]);
                        ss = sr.ReadLine().Split('=');
                        pulseParam.DDS.f2 = double.Parse(ss[1]);
                        ss = sr.ReadLine().Split('=');
                        pulseParam.DDS.f3 = double.Parse(ss[1]);
                        ss = sr.ReadLine().Split('=');
                        pulseParam.DDS.amp0 = Int32.Parse(ss[1]);       // max 1023 = 2.2 volt
                        ss = sr.ReadLine().Split('=');
                        pulseParam.DDS.amp1 = Int32.Parse(ss[1]);       // max 1023 = 2.2 volt
                        ss = sr.ReadLine().Split('=');
                        pulseParam.DDS.amp2 = Int32.Parse(ss[1]);       // max 1023 = 2.2 volt
                        ss = sr.ReadLine().Split('=');
                        pulseParam.DDS.amp3 = Int32.Parse(ss[1]);       // max 1023 = 2.2 volt
                        ss = sr.ReadLine().Split('=');
                        pulseParam.DDS.ph0 = double.Parse(ss[1]);       // degree
                        ss = sr.ReadLine().Split('=');
                        pulseParam.DDS.ph1 = double.Parse(ss[1]);       // degree
                        ss = sr.ReadLine().Split('=');
                        pulseParam.DDS.ph2 = double.Parse(ss[1]);       // degree
                        ss = sr.ReadLine().Split('=');
                        pulseParam.DDS.ph3 = double.Parse(ss[1]);       // degree
                        // data acquisition parameters
                        sr.ReadLine();  // read section title
                        ss = sr.ReadLine().Split('=');
                        acqParam.runMode = Int32.Parse(ss[1]);          // 0 = interactive ; -1 = data collection
                        ss = sr.ReadLine().Split('=');
                        acqParam.nAvg = Int32.Parse(ss[1]);
                        ss = sr.ReadLine().Split('=');
                        acqParam.Ttr = double.Parse(ss[1]);             // ms
                        ss = sr.ReadLine().Split('=');
                        acqParam.TreadoutF = double.Parse(ss[1]);       // ms
                        ss = sr.ReadLine().Split('=');
                        acqParam.TreadoutE = double.Parse(ss[1]);       // ms
                        ss = sr.ReadLine().Split('=');
                        acqParam.FID_samples = Int32.Parse(ss[1]);      // points
                        ss = sr.ReadLine().Split('=');
                        acqParam.Echo_samples = Int32.Parse(ss[1]);     // points
                        ss = sr.ReadLine().Split('=');
                        acqParam.nFIDs = Int32.Parse(ss[1]);
                        ss = sr.ReadLine().Split('=');
                        acqParam.nEchoes = Int32.Parse(ss[1]);
                        ss = sr.ReadLine().Split('=');
                        acqParam.I_offset = double.Parse(ss[1]);
                        ss = sr.ReadLine().Split('=');
                        acqParam.Q_offset = double.Parse(ss[1]);
                    }
                }
                refreshParams();
                uploadParams();
            }
            catch (Exception ex)
            {
                textBoxMCUmsg.AppendText("Error: " + ex.Message);
            }
        }
        private double getTestPoint(int row)
        {
            int n1 = Convert.ToInt32(textBoxProcPoint1.Text);
            int n2 = Convert.ToInt32(textBoxProcPoint2.Text);
            double[] m = new double[Data.dataLength];
            double x, y;
            double sum;
            double v = 0;
            for (int i = 0; i < Data.dataLength; i++)
            {
                x = 0;
                y = 0;
                for (int k = 0; k < Data.ncols; k++)
                {
                    x = x + (Data.signal[k, row].I[i] - acqParam.I_offset);
                    y = y + (Data.signal[k, row].Q[i] - acqParam.Q_offset);
                }
                x = x / (double)Data.ncols;         // avreage
                y = y / (double)Data.ncols;         // average
                m[i] = Math.Sqrt(x * x + y * y);    // modulus
            }
            if (radioButtonMax.Checked)
            {
                v = m.Max();
            }
            else if (radioButtonSpec.Checked)
            {
                sum = 0;
                for (int j = n1; j <= n2; j++)
                    sum = sum + m[j];                
                v = sum/(n2-n1);
            }
            return (v);
        }
        private void toolStripButtonProc_Click(object sender, EventArgs e)
        {
            if (activeSeq == "calib")
            {
                chartProcess.Series["S1"].Points.Clear();
                for (int j = 0; j < Data.nrows; j++)
                {
                    Data.procData[j].x = (seq_calib.Tpw0.value + seq_calib.Tdpw.value * (double)j) / 1000.0;
                    Data.procData[j].y = getTestPoint(j);
                    chartProcess.Series["S1"].Points.AddXY(Data.procData[j].x, Data.procData[j].y);
                }
            }
            else if (activeSeq == "cpmg")
            {
                chartProcess.Series["S1"].Points.Clear();
                for (int j = 0; j < Data.nrows; j++)
                {
                    Data.procData[j].x = seq_CPMG.Tte.value * (double)(j + 1);
                    Data.procData[j].y = getTestPoint(j);
                    chartProcess.Series["S1"].Points.AddXY(Data.procData[j].x, Data.procData[j].y);
                }
            }
            else if (activeSeq == "ir")
            {
                chartProcess.Series["S1"].Points.Clear();
                for (int j = 0; j < Data.nrows; j++)
                {
                    Data.procData[j].x = seq_IR.IRtau.value * (double)(j + 1);
                    Data.procData[j].y = getTestPoint(j);
                    chartProcess.Series["S1"].Points.AddXY(Data.procData[j].x, Data.procData[j].y);
                }
            }
        }
        private void checkBoxRealtime_CheckedChanged(object sender, EventArgs e)
        {
            Data.realtimeProc = checkBoxRealtimeProc.Checked;
        }
        private void toolStripMenuItemSaveProc_Click(object sender, EventArgs e)
        {
            try
            {
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    using (StreamWriter sw = new StreamWriter(saveFileDialog1.FileName))
                    {
                        sw.WriteLine(activeSeq);
                        sw.WriteLine("Number of points = " + Data.nrows.ToString());
                        sw.WriteLine("ms, values");
                        for (int j = 0; j < chartProcess.Series["S1"].Points.Count; j++)
                            sw.WriteLine(Data.procData[j].x.ToString("F6") + (char)9 + Data.procData[j].y.ToString("F6"));  // (char)9 is TAB character
                    }
                }
            }
            catch (Exception exc)
            {
                textBoxMCUmsg.AppendText("Error: " + exc.Message);
            }
        }
        private void buttonUploadAll_Click(object sender, EventArgs e)
        {
            uploadParams();
        }
        private void buttonGetOffset_Click(object sender, EventArgs e)
        {
            try
            {
                double x, y;
                x = 0;
                y = 0;
                for (int i = 0; i < chartRaw.Series["I"].Points.Count; i++)
                {
                    x = x + chartRaw.Series["I"].Points[i].YValues[0];
                    y = y + chartRaw.Series["Q"].Points[i].YValues[0];

                }
                acqParam.I_offset = x / chartRaw.Series["I"].Points.Count;
                acqParam.Q_offset = y / chartRaw.Series["Q"].Points.Count;
                textBoxAcqIOffset.Text = acqParam.I_offset.ToString("F0");
                textBoxAcqQOffset.Text = acqParam.Q_offset.ToString("F0");
            }
            catch (Exception ex)
            {
                textBoxMCUmsg.AppendText("Error: " + ex.Message);
            }
        }
        private void textBoxIOffset_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                try
                {
                    acqParam.I_offset = Convert.ToDouble(textBoxAcqIOffset.Text);
                }
                catch (Exception ex)
                {
                    textBoxMCUmsg.AppendText("Error: " + ex.Message);
                }
            }
        }
        private void textBoxAcqQOffset_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                try
                {
                    acqParam.Q_offset = Convert.ToDouble(textBoxAcqQOffset.Text);
                }
                catch (Exception ex)
                {
                    textBoxMCUmsg.AppendText("Error: " + ex.Message);
                }
            }
        }
        private void textBoxRawSam_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                try
                {
                    rawSampling = Convert.ToInt32(textBoxRawSam.Text);
                }
                catch (Exception ex)
                {
                    textBoxMCUmsg.AppendText("Error in [textBoxRawSam] " + ex.Message);
                }
            }
        }
        private void textBoxMinY_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                try
                {
                    chartRaw.ChartAreas[0].AxisY.Minimum = Convert.ToInt32(textBoxMinY.Text);
                }
                catch (Exception ex)
                {
                    textBoxMCUmsg.AppendText("Error in [textBoxMinY] " + ex.Message);
                }
            }
        }
        private void textBoxMaxY_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                try
                {
                    chartRaw.ChartAreas[0].AxisY.Maximum = Convert.ToInt32(textBoxMaxY.Text);
                }
                catch (Exception ex)
                {
                    textBoxMCUmsg.AppendText("Error in [textBoxMaxY] " + ex.Message);
                }
            }
        }
        private void checkBoxFixY_CheckedChanged(object sender, EventArgs e)
        {
            textBoxMinY.Enabled = checkBoxFixY.Checked;
            textBoxMaxY.Enabled = checkBoxFixY.Checked;
            checkBoxZoomY.Enabled = !checkBoxFixY.Checked;

        }
        private void toolStripComboBoxSeq_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (toolStripComboBoxSeq.SelectedIndex == 0)
            {
                listViewSeqParams.Items.Clear();
            }
            else if (toolStripComboBoxSeq.SelectedIndex == 1)   // tuning
            {
                listViewSeqParams.Items.Clear();
                activeSeq = toolStripComboBoxSeq.Text;
            }
            else if (toolStripComboBoxSeq.SelectedIndex == 2)   // calib
            {
                listViewSeqParams.Items.Clear();
                activeSeq = toolStripComboBoxSeq.Text;
                addListviewSeqParam(listViewSeqParams, seq_calib.Tpw0);
                addListviewSeqParam(listViewSeqParams, seq_calib.Tdpw);
                addListviewSeqParam(listViewSeqParams, seq_calib.npw);
                addListviewSeqParam(listViewSeqParams, seq_calib.IorQ);
            }
            else if (toolStripComboBoxSeq.SelectedIndex == 3)   // cpmg
            {
                listViewSeqParams.Items.Clear();
                activeSeq = toolStripComboBoxSeq.Text;
                addListviewSeqParam(listViewSeqParams, seq_CPMG.Tte);
                addListviewSeqParam(listViewSeqParams, seq_CPMG.ne);
            }
            else if (toolStripComboBoxSeq.SelectedIndex == 4)   // ir
            {
                listViewSeqParams.Items.Clear();
                activeSeq = toolStripComboBoxSeq.Text;
                addListviewSeqParam(listViewSeqParams, seq_IR.IRtau);
                addListviewSeqParam(listViewSeqParams, seq_IR.nIR);
            }
        }
    }
}
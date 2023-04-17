using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Wit.SDK.Modular.Sensor.Modular.DataProcessor.Constant;
using Wit.SDK.Modular.WitSensorApi.Modular.HWT9073;

namespace Wit.Example_HWT9073_485
{
    /// <summary>
    /// 程序主窗口
    /// 说明：
    /// 1.本程序是维特智能开发的HWT9073485九轴传感器示例程序
    /// 2.适用示例程序前请咨询技术支持,询问本示例程序是否支持您的传感器
    /// 3.使用前请了解传感器的通信协议
    /// 4.本程序只有一个窗口,所有逻辑都在这里
    /// 
    /// Program main window
    /// Description:
    /// 1. This program is the WT901C485 9-axis sensor sample program developed by WitMotion
    /// 2. Please consult technical support before applying the sample program and ask whether the sample program supports your sensor
    /// 3. Understand the communication protocol of the sensor before using it
    /// 4. This program only has one window, all the logic is here
    /// </summary>
    public partial class Form1 : Form
    {
        /// <summary>
        /// HWT9073485
        /// </summary>
        private HWT9073485 HWT9073485 { get; set; } = new HWT9073485();

        /// <summary>
        /// HWT9073485支持的波特率
        /// Supported baud rate
        /// </summary>
        private List<int> SupportBaudRateList { get; set; } = new List<int>() { 4800, 9600, 19200, 38400, 57600, 115200, 230400 };

        /// <summary>
        /// 控制自动刷新数据线程是否工作
        /// Controls whether the auto flush data thread works
        /// </summary>
        public bool EnableRefreshDataTh { get; private set; }

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 窗体加载时
        /// Form load time
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            // 加载串口号到下拉框里      Loading serial port number
            portComboBox_MouseDown(null, null);

            // 加载波特率下拉框 Load baud rate
            for (int i = 0; i < SupportBaudRateList.Count; i++)
            {
                baudComboBox.Items.Add(SupportBaudRateList[i]);
            }
            // 默认选中9600 Default 9600
            baudComboBox.SelectedItem = 9600;

            // 启动刷新数据线程 Start refreshing data thread
            Thread thread = new Thread(RefreshDataTh);
            thread.IsBackground = true;
            EnableRefreshDataTh = true;
            thread.Start();
        }

        /// <summary>
        /// 窗体关闭时
        /// Form close time
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 关闭刷新数据线程 Close the refresh data thread
            EnableRefreshDataTh = false;
            // 关闭串口 Close serial port
            closeButton_Click(null, null);
        }

        /// <summary>
        /// 鼠标移动到串口号下拉框里时
        /// The mouse moves to the serial port number drop-down box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void portComboBox_MouseDown(object sender, MouseEventArgs e)
        {
            portComboBox.Items.Clear();
            string[] portNameList = SerialPort.GetPortNames();

            for (int i = 0; i < portNameList.Length; i++)
            {
                portComboBox.Items.Add(portNameList[i]);
            }
        }

        /// <summary>
        /// 打开设备
        /// Turn on the device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openButton_Click(object sender, EventArgs e)
        {
            // 获得连接的串口号和波特率  Obtain the serial port number and baud rate for the connection
            string portName;
            int baudrate;
            byte modbusId;
            try
            {
                portName = (string)portComboBox.SelectedItem;
                baudrate = (int)baudComboBox.SelectedItem;
                modbusId = byte.Parse(ModbustextBox.Text.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            // 不重复打开    Open without repeating
            if (HWT9073485.IsOpen())
            {
                return;
            }

            // 打开设备 Turn on the device
            try
            {
                HWT9073485.SetPortName(portName);
                HWT9073485.SetBaudrate(baudrate);
                HWT9073485.SetModbusId(modbusId);
                HWT9073485.Open();
                // 实现记录数据事件 Implement logging data events
                HWT9073485.OnRecord += HWT9073485_OnRecord;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// 当传感器数据刷新时会调用这里，您可以在这里记录数据
        /// This is called when the sensor data is refreshed, so you can record the data here
        /// </summary>
        /// <param name="HWT9073485"></param>
        private void HWT9073485_OnRecord(HWT9073485 HWT9073485)
        {
            string text = GetDeviceData(HWT9073485);
            Debug.WriteLine(text);
        }

        /// <summary>
        /// 关闭设备
        /// Shut down device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeButton_Click(object sender, EventArgs e)
        {
            try
            {
                // 如果已经打开了设备就关闭设备   Turn off the device if it is already on
                if (HWT9073485.IsOpen())
                {
                    HWT9073485.OnRecord -= HWT9073485_OnRecord;
                    HWT9073485.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// 刷新数据线程
        /// Refresh data thread
        /// </summary>
        private void RefreshDataTh()
        {
            while (EnableRefreshDataTh)
            {
                Thread.Sleep(100);
                if (HWT9073485.IsOpen())
                {
                    dataRichTextBox.Invoke(new Action(() =>
                    {
                        dataRichTextBox.Text = GetDeviceData(HWT9073485);
                    }));
                }

            }
        }

        /// <summary>
        /// 获得设备的数据
        /// Get the device's data
        /// </summary>
        private string GetDeviceData(HWT9073485 HWT9073485)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(HWT9073485.GetDeviceName()).Append("\n");
            // 加速度  ACC
            builder.Append("AccX").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.AccX)).Append("g \t");
            builder.Append("AccY").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.AccY)).Append("g \t");
            builder.Append("AccZ").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.AccZ)).Append("g \n");
            // 角速度  Angular velocity
            builder.Append("GyroX").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.AsX)).Append("°/s \t");
            builder.Append("GyroY").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.AsY)).Append("°/s \t");
            builder.Append("GyroZ").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.AsZ)).Append("°/s \n");
            // 角度   Angle
            builder.Append("AngleX").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.AngleX)).Append("° \t");
            builder.Append("AngleY").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.AngleY)).Append("° \t");
            builder.Append("AngleZ").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.AngleZ)).Append("° \n");
            // 磁场   Mag
            builder.Append("MagX").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.HX)).Append("uT \t");
            builder.Append("MagY").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.HY)).Append("uT \t");
            builder.Append("MagZ").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.HZ)).Append("uT \n");
            // 经纬度  Longitude and latitude
            builder.Append("Lon").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.Lon)).Append("′ \t");
            builder.Append("Lat").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.Lat)).Append("′ \n");
            // 端口号  Port
            builder.Append("D0").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.D0)).Append("\t");
            builder.Append("D1").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.D1)).Append("\t");
            builder.Append("D2").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.D2)).Append("\t");
            builder.Append("D3").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.D3)).Append("\n");
            // 四元数  Quaternion
            builder.Append("Q0").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.Q0)).Append("\t");
            builder.Append("Q1").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.Q1)).Append("\t");
            builder.Append("Q2").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.Q2)).Append("\t");
            builder.Append("Q3").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.Q3)).Append("\n");
            // 气压   Barometric
            builder.Append("P").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.Q1)).Append("Pa \t");
            builder.Append("H").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.Q2)).Append("m \t");
            // 温度   Temp
            builder.Append("T").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.T)).Append("℃ \n");
            // GPS
            builder.Append("GPSHeight").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.GPSHeight)).Append(" m \t");
            builder.Append("GPSYaw").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.GPSYaw)).Append("° \t");
            builder.Append("GPSV").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.GPSV)).Append("km/h \n");
            // 定位精度 Positioning accuracy
            builder.Append("PDOP").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.PDOP)).Append("\t");
            builder.Append("VDOP").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.VDOP)).Append("\t");
            builder.Append("HDOP").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.HDOP)).Append("\n");
            // 版本号  Version
            builder.Append("VersionNumber").Append(":").Append(HWT9073485.GetDeviceData(WitSensorKey.VersionNumber)).Append("\n");
            return builder.ToString();
        }

        /// <summary>
        /// 加计校准
        /// Acceleration calibration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void appliedCalibrationButton_Click(object sender, EventArgs e)
        {
            if (HWT9073485.IsOpen() == false)
            {
                return;
            }

            try
            {
                // 解锁寄存器并发送命令   Unlock the register and send the command
                HWT9073485.UnlockReg();
                HWT9073485.AppliedCalibration();
                // 下面两行与上面等价,推荐使用上面的    Equivalent to above
                //HWT9073485.SendProtocolData(Modbus16Utils.GetWrite(HWT9073485.GetModbusId(), 0x69, 0xB588));
                //HWT9073485.SendProtocolData(new byte[] { 50, 06, 00, 69, B5, 88, 22, A1 });
                //HWT9073485.SendProtocolData(Modbus16Utils.GetWrite(HWT9073485.GetModbusId(), 0x01, 0x0001));
                //HWT9073485.SendProtocolData(new byte[] { 50, 06, 00, 01, 00, 01, 32, 4B });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 读取03寄存器
        /// Read 03 register
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void readReg03Button_Click(object sender, EventArgs e)
        {
            if (HWT9073485.IsOpen() == false)
            {
                return;
            }
            try
            {
                // 等待时长 Unlock the register and send the command
                int waitTime = 150;
                // 发送读取命令，并且等待传感器返回数据，如果没读上来可以将 waitTime 延长，或者多读几次
                // Send a read command and wait for the sensor to return data. If it is not read, the waitTime can be extended or read several more times
                HWT9073485.SendReadReg(0x03, waitTime);
                // 下面这两行和上面等价推荐使用上面的    Equivalent to above
                //HWT9073485.SendProtocolData(Modbus16Utils.GetRead(HWT9073485.GetModbusId(), 0x03, 0x01), waitTime);
                //HWT9073485.SendProtocolData(new byte[] { 50, 03, 00, 03, 00, 01, 79, 8B }, waitTime);

                string reg03Value = HWT9073485.GetDeviceData("03");
                MessageBox.Show($"寄存器03值为 : {reg03Value}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 设置带宽20Hz
        /// Set bandwidth of 20Hz
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bandWidth20_Click(object sender, EventArgs e)
        {
            if (HWT9073485.IsOpen() == false)
            {
                return;
            }
            try
            {
                // 解锁寄存器并发送命令   Unlock the register and send the command
                HWT9073485.UnlockReg();
                HWT9073485.SetBandWidth(0x04);
                // 下面两行与上面等价,推荐使用上面的    Equivalent to above
                //HWT9073485.SendProtocolData(Modbus16Utils.GetWrite(HWT9073485.GetModbusId(), 0x69, 0xB588));
                //HWT9073485.SendProtocolData(new byte[] { 50, 06, 00, 69, B5, 88, 22, A1 });
                //HWT9073485.SendProtocolData(Modbus16Utils.GetWrite(HWT9073485.GetModbusId(), 0x01, 0x0000));
                //HWT9073485.SendProtocolData(new byte[] { 50, 06, 00, 1F, 00, 04, D5, 8B });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 设置带宽256Hz
        /// Set bandwidth of 256Hz
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bandWidth256_Click(object sender, EventArgs e)
        {
            if (HWT9073485.IsOpen() == false)
            {
                return;
            }
            try
            {
                // 解锁寄存器并发送命令   Unlock the register and send the command
                HWT9073485.UnlockReg();
                HWT9073485.SetBandWidth(0x00);
                // 下面两行与上面等价,推荐使用上面的    Equivalent to above
                //HWT9073485.SendProtocolData(Modbus16Utils.GetWrite(HWT9073485.GetModbusId(), 0x69, 0xB588));
                //HWT9073485.SendProtocolData(new byte[] { 50, 06, 00, 69, B5, 88, 22, A1 });
                //HWT9073485.SendProtocolData(Modbus16Utils.GetWrite(HWT9073485.GetModbusId(), 0x01, 0x0000));
                //HWT9073485.SendProtocolData(new byte[] { 50, 06, 00, 1F, 00, 00, D5, 8B });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 设置设备地址为50
        /// Set the device address to 50
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetAddrBtn_Click(object sender, EventArgs e)
        {
            if (HWT9073485.IsOpen() == false)
            {
                return;
            }
            try
            {
                // 解锁寄存器并发送命令   Unlock the register and send the command
                HWT9073485.UnlockReg();
                byte modbusId = byte.Parse(ModbustextBox.Text.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                HWT9073485.SetModbusId(modbusId);
                // 下面两行与上面等价,推荐使用上面的    Equivalent to above
                //HWT9073485.SendProtocolData(Modbus16Utils.GetWrite(HWT9073485.GetModbusId(), 0x69, 0xB588));
                //HWT9073485.SendProtocolData(new byte[] { 50, 06, 00, 69, B5, 88, 22, A1 });
                //HWT9073485.SendProtocolData(Modbus16Utils.GetWrite(HWT9073485.GetModbusId(), 0x1A, 0x0050));
                //HWT9073485.SendProtocolData(new byte[] { 50, 06, 00, 1A, 00, 50, A1, B0 });

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 开始磁场校准
        /// Start magnetic field calibration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startFieldCalibrationButton_Click(object sender, EventArgs e)
        {
            if (HWT9073485.IsOpen() == false)
            {
                return;
            }
            try
            {
                // 解锁寄存器并发送命令   Unlock the register and send the command
                HWT9073485.UnlockReg();
                HWT9073485.StartFieldCalibration();
                // 下面两行与上面等价,推荐使用上面的    Equivalent to above
                //HWT9073485.SendProtocolData(Modbus16Utils.GetWrite(HWT9073485.GetModbusId(), 0x69, 0xB588));
                //HWT9073485.SendProtocolData(new byte[] { 50, 06, 00, 69, B5, 88, 22, A1 });
                //HWT9073485.SendProtocolData(Modbus16Utils.GetWrite(HWT9073485.GetModbusId(), 0x01, 0x0007));
                //HWT9073485.SendProtocolData(new byte[] { 50, 06, 00, 01, 00, 07, 94, 49 });
                MessageBox.Show("开始磁场校准,请绕传感器XYZ三轴各转一圈,转完以后点击【结束磁场校准】");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 结束磁场校准
        /// End magnetic field calibration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void endFieldCalibrationButton_Click(object sender, EventArgs e)
        {
            if (HWT9073485.IsOpen() == false)
            {
                return;
            }
            try
            {
                // 解锁寄存器并发送命令   Unlock the register and send the command
                HWT9073485.UnlockReg();
                HWT9073485.EndFieldCalibration();
                // 下面两行与上面等价,推荐使用上面的    Equivalent to above
                //HWT9073485.SendProtocolData(Modbus16Utils.GetWrite(HWT9073485.GetModbusId(), 0x69, 0xB588));
                //HWT9073485.SendProtocolData(new byte[] { 50, 06, 00, 69, B5, 88, 22, A1 });
                //HWT9073485.SendProtocolData(Modbus16Utils.GetWrite(HWT9073485.GetModbusId(), 0x01, 0x0000));
                //HWT9073485.SendProtocolData(new byte[] { 50, 06, 00, 01, 00, 00, D5, 8B });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}

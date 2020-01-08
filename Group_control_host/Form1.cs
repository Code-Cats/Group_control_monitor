using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace Group_control_host
{

    /// <summary>
    /// 所有设备名称的枚举，相当于宏
    /// </summary>
    public enum ROBOT_Type
    {
        Robot1,
        Robot2,
        Robot3,
        Robot4,
        Robot5,
        Robot6,
        Robot7,
        Robot8,
        Robot9,
        Robot10,
        Robot11,
        Robot12,
        ROBOT_Type_num
    }

    /// <summary>
    /// 表示机器当前在线状态
    /// </summary>
    public enum ROBOT_State
    {
        Off_line,   //放在第一个是默认类型-不在线
        On_line_alive,      //在线自由状态
        On_line_controlled,   ////在线受控状态
        ROBOT_State_num
    }

    public struct ROBOT_Info  //结构体，机器ID及运动信息、受控状态综合 
    {
        /// <summary>
        /// 发送的x方向运动速度
        /// </summary>
        public short SendSpeedVx; //16bit
        /// <summary>
        /// 发送的y方向运动速度
        /// </summary>
        public short SendSpeedVy; //16bit
        /// <summary>
        /// 发送的w方向运动速度
        /// </summary>
        public short SendSpeedVw; //16bit
        /// <summary>
        /// 在线状态
        /// </summary>
        public ROBOT_State On_line_state;   //在线状态
    }

    public partial class Form1 : Form
    {

        /// <summary>
        /// 所有机器信息结构体数组
        /// </summary>
        static ROBOT_Info[] RobotInfo = new ROBOT_Info[(int)ROBOT_Type.ROBOT_Type_num]; //初始化12个机器信息结构体

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetControlEnabled(btnSendTestMsg, false);
            label_inform.Text = label_inform.Text.Split(':')[0] + ":COM4 disconnect";

        }

        /// <summary>
        /// 定义串口对象
        /// </summary>
        private SerialPort serial = new SerialPort();

        /// <summary>
        /// 打开串口
        /// </summary>
        /// <param name="strPortName">串口号</param>
        /// <param name="nRate">波特率</param>
        /// <param name="nDataBit">数据位</param>
        /// <param name="stopBits">停止位</param>
        /// /// <param name="nParity">校验位</param>
        /// <returns></returns>
        public bool OpenSerial(string strPortName, int nRate, int nDataBit, float nStopBits, int nParity)
        {
            //这里就是绑定串口接收回调事件，即发送一条串口命令，发送成功，则会触发此事件进入ReciceSerialData方法，我们就进行判断发送成功还是失败。
            //serial.DataReceived += new SerialDataReceivedEventHandler(ReciveSerialData);
            serial.PortName = strPortName;//串口号
            serial.BaudRate = nRate;//波特率
            float f = nStopBits;//停止位
            if (f == 0)
            {
                serial.StopBits = StopBits.None;
            }
            else if (f == 1.5)
            {
                serial.StopBits = StopBits.OnePointFive;
            }
            else if (f == 1)
            {
                serial.StopBits = StopBits.One;
            }
            else
            {
                serial.StopBits = StopBits.Two;
            }

            serial.DataBits = nDataBit;//数据位
            if (nParity == 0) //校验位
            {
                serial.Parity = Parity.None;
            }
            else if (nParity == 1)
            {
                serial.Parity = Parity.Odd;
            }
            else if (nParity == 2)
            {
                serial.Parity = Parity.Even;
            }
            else
            {
                serial.Parity = Parity.None;
            }

            serial.ReadTimeout = 3000;//设置超时读取时间
            serial.WriteTimeout = 500;//超时写入时间
            try
            {
                if (!serial.IsOpen)
                {
                    serial.Open();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return false;
            }

            return true;

        }

        /// <summary>
        /// 打开串口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOpenSerial_Click(object sender, EventArgs e)
        {
            if (!OpenSerial("COM4", 115200, 8, 1, 0))
            {
                //串口打开失败
                MessageBox.Show("串口打开失败！");
            }
            else
            {
                SetControlEnabled(btnSendTestMsg, true);
                label_inform.Text = label_inform.Text.Split(':')[0] + ":COM4 has connected";    //更新指示信息
                SetControlEnabled(btnOpenSerial, false);    //将打开串口按钮失能，以免程序错误
                btnMotionSwitch.Enabled = true; //使能运动控制开关
                btnMotionSwitch.Visible = true; //运动控制开关可见
            }
        }

        /// <summary>
        /// 关闭串口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCloseSerial_Click(object sender, EventArgs e)
        {
            try
            {
                if (serial.IsOpen)
                {
                    serial.Close();
                    SetControlEnabled(btnSendTestMsg, false);
                    label_inform.Text = label_inform.Text.Split(':')[0] + ":COM4 disconnect";
                    SetControlEnabled(btnOpenSerial, true); //使能串口打开按钮
                    btnMotionSwitch.Enabled = false; //失能运动控制开关
                    btnMotionSwitch.Visible = false; //运动控制开关不可见
                    motionControlState = false; //运动控制部分全部失能
                    pictureBox_Left.Enabled = false;
                    pictureBox_Left.Visible = false;
                    pictureBox_Right.Enabled = false;
                    pictureBox_Right.Visible = false;
                    pictureBox_Front.Enabled = false;
                    pictureBox_Front.Visible = false;
                    pictureBox_back.Enabled = false;
                    pictureBox_back.Visible = false;
                    pictureBox_Clockwise.Enabled = false;
                    pictureBox_Clockwise.Visible = false;
                    pictureBox_Anticlockwise.Enabled = false;
                    pictureBox_Anticlockwise.Visible = false;
                    labelMotionState.Text = labelMotionState.Text.Split(':')[0] + ":off";
                    timer_MsgSend.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// 发送测试数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSendTestMsg_Click(object sender, EventArgs e)
        {
            if (true)   //如果使用16进制发送
            {
                MatchCollection mc = Regex.Matches(textBoxSendData.Text.ToString(), @"\b[\da-fA-F]{2}");
                List<byte> buf = new List<byte>();
                foreach (Match m in mc)
                {
                    buf.Add(byte.Parse(m.Value, System.Globalization.NumberStyles.HexNumber));
                }

                serial.Write(buf.ToArray(), 0, buf.Count);
            }
        }



        /// <summary>
        /// 刷新按钮状态，重置为alive(ID1-4),当前无机器人状态反馈，故只能预设刷新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRefreshState_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 4; i++)
            {
                RobotInfo[i].On_line_state = ROBOT_State.On_line_alive;
            }
        }

        /// <summary>
        /// 测试按钮外观
        /// </summary>
        ROBOT_State test_button_state = new ROBOT_State();
        private void test_button_Click(object sender, EventArgs e)
        {
            test_button_state++;
            if (test_button_state == ROBOT_State.ROBOT_State_num) test_button_state = 0;
            test_button.Text = test_button_state.ToString();
            button_color_set(btnRobot1State, test_button_state);
            button_color_set(btnRobot2State, test_button_state);
            button_color_set(btnRobot3State, test_button_state);
            button_color_set(btnRobot4State, test_button_state);
            button_color_set(btnRobot5State, test_button_state);
            button_color_set(btnRobot6State, test_button_state);
            button_color_set(btnRobot7State, test_button_state);
            button_color_set(btnRobot8State, test_button_state);
            button_Enabled_Text_Set(btnRobot1State, test_button_state);
            button_Enabled_Text_Set(btnRobot2State, test_button_state);
        }


        private void button_color_set(System.Windows.Forms.Button button, ROBOT_State button_state)
        {
            //button.Name.Length//可以通过长度、字符串识别来识别传入的按钮属于什么684835
            switch (button_state)
            {
                case ROBOT_State.Off_line:
                    {
                        button.BackColor = Color.FromArgb(65, 65, 65); //灰色 disconnect
                        button.ForeColor = Color.FromArgb(140, 140, 140); //160 160 160
                        //button.Enabled = false;
                        //SetControlEnabled(button, false);
                        //button.Text = button.Text.Split('\n')[0]+ "\noff-line";//fuck this!线程中连按钮文本都不能改变？？？？服  了 
                        break;
                    }
                case ROBOT_State.On_line_alive:
                    {
                        button.BackColor = Color.FromArgb(2, 80, 120); //浅色alive 20 140 210
                        button.ForeColor = Color.FromArgb(160, 160, 160); //160 160 160 字体颜色//200 200 200
                        // button.Enabled = false;
                        //SetControlEnabled(button, false);//转移到定时器里了

                        //button.Text = button.Text.Split('\n')[0] + "\nhas_occupied";//fuck this!线程中连按钮文本都不能改变？？？？服  了

                        break;
                    }
                case ROBOT_State.On_line_controlled:
                    {
                        button.BackColor = Color.FromArgb(2, 131, 201);
                        button.ForeColor = Color.FromArgb(243, 249, 252);//46 58 132
                        // button.Enabled = false;
                        //SetControlEnabled(button, false); //转移到定时器里去了

                        //button.Text = button.Text.Split('\n')[0] + "\nconnect_OK";//fuck this!线程中连按钮文本都不能改变？？？？服  了
                        /////////////////////////button.Text = button.Text.Replace("click_connect", "off-line");
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        /// <summary>
        /// 微笑，这个函数存在的原因是网上魔改的buttonEnabled函数无法在线程中使用，只能在定时器定时刷新
        /// </summary>
        /// <param name="button"></param>
        /// <param name="button_state"></param>
        private void button_Enabled_Text_Set(System.Windows.Forms.Button button, ROBOT_State button_state)
        {
            switch (button_state)
            {
                case ROBOT_State.Off_line:
                    {
                        SetControlEnabled(button, false);
                        button.Text = button.Text.Split('\n')[0] + "\noff-line";
                        break;
                    }
                case ROBOT_State.On_line_alive:
                    {
                        SetControlEnabled(button, true);
                        button.Text = button.Text.Split('\n')[0] + "\nalive";
                        break;
                    }
                case ROBOT_State.On_line_controlled:
                    {
                        SetControlEnabled(button, true);
                        button.Text = button.Text.Split('\n')[0] + "\ncontroled";
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }


        /// <summary>
        /// 关闭程序，强制退出
        /// </summary>
        ///<param name="sender">
        ///<param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(0);
        }

        //////////////////////////////////////以下是为了防止按钮在失能时字体颜色发生改变,代替 button.Enabled = <bool>;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int wndproc);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        public const int GWL_STYLE = -16;
        public const int WS_DISABLED = 0x8000000;

        public void SetControlEnabled(Control c, bool enabled)  //该函数被线程调用会发生死循环
        {
            if (enabled)
            { SetWindowLong(c.Handle, GWL_STYLE, (~WS_DISABLED) & GetWindowLong(c.Handle, GWL_STYLE)); }
            else
            { SetWindowLong(c.Handle, GWL_STYLE, WS_DISABLED | GetWindowLong(c.Handle, GWL_STYLE)); }
        }//



        /// <summary>
        /// 按钮使能状态刷新函数：传入参数为对应按钮结构体，按钮在结构体中对应ID，该函数可以在值刷新时才调用
        /// </summary>
        private void Update_buttonsEnabled_andText(ROBOT_Info robotinfo, ROBOT_Type robot_index)
        {
            switch (robot_index)
            {
                case ROBOT_Type.Robot1:
                    {
                        button_Enabled_Text_Set(btnRobot1State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot2:
                    {
                        button_Enabled_Text_Set(btnRobot2State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot3:
                    {
                        button_Enabled_Text_Set(btnRobot3State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot4:
                    {
                        button_Enabled_Text_Set(btnRobot4State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot5:
                    {
                        button_Enabled_Text_Set(btnRobot5State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot6:
                    {
                        button_Enabled_Text_Set(btnRobot6State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot7:
                    {
                        button_Enabled_Text_Set(btnRobot7State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot8:
                    {
                        button_Enabled_Text_Set(btnRobot8State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot9:
                    {
                        button_Enabled_Text_Set(btnRobot9State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot10:
                    {
                        button_Enabled_Text_Set(btnRobot10State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot11:
                    {
                        button_Enabled_Text_Set(btnRobot11State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot12:
                    {
                        button_Enabled_Text_Set(btnRobot12State, robotinfo.On_line_state);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        /// <summary>
        /// 按钮状态外观刷新函数：传入参数为对应按钮结构体，按钮在结构体中对应ID
        /// </summary>
        void Update_buttons_color(ROBOT_Info robotinfo, ROBOT_Type robot_index)
        {
            switch (robot_index)
            {
                case ROBOT_Type.Robot1:
                    {
                        button_color_set(btnRobot1State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot2:
                    {
                        button_color_set(btnRobot2State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot3:
                    {
                        button_color_set(btnRobot3State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot4:
                    {
                        button_color_set(btnRobot4State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot5:
                    {
                        button_color_set(btnRobot5State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot6:
                    {
                        button_color_set(btnRobot6State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot7:
                    {
                        button_color_set(btnRobot7State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot8:
                    {
                        button_color_set(btnRobot8State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot9:
                    {
                        button_color_set(btnRobot9State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot10:
                    {
                        button_color_set(btnRobot10State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot11:
                    {
                        button_color_set(btnRobot11State, robotinfo.On_line_state);
                        break;
                    }
                case ROBOT_Type.Robot12:
                    {
                        button_color_set(btnRobot12State, robotinfo.On_line_state);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }


        private void timer_UI_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < 12; i++)
            {
                Update_buttons_color(RobotInfo[i], (ROBOT_Type)i);  //更新按钮外观
                Update_buttonsEnabled_andText(RobotInfo[i], (ROBOT_Type)i);//更新按钮文本与使能
            }
            labelVxShow.Text = labelVxShow.Text.Split(':')[0] + ":"+ commonVx.ToString();
            labelVyShow.Text = labelVyShow.Text.Split(':')[0] + ":" + commonVy.ToString();
            labelVwShow.Text = labelVwShow.Text.Split(':')[0] + ":" + commonVw.ToString();

        }

        private void btnRobot1State_Click(object sender, EventArgs e)
        {
            if (RobotInfo[0].On_line_state == ROBOT_State.On_line_alive)
            {
                RobotInfo[0].On_line_state = ROBOT_State.On_line_controlled;
            }
            else if (RobotInfo[0].On_line_state == ROBOT_State.On_line_controlled)
            {
                RobotInfo[0].On_line_state = ROBOT_State.On_line_alive;
            }
        }

        private void btnRobot2State_Click(object sender, EventArgs e)
        {
            if (RobotInfo[1].On_line_state == ROBOT_State.On_line_alive)
            {
                RobotInfo[1].On_line_state = ROBOT_State.On_line_controlled;
            }
            else if (RobotInfo[1].On_line_state == ROBOT_State.On_line_controlled)
            {
                RobotInfo[1].On_line_state = ROBOT_State.On_line_alive;
            }
        }

        private void btnRobot3State_Click(object sender, EventArgs e)
        {
            if (RobotInfo[2].On_line_state == ROBOT_State.On_line_alive)
            {
                RobotInfo[2].On_line_state = ROBOT_State.On_line_controlled;
            }
            else if (RobotInfo[2].On_line_state == ROBOT_State.On_line_controlled)
            {
                RobotInfo[2].On_line_state = ROBOT_State.On_line_alive;
            }
        }

        private void btnRobot4State_Click(object sender, EventArgs e)
        {
            if (RobotInfo[3].On_line_state == ROBOT_State.On_line_alive)
            {
                RobotInfo[3].On_line_state = ROBOT_State.On_line_controlled;
            }
            else if (RobotInfo[3].On_line_state == ROBOT_State.On_line_controlled)
            {
                RobotInfo[3].On_line_state = ROBOT_State.On_line_alive;
            }
        }

        private void btnRobot5State_Click(object sender, EventArgs e)
        {

        }

        private void btnRobot6State_Click(object sender, EventArgs e)
        {

        }

        private void btnRobot7State_Click(object sender, EventArgs e)
        {

        }

        private void btnRobot8State_Click(object sender, EventArgs e)
        {

        }

        private void btnRobot9State_Click(object sender, EventArgs e)
        {

        }

        private void btnRobot10State_Click(object sender, EventArgs e)
        {

        }

        private void btnRobot11State_Click(object sender, EventArgs e)
        {
            
        }

        private void btnRobot12State_Click(object sender, EventArgs e)
        {

        }

        private void btnTestDown_MouseDown(object sender, MouseEventArgs e)
        {
            label10.Text = label10.Text.Split(':')[0] + ":press";
        }

        private void btnTestDown_MouseUp(object sender, MouseEventArgs e)
        {
            label10.Text = label10.Text.Split(':')[0] + ":up";
        }

        public bool motionControlState = false;
        private void btnMotionSwitch_Click(object sender, EventArgs e)
        {
            motionControlState = !motionControlState;
            if(motionControlState)
            {
                pictureBox_Left.Enabled = true;
                pictureBox_Left.Visible = true;
                pictureBox_Right.Enabled = true;
                pictureBox_Right.Visible = true;
                pictureBox_Front.Enabled = true;
                pictureBox_Front.Visible = true;
                pictureBox_back.Enabled = true;
                pictureBox_back.Visible = true;
                pictureBox_Clockwise.Enabled = true;
                pictureBox_Clockwise.Visible = true;
                pictureBox_Anticlockwise.Enabled = true;
                pictureBox_Anticlockwise.Visible = true;
                labelMotionState.Text = labelMotionState.Text.Split(':')[0] + ":on";
                timer_MsgSend.Enabled = true;

            }
            else
            {
                pictureBox_Left.Enabled = false;
                pictureBox_Left.Visible = false;
                pictureBox_Right.Enabled = false;
                pictureBox_Right.Visible = false;
                pictureBox_Front.Enabled = false;
                pictureBox_Front.Visible = false;
                pictureBox_back.Enabled = false;
                pictureBox_back.Visible = false;
                pictureBox_Clockwise.Enabled = false;
                pictureBox_Clockwise.Visible = false;
                pictureBox_Anticlockwise.Enabled = false;
                pictureBox_Anticlockwise.Visible = false;
                labelMotionState.Text = labelMotionState.Text.Split(':')[0] + ":off";
                timer_MsgSend.Enabled = false;
            }
        }

        ROBOT_State msgSendRovotIndex = new ROBOT_State();  //每隔10ms发送一个机器对应的值
        Byte[] sendMsgArray = new Byte[9];
        private void timer_MsgSend_Tick(object sender, EventArgs e)
        {
            if (RobotInfo[(int)msgSendRovotIndex].On_line_state==ROBOT_State.On_line_controlled)
            {
                sendMsgArray[0] = 0x5A;
                sendMsgArray[1] = 0x00;
                sendMsgArray[2] = (Byte)msgSendRovotIndex;
                sendMsgArray[3] = (Byte)(commonVx >> 8);
                sendMsgArray[4] = (Byte)(commonVx);
                sendMsgArray[5] = (Byte)(commonVy >> 8);
                sendMsgArray[6] = (Byte)(commonVy);
                sendMsgArray[7] = (Byte)(commonVw >> 8);
                sendMsgArray[8] = (Byte)(commonVw);
                serial.Write(sendMsgArray, 0, 9);
            }
            

            msgSendRovotIndex++;
            if ((int)msgSendRovotIndex>3)   //只检索ID 0-3
            {
                msgSendRovotIndex = 0;
            }
        }

        /// <summary>
        /// 受控机器的公共Vx
        /// </summary>
        public short commonVx = 0;
        /// <summary>
        /// 受控机器的公共Vy
        /// </summary>
        public short commonVy = 0;
        /// <summary>
        /// 受控机器的公共Vw
        /// </summary>
        public short commonVw = 0;

        private void pictureBox_Front_MouseDown(object sender, MouseEventArgs e)
        {
            commonVx = 50;
        }

        private void pictureBox_Front_MouseUp(object sender, MouseEventArgs e)
        {
            commonVx = 0;
        }

        private void pictureBox_back_MouseDown(object sender, MouseEventArgs e)
        {
            commonVx = -50;
        }

        private void pictureBox_back_MouseUp(object sender, MouseEventArgs e)
        {
            commonVx = 0;
        }

        private void pictureBox_Left_MouseDown(object sender, MouseEventArgs e)
        {
            commonVy = 50;
        }

        private void pictureBox_Left_MouseUp(object sender, MouseEventArgs e)
        {
            commonVy = 0;
        }

        private void pictureBox_Right_MouseDown(object sender, MouseEventArgs e)
        {
            commonVy = -50;
        }

        private void pictureBox_Right_MouseUp(object sender, MouseEventArgs e)
        {
            commonVy = 0;
        }

        private void pictureBox_Clockwise_MouseDown(object sender, MouseEventArgs e)
        {
            commonVw = 50;
        }

        private void pictureBox_Clockwise_MouseUp(object sender, MouseEventArgs e)
        {
            commonVw = 0;
        }

        private void pictureBox_Anticlockwise_MouseDown(object sender, MouseEventArgs e)
        {
            commonVw = -50;
        }

        private void pictureBox_Anticlockwise_MouseUp(object sender, MouseEventArgs e)
        {
            commonVw = 0;
        }
    }//Form end
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace 侠之道存档修改器
{
    class AutoSizeFormClass
    {//控件的初始位置信息。
        public struct controlRect
        {
            public int Left;
            public int Top;
            public int Width;
            public int Height;
        }
        //存储控件名和他的位置
        public Dictionary<String, controlRect> oldCtrl = new Dictionary<String, controlRect>();
        int ctrlNo = 0;
        public bool isInit = false;

        //记录窗体和其控件的初始位置和大小,
        public void controllInitializeSize(Control mForm)
        {
            LogHelper.Debug("controllInitializeSize");
            controlRect cR;
            cR.Left = mForm.Left; cR.Top = mForm.Top; cR.Width = mForm.Width; cR.Height = mForm.Height;

            insertDictionary(mForm.Name, cR);


            AddControl(mForm);//窗体内其余控件还可能嵌套控件(比如panel),要单独抽出,因为要递归调用

            //this.WindowState = (System.Windows.Forms.FormWindowState)(2);//记录完控件的初始位置和大小后，再最大化
            //0 - Normalize , 1 - Minimize,2- Maximize
            isInit = true;
        }

        private void AddControl(Control ctl)
        {
            LogHelper.Debug("AddControl");
            foreach (Control c in ctl.Controls)
            {

                controlRect objCtrl;
                objCtrl.Left = c.Left; objCtrl.Top = c.Top; objCtrl.Width = c.Width; objCtrl.Height = c.Height;
                insertDictionary(c.Name, objCtrl);

                if (c.Controls.Count > 0)
                    AddControl(c);

            }
        }

        //(3.2)控件自适应大小,
        public void controlAutoSize(Control mForm)
        {

            LogHelper.Debug("controlAutoSize");
            if (ctrlNo == 0)
            {

                AddControl(mForm);//窗体内其余控件可能嵌套其它控件(比如panel),故单独抽出以便递归调用
            }
            float wScale = (float)mForm.Width / oldCtrl[mForm.Name].Width; ;//新旧窗体之间的比例，与最早的旧窗体
            float hScale = (float)mForm.Height / oldCtrl[mForm.Name].Height; ;//.Height;

            ctrlNo = 1;//进入=1，第0个为窗体本身,窗体内的控件,从序号1开始


            AutoScaleControl(mForm, wScale, hScale);//窗体内其余控件还可能嵌套控件(比如panel),要单独抽出,因为要递归调用
        }

        private void AutoScaleControl(Control ctl, float wScale, float hScale)
        {
            LogHelper.Debug("AutoScaleControl");
            int ctrLeft0, ctrTop0, ctrWidth0, ctrHeight0;
            //int ctrlNo = 1;//第1个是窗体自身的 Left,Top,Width,Height，所以窗体控件从ctrlNo=1开始
            foreach (Control c in ctl.Controls)
            { //**放在这里，是先缩放控件的子控件，后缩放控件本身
              //if (c.Controls.Count > 0)
              //   AutoScaleControl(c, wScale, hScale);//窗体内其余控件还可能嵌套控件(比如panel),要单独抽出,因为要递归调用
                ctrLeft0 = oldCtrl[c.Name].Left;
                ctrTop0 = oldCtrl[c.Name].Top;
                ctrWidth0 = oldCtrl[c.Name].Width;
                ctrHeight0 = oldCtrl[c.Name].Height;
                //c.Left = (int)((ctrLeft0 - wLeft0) * wScale) + wLeft1;//新旧控件之间的线性比例
                //c.Top = (int)((ctrTop0 - wTop0) * h) + wTop1;
                c.Left = (int)((ctrLeft0) * wScale);//新旧控件之间的线性比例。控件位置只相对于窗体，所以不能加 + wLeft1
                c.Top = (int)((ctrTop0) * hScale);//
                c.Width = (int)(ctrWidth0 * wScale);//只与最初的大小相关，所以不能与现在的宽度相乘 (int)(c.Width * w);
                c.Height = (int)(ctrHeight0 * hScale);//
                //AutoScaleFont(c);
                ctrlNo++;//累加序号
                         //**放在这里，是先缩放控件本身，后缩放控件的子控件
                if (c.Controls.Count > 0)
                    AutoScaleControl(c, wScale, hScale);//窗体内其余控件还可能嵌套控件(比如panel),要单独抽出,因为要递归调用

            }
        }
        private void AutoScaleFont(Control c)
        {
            LogHelper.Debug("AutoScaleFont");
            string[] type = c.GetType().ToString().Split('.');
            string controlType = type[type.Length - 1];

            switch (controlType)
            {
                //case "Button":
                //    c.Font = new System.Drawing.Font("宋体", c.Height * 0.4f);
                //    break;

                case "GroupBox":
                    c.Font = new System.Drawing.Font("宋体", c.Height * 0.04f);
                    break;
            }
        }




        private void insertDictionary(String name, controlRect cr)   //添加控件名和位置，如果名称重复则更新
        {
            LogHelper.Debug("insertDictionary:"+ name);
            Dictionary<String, controlRect> temp = new Dictionary<String, controlRect>();
            bool flag = false;
            foreach (var pair in oldCtrl)
            {
                if (pair.Key.ToString() == name)
                {
                    temp.Add(name, cr);
                    flag = true;
                }
            }
            if (flag == false)
            {
                oldCtrl.Add(name, cr);
            }
            foreach (var value in temp)
            {
                oldCtrl.Remove(value.Key.ToString());
                oldCtrl.Add(value.Key, value.Value);
            }
            temp.Clear();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace KonkukCommunicationDesign
{
    public partial class CeilingWindow
    {
        SettingWindow settingWindow;

        public void shortCutDispatch(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.S :
                    SettingWindow window = new SettingWindow(this, wallDisplayWindow);
                    window.ShowDialog();
                    break;
            }
        }
    }
}

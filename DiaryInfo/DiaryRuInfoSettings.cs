using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiaryInfo
{
    class DiaryInfoSettings : ApplicationSettingsBase
    {
        [UserScopedSettingAttribute()]
        [DefaultSettingValueAttribute("2000")]
        public int BaloonShowDelay
        {
            get { return (int)(this["BaloonShowDelay"]); }
            set { if(value > 0) this["BaloonShowDelay"] = value; }
        }

        [UserScopedSettingAttribute()]
        [DefaultSettingValueAttribute("300000")]
        public int TimerForRequest
        {
            get { return (int)(this["TimerForRequest"]); }
            set { if (value > 0) this["TimerForRequest"] = value; }
        }

        [UserScopedSettingAttribute()]
        [DefaultSettingValueAttribute("5000")]
        public int TimeoutForWebRequest
        {
            get { return (int)(this["TimeoutForWebRequest"]); }
            set { if (value > 0) this["TimeoutForWebRequest"] = value; }
        }

        [UserScopedSettingAttribute()]
        [DefaultSettingValueAttribute("true")]
        public bool SaveCookiesToDisk
        {
            get { return (bool)(this["SaveCookiesToDisk"]); }
            set { this["SaveCookiesToDisk"] = value; }
        }
    }
}

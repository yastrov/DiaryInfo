using System;
using System.Collections.Generic;
using System.Configuration;
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
        [DefaultSettingValueAttribute("00:05:00")]
        public TimeSpan TimerForRequest
        {
            get { return (TimeSpan)this["TimerForRequest"]; }
            set { if (value != TimeSpan.Zero) this["TimerForRequest"] = value; }
        }

        [UserScopedSettingAttribute()]
        [DefaultSettingValueAttribute("5000")]
        public int TimeoutForWebRequest
        {
            get { return (int)(this["TimeoutForWebRequest"]); }
            set { if (value > 0) this["TimeoutForWebRequest"] = value; }
        }

        [UserScopedSettingAttribute()]
        [DefaultSettingValueAttribute("2000")]
        public int TimeoutForTrayIconBaloon
        {
            get { return (int)(this["TimeoutForTrayIconBaloon"]); }
            set { if (value >= 0) this["TimeoutForTrayIconBaloon"] = value; }
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiaryInfo
{
    public class TimeSpanViewModel: IComparable<TimeSpanViewModel>, INotifyPropertyChanged
    {
        public string Description { get; set; }
        public TimeSpan Interval { get; set; }
        public TimeSpanViewModel(string description, TimeSpan time)
        {
            this.Interval = time;
            this.Description = description;
        }

        public int CompareTo(TimeSpanViewModel other)
        {
            return this.Interval.CompareTo(other.Interval);
        }
        public int CompareTo(TimeSpan other)
        {
            return this.Interval.CompareTo(other);
        }
        public override string ToString()
        {
            return this.Interval.ToString();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}

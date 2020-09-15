using System.Windows;
using System.Collections.Generic;
using System.ComponentModel;
using Energistics.Etp.v11.Datatypes.ChannelData;

namespace ShellSquare.ETP.Simulator
{
    public class ChannelItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Eml { get; set; }
        public string Uid { get; set; }

        private bool m_Selected;
        public bool Selected
        {
            get
            {
                return m_Selected;
            }
            set
            {
                m_Selected = value;
                RaisePropertyChanged("Selected");
            }
        }

        public string DisplayName
        {
            get
            {
                return $"{Name}";
            }
        }

        public string DataType { get; set; }
        public bool HasDepthIndex { get; set; }
        public bool HasTimeIndex { get; set; }

        public ChannelMetadataRecord ChannelMetadataRecord { get; set; }





        public List<ChannelItem> ChannelItems { get; private set; }

        public int Level { get; set; }

        public int ChildrensCount { get; set; }
        public bool HasChildren
        {
            get
            {
                if (ChildrensCount > 0 || ChildrensCount == -1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }



        public bool ChildrensLoaded { get; set; }
      
   
        public string Space
        {
            get
            {
                string space = "";
                for (int i = 0; i < Level; i++)
                {
                    space = space + "   ";
                }
                return space;
            }
        }
     


    }
}

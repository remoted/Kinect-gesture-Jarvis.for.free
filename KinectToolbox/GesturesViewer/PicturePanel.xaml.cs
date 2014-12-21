using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GesturesViewer
{
    /// <summary>
    /// PicturePanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PicturePanel : BasePanel
    {

        BitmapImage _image = null;

        public bool CreateImage()
        {
            Image = new BitmapImage(new Uri(FullName));
            if (Image != null)
                return true;

            return false;
        }

        public BitmapImage Image
        {
            get { return _image; }
            set
            {
                _image = value;
                OnPropertyChanged("Image");
            }
        }

        public PicturePanel()
        {
            InitializeComponent();
            this.DataContext = this;
        }
    }
}

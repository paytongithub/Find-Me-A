using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Find_Me_A.Views
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {
        public HomePage()
        {
            InitializeComponent();
            //Image paths need to be inserted
            FeaturedItems.ItemsSource = new string[]
            {

            };
            TopPicksItems.ItemsSource = new string[]
            {

            };
            TrendingItems.ItemsSource = new string[]
            {

            };


        }
    }
}

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
using FindMeA_.Views;

namespace FindMeA_
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new WelcomePage());
        }

        private void Home_Click(object sender, RoutedEventArgs e)
            => MainFrame.Navigate(new HomePage());
        private void Search_Click(object sender, RoutedEventArgs e)
            => MainFrame.Navigate(new SearchPage());
        private void Watchlist_Click(object sender, RoutedEventArgs e)
            => MainFrame.Navigate(new WatchlistPage());

        private void Reviews_Click(object sender, RoutedEventArgs e)
            => MainFrame.Navigate(new ReviewsPage());

        private void Profile_Click(object sender, RoutedEventArgs e)
             => MainFrame.Navigate(new ProfilePage());
    }
}

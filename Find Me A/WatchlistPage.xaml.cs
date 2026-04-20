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
using System.Windows.Shapes;
using Find_Me_A;

namespace FindMeA_.Views
{
    /// <summary>
    /// Interaction logic for WatchlistPage.xaml
    /// </summary>

    public partial class WatchlistPage : Page
    {
        public WatchlistPage()
        {
            InitializeComponent();

            // Your connection string
            string conn = "Server=YOURSERVER;Database=YOURDB;Trusted_Connection=True;";
            _watchList = new WatchList(conn);

            LoadWatchlist();
        }

        private void LoadWatchlist()
        {
            string username = "Name"; // Replace with logged-in user

            var items = _watchList.GetWatchedList(username);

            WatchlistPanel.Children.Clear();

            foreach (var item in items)
            {
                var stack = new StackPanel { Margin = new Thickness(10) };

                var title = new TextBlock
                {
                    Text = item.Title.TitleName,
                    Foreground = Brushes.White,
                    FontSize = 18
                };

                var meta = new TextBlock
                {
                    Text = $"{item.Title.TitleType} • {item.Title.ReleaseDate.Year}",
                    Foreground = Brushes.Gray
                };

                var rating = new TextBlock
                {
                    Text = item.UserRating.HasValue ? $"Your Rating: {item.UserRating}/10" : "Not rated",
                    Foreground = Brushes.Gold
                };

                stack.Children.Add(title);
                stack.Children.Add(meta);
                stack.Children.Add(rating);

                WatchlistPanel.Children.Add(stack);
            }
        }
    }
}

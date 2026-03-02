using DnDBattle.Models;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Editors;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;
using DnDBattle.Views.TileMap;
using DnDBattle.Services.TileService;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Views.Creatures
{
    /// <summary>
    /// Interaction logic for TokenEditorView.xaml
    /// </summary>
    public partial class TokenEditorView : UserControl
    {
        public TokenEditorView()
        {
            InitializeComponent();
        }

        public Token Token
        {
            get => (Token)DataContext;
            set => DataContext = value;
        }

        private async void UploadImage_Click(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is Token token)) return;
            var dlg = new OpenFileDialog { Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp" };
            if (dlg.ShowDialog() == true)
            {
                string oldFilePath = dlg.FileName;
                string newFilePath = 
                    System.IO.Path.Combine(Options.DefaultTokenImagePath, 
                    Token.Name + "_" + Token.Id.ToString().Substring(0, 8) + System.IO.Path.GetExtension(oldFilePath));
                try
                {
                    File.Move(oldFilePath, newFilePath, true);

                    token.Image = new BitmapImage(new Uri(newFilePath));
                    token.IconPath = newFilePath;

                    using (var db = new CreatureDatabaseService())
                    {
                        await db.UpdateCreatureAsync(Token);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load image: {ex.Message}");
                }
            }
        }

        private void CenterToken_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is Token token)
            {
                token.GridX = 0;
                token.GridY = 0;
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = sender as TextBox;
                textBox?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                e.Handled = true;
            }
        }
    }
}

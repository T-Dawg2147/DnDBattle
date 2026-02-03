using DnDBattle.Models.Tiles;
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

namespace DnDBattle.Views.Editors
{
    /// <summary>
    /// Interaction logic for SecretEditorDialog.xaml
    /// </summary>
    public partial class SecretEditorDialog : Window
    {
        public SecretMetadata Secret { get; private set; }

        public SecretEditorDialog(SecretMetadata existing = null)
        {
            InitializeComponent();
            Secret = existing ?? new SecretMetadata();

            if (existing != null)
            {
                LoadData();
            }
        }

        private void LoadData()
        {
            TxtName.Text = Secret.Name;
            CmbSecretType.SelectedIndex = (int)Secret.SecretKind;
            TxtDC.Text = Secret.InvestigationDC.ToString();
            TxtDiscovery.Text = Secret.DiscoveryDescription;
            ChkRequiresActivation.IsChecked = Secret.RequiresActivation;
            TxtActivation.Text = Secret.ActivationDescription;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Secret.Name = TxtName.Text;
            Secret.SecretKind = (SecretType)CmbSecretType.SelectedIndex;
            Secret.InvestigationDC = int.TryParse(TxtDC.Text, out int dc) ? dc : 15;
            Secret.DiscoveryDescription = TxtDiscovery.Text;
            Secret.RequiresActivation = ChkRequiresActivation.IsChecked ?? false;
            Secret.ActivationDescription = TxtActivation.Text;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

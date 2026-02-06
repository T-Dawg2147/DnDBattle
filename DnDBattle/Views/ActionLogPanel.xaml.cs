using System.Collections.ObjectModel;
using System.Windows.Controls;
using DnDBattle.Models;

namespace DnDBattle.Views
{
    /// <summary>
    /// Interaction logic for ActionLogPanel.xaml
    /// </summary>
    public partial class ActionLogPanel : UserControl
    {
        public ActionLogPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Bind the action log collection to the list.
        /// </summary>
        public void SetActionLog(ObservableCollection<ActionLogEntry> actionLog)
        {
            ActionLogList.ItemsSource = actionLog;
        }
    }
}

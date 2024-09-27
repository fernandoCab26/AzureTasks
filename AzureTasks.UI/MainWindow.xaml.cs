using AzureTasks.UI.Presenter;
using AzureTasks.UI.Views;
using Models;
using System.Windows;

namespace AzureTasks.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IMainView
    {

        private MainViewPresenter _presenter;
        public MainWindow()
        {
            InitializeComponent();
            _presenter = new MainViewPresenter(this);
            _presenter.ShowMessageHandler += ShowMessageHandler;
        }

        private void ShowMessageHandler(object? sender, string e)
        {
            MessageBox.Show(e);
        }

        public string ItemType
        {
            get { return txtType.Text; }
            set { txtType.Text = value; }
        }
        public string Id { get => txtId.Text; set => txtId.Text = value; }
        public string Area { get => txtArea.Text; set => txtArea.Text = value; }
        public string Iteration { get => txtPath.Text; set => txtPath.Text = value; }
        public string ProjectProcess { get => txtProcess.Text; set => txtProcess.Text = value; }
        public string Size { get => "0"; set => throw new NotImplementedException(); }
        public string DevepmentSize { get => txtDevelopTime.Text; set => txtDevelopTime.Text = value; }
        public string TestingSize { get => txtTestingTime.Text; set => txtTestingTime.Text = value; }
        public int ComponentsNumber { get => 0; set => throw new NotImplementedException(); }
        public string Organization { get => txtOrganization.Text; set => txtOrganization.Text = value; }
        public string Pat { get => txtPat.Text; set => txtPat.Text = value; }
        public string Project { get => txtProject.Text; set => txtProject.Text = value; }
        public string AssignedTo { get => (string)cmbDevTeam.SelectedValue; }
        public string Reviewer { get => (string)cmbReviewers.SelectedValue; }
        public string ItemName { get => txtName.Text; set => txtName.Text = value; }

        public event EventHandler GenerateTaskEvent;
        public event EventHandler ImportTaskHandler;
        public event EventHandler ClearTasksHandler;
        public event EventHandler AddComponentHandler;
        public event EventHandler SearchItemHandler;

        public void BindingProcessTasks(List<DevelopmentTask> processTasks)
        {
            this.dgProcessTasks.ItemsSource = processTasks;
        }

        public void BindingGeneratedTasks(List<AzureTask> azureTasks)
        {
            this.dgGeneratedTask.ItemsSource = null;
            this.dgGeneratedTask.ItemsSource = azureTasks;
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            GenerateTaskEvent?.Invoke(this, EventArgs.Empty);
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            string messageBoxText = "¿Deseas relizar la importación a Azure DevOps?";
            string caption = "Importar a Azure DevOps";
            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Question;
            MessageBoxResult result;
            result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
            if (result == MessageBoxResult.Yes)
            {
                ImportTaskHandler?.Invoke(this, EventArgs.Empty);
            }

        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            string messageBoxText = "¿Deseas limpiar las tareas?";
            string caption = "Limpiar";
            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Question;
            MessageBoxResult result;
            result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
            if (result == MessageBoxResult.Yes)
            {
                ClearTasksHandler?.Invoke(this, EventArgs.Empty);
            }
        }

        public void BindingDevTeam(List<TeamMember> teamMembers)
        {
            cmbDevTeam.ItemsSource = teamMembers;
            cmbDevTeam.DisplayMemberPath = "User";
            cmbDevTeam.SelectedValuePath = "User";
            cmbDevTeam.SelectedIndex = 0;

            cmbReviewers.ItemsSource = teamMembers;
            cmbReviewers.DisplayMemberPath = "User";
            cmbReviewers.SelectedValuePath = "User";
            cmbReviewers.SelectedIndex = 0;
        }

        public void BidingComponents(List<TaskComponent> components)
        {
            dgComponents.ItemsSource = null;
            dgComponents.ItemsSource = components;
        }

        private void dgComponents_InitializingNewItem(object sender, System.Windows.Controls.InitializingNewItemEventArgs e)
        {
            AddComponentHandler?.Invoke(this, e);
        }

        private void btbGetItem_Click(object sender, RoutedEventArgs e)
        {
            SearchItemHandler?.Invoke(this, e);
        }
    }
}
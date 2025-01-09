using Mayansoft.AzureConnect.Models;
using Mayansoft.AzureConnect.UI.Presenter;
using Mayansoft.AzureConnect.UI.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace Mayansoft.AzureConnect.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IMainView
    {

        private readonly MainViewPresenter _presenter;
        private bool isFirsLoad = true;
        private string _organizationPrev;
        public MainWindow()
        {
            InitializeComponent();
            _presenter = new MainViewPresenter(this);
            _presenter.ShowMessageHandler += ShowMessageHandler;
            cmbOrganizations.DropDownOpened += CmbProjects_DropDownOpened;
            IsLoading = true;
        }

        private void CmbProjects_DropDownOpened(object sender, EventArgs e)
        {
            _organizationPrev = cmbOrganizations.SelectedValue.ToString();
        }

        private void ShowMessageHandler(object sender, string e)
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
        public string DevepmentSize { get => (string)lblDevelopmentTime.Content; set => lblDevelopmentTime.Content = value; }
        public string TestingSize { get => (string)lblTestingTime.Content; set => lblTestingTime.Content = value; }
        public string Organization { get => cmbOrganizations.SelectedValue.ToString(); }
        public string Project
        {
            get
            {
                if (cmbProjects.SelectedValue == null)
                {
                    return string.Empty;
                }
                else
                {
                    return (string)cmbProjects.SelectedValue;
                }
            }
        }
        public string AssignedTo { get => (string)cmbDevTeam.SelectedValue; }
        public string Reviewer { get => (string)cmbReviewers.SelectedValue; }
        public string ItemName { get => txtName.Text; set => txtName.Text = value; }

        public string TestingAssignedTo => (string)cmbDevTeamTest.SelectedValue;

        public string TestingReviewer => (string)cmbReviewersTest.SelectedValue;

        public string OthersTasksAssignedTo => (string)cmbDevTeamOthers.SelectedValue;

        public string OtherTime { get => (string)lblOtherTime.Content; set => lblOtherTime.Content = value; }
        public string TotalTime { get => (string)lblTotalTime.Content; set => lblTotalTime.Content = value; }
        public bool IsLoading { get => progressBar.Visibility == Visibility.Visible; set => progressBar.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        public event EventHandler GenerateTaskEvent;
        public event EventHandler ImportTaskHandler;
        public event EventHandler ClearTasksHandler;
        public event EventHandler AddComponentHandler;
        public event EventHandler SearchItemHandler;
        public event EventHandler OnchangeOrganization;
        public event EventHandler OnchangeProject;
        public event PropertyChangedEventHandler PropertyChanged;

        public void BindingProcessTasks(List<DevelopmentTask> processTasks, List<NormalTask> testingTasks, List<NormalTask> otherTasks)
        {
            this.dgProcessTasks.ItemsSource = processTasks;
            this.dgTestingTask.ItemsSource = testingTasks;
            dgOtherTasks.ItemsSource = otherTasks;
        }

        public void BindingGeneratedTasks(List<AzureTask> azureTasks)
        {
            this.dgGeneratedTask.ItemsSource = null;
            this.dgGeneratedTask.ItemsSource = azureTasks;
            dgTestingTaskGenerated.ItemsSource = null;
            dgTestingTaskGenerated.ItemsSource = azureTasks;
            dgOtherTasksGenerated.ItemsSource = null;
            dgOtherTasksGenerated.ItemsSource = azureTasks;
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

            cmbDevTeamTest.ItemsSource = teamMembers;
            cmbDevTeamTest.DisplayMemberPath = "User";
            cmbDevTeamTest.SelectedValuePath = "User";
            cmbDevTeamTest.SelectedIndex = 0;

            cmbReviewersTest.ItemsSource = teamMembers;
            cmbReviewersTest.DisplayMemberPath = "User";
            cmbReviewersTest.SelectedValuePath = "User";
            cmbReviewersTest.SelectedIndex = 0;

            cmbDevTeamOthers.ItemsSource = teamMembers;
            cmbDevTeamOthers.DisplayMemberPath = "User";
            cmbDevTeamOthers.SelectedValuePath = "User";
            cmbDevTeamOthers.SelectedIndex = 0;
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
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public void BindingProjects(List<ListItem<string>> projects, string selectedValue)
        {
            cmbProjects.ItemsSource = projects;
            cmbProjects.DisplayMemberPath = "Name";
            cmbProjects.SelectedValuePath = "Value";
            cmbProjects.SelectedValue = selectedValue;
        }

        public void BindingOrganization(List<Organization> organizations, string selectedValue)
        {
            cmbOrganizations.ItemsSource = organizations;
            cmbOrganizations.DisplayMemberPath = "Name";
            cmbOrganizations.SelectedValuePath = "Value";
            cmbOrganizations.SelectedValue = selectedValue;
        }

        #region Events
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
        private void cmbOrganizations_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (isFirsLoad || cmbOrganizations.SelectedValue?.ToString() == _organizationPrev)
            {
                isFirsLoad = false;
                return;
            }
            string messageBoxText = "¿Deseas cambiar de organización?";
            string caption = "La información capturada se perderá";
            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Question;
            MessageBoxResult result;
            result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
            if (result == MessageBoxResult.Yes)
            {
                OnchangeOrganization?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                cmbOrganizations.SelectedValue = _organizationPrev;
            }
        }


        #endregion

        private void cmbProjects_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            OnchangeProject?.Invoke(this, EventArgs.Empty);
        }
    }
}
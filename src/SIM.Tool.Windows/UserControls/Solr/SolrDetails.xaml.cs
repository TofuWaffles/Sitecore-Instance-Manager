namespace SIM.Tool.Windows.UserControls.Install
{
  using System;
  using System.IO;
  using System.Linq;
  using System.Windows;
  using System.Windows.Controls;
  using SIM.Tool.Base;
  using SIM.Tool.Base.Pipelines;
  using SIM.Tool.Base.Wizards;
  using JetBrains.Annotations;
  using SIM.Sitecore9Installer;
  using SIM.Sitecore9Installer.Configuration;
  using SIM.Sitecore9Installer.Tasks;
  using SIM.Sitecore9Installer.Security;

  [UsedImplicitly]
  public partial class SolrDetails : IWizardStep, IFlowControl
  {
    /// <summary>
    /// Task for Solr installs for versions <9
    /// </summary>
    private Tasker tasker;

    /// <summary>
    /// Task for Solr installs for versions 9+
    /// </summary>
    private Tasker tasker9;
    #region Constructors

    public SolrDetails()
    {
      InitializeComponent();

    }

    #endregion

    #region Public Methods

    public bool OnMovingBack(WizardArgs wizardArg)
    {
      return true;
    }

    public bool OnMovingNext(WizardArgs wizardArgs)
    {
      Tasker solrTasker = tasker;
      string solrVersion = solrVersionSelector.SelectedValue as string;
      if (Version.TryParse(solrVersion, out var v) && v.Major >= 9)
      {
        solrTasker = tasker9;
        Task solrInstallTask = solrTasker.Tasks.Single(t => t.Name == "Solr");
        solrInstallTask.LocalParams.AddOrUpdateParam("SolrAdminUserName", "admin", InstallParamType.String);
        solrInstallTask.LocalParams.AddOrUpdateParam("SolrAdminPassword", SolrPasswordGenerator.Generate(), InstallParamType.String);
      }
      solrTasker.GlobalParams.First(p => p.Name == "SolrVersion").Value = solrVersion;
      solrTasker.GlobalParams.First(p => p.Name == "SolrDomain").Value = this.solrHostSelector.Text;

      if (!string.IsNullOrEmpty(this.solrPortSelector.Text))
      {
        solrTasker.GlobalParams.First(p => p.Name == "SolrPort").Value = this.solrPortSelector.Text;
      }

      if (!string.IsNullOrEmpty(this.solrFolderSelector.Text))
      {
        solrTasker.GlobalParams.First(p => p.Name == "SolrInstallRoot").Value = this.solrFolderSelector.Text;
      }

      string javaHomeVariable = Environment.GetEnvironmentVariable("JAVA_HOME");
      if (string.IsNullOrEmpty(javaHomeVariable))
      {
        MessageBox.Show("Required JAVA_HOME system variable was not found.\nIt seems Java Runtime Environment (JRE) has not been installed.", "Warning");
        return false;
      }
      else if (!Directory.Exists(Path.Combine(javaHomeVariable, "bin")) || Directory.GetFiles(Path.Combine(javaHomeVariable, "bin"), "java.exe").Length == 0)
      {
        MessageBox.Show($"The 'JAVA_HOME' system variable does not point to the proper JRE folder: {javaHomeVariable}", "Warning");
        return false;
      }

      solrTasker.GlobalParams.First(p => p.Name == "JavaHome").Value = javaHomeVariable;
      Install9WizardArgs args = (Install9WizardArgs)wizardArgs;
      args.Tasker = solrTasker;
      return true;
    }
    #endregion

    #region Private Methods
    private void SolrVersionSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      string solrVersion = this.solrVersionSelector.SelectedValue as string;
      this.solrHostSelector.Text = "solr" + solrVersion.Replace(".", string.Empty);
    }

    private void LocationBtn_Click(object sender, RoutedEventArgs e)
    {
      WindowHelper.PickFolder("Choose location folder", this.solrFolderSelector, null);
    }

    private void FillSolrVersions()
    {
      this.solrVersionSelector.DataContext = Configuration.Instance.SolrMap.Keys;
    }
    #endregion
    #region IWizardStep Members

    void IWizardStep.InitializeStep(WizardArgs wizardArgs)
    {
      string root = Path.Combine(Directory.GetCurrentDirectory(), "CustomSifConfig/Solr");
      this.tasker = new Tasker(root, "solr", null);

      // TODO: Solr 9+ installations currently have untrusted SSL certs
      this.tasker9 = new Tasker(root, "solr9", null);
      this.FillSolrVersions();
      this.solrPortSelector.Text = tasker.Tasks.First(t => t.Name == "Solr").LocalParams.First(p => p.Name == "SolrPort").Value;
      this.solrFolderSelector.Text = tasker.Tasks.First(t => t.Name == "Solr").LocalParams.First(p => p.Name == "SolrInstallRoot").Value;
    }

    bool IWizardStep.SaveChanges(WizardArgs wizardArgs)
    {
      return true;
    }

    #endregion

  }
}

using System;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Nomad.Commons.Plugin;
using Nomad.Shared;

namespace Nomad.Plugin.CmdLinePlugin
{
  [Guid("2C3041DD-BA02-408D-A020-09BC7CFBB9B1")]
  [ExportExtension(typeof(IRunOnce), Configure = true)]
  public class CommandLinePlugin : IRunOnce
  {
    private static int _MaxCommandLength = 4096;
    private static int _HistoryDepth = 15;

    private static void AddCommandLineToTab(ITab tab)
    {
      IFolderView FolderView = tab as IFolderView;
      if (FolderView != null)
      {
        CommandLinePanel CmdLinePanel = new CommandLinePanel();
        CmdLinePanel.MaxCommandLength = _MaxCommandLength;
        CmdLinePanel.HistoryDepth = _HistoryDepth;
        tab.DockControl(DockStyle.Bottom, CmdLinePanel);
        FolderView.CurrentFolderChanged += (sender2, e2) => CmdLinePanel.CurrentFolder = FolderView.CurrentFolder;
      }
    }

    public void Execute(IServiceProvider serviceProvider)
    {
      var TabManager = (ITabManager)serviceProvider.GetService(typeof(ITabManager));
      foreach (ITab NextTab in TabManager)
        AddCommandLineToTab(NextTab);
      TabManager.Added += (sender, e) => AddCommandLineToTab(e.Tab);
    }

    [ConfigurationProperty("maxCommandLength", DefaultValue = 4096)]
    public int MaxCommandLength 
    {
      get { return _MaxCommandLength; }
      set { _MaxCommandLength = value; }
    }

    [ConfigurationProperty("historyDepth", DefaultValue = 15)]
    [IntegerValidator(MinValue = 0)]
    public int HistoryDepth
    {
      get { return _HistoryDepth; }
      set { _HistoryDepth = value; }
    }
  }
}
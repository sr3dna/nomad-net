using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Nomad.Commons.Plugin;
using Nomad.Shared;

namespace Nomad.Plugin
{
  [Guid("2C3041DD-BA02-408D-A020-09BC7CFBB9B1")]
  [ExportExtension(typeof(IRunOnce))]
  public class CommandLinePlugin : IRunOnce
  {
    private static void AddCommandLineToTab(ITab tab)
    {
      IFolderView FolderView = tab as IFolderView;
      if (FolderView != null)
      {
        CommandLinePanel CmdLinePanel = new CommandLinePanel();
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
  }
}
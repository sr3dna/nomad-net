using System.Collections.Generic;
using System.ComponentModel;
using Nomad.Commons.Plugin;
using Nomad.Shared;

namespace Nomad.Plugin.CmdLinePlugin
{
  [ExtensionDependency(typeof(CommandLinePlugin))]
  public abstract class CmdLineCommandBase : Component
  {
    protected ITab CurrentTab
    {
      get { return ((ITabManager)GetService(typeof(ITabManager))).SelectedTab; }
    }

    protected CommandLinePanel CurrentCommandLine
    {
      get { return CurrentTab.GetDockedControls().OfType<CommandLinePanel>().FirstOrDefault(); }
    }
  }
}
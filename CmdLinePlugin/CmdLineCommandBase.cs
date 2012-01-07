using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using Nomad.Shared;

namespace Nomad.Plugin
{
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

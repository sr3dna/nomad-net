using System;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Nomad.Commons.Plugin;
using Nomad.Plugin.CmdPrompt.Properties;

namespace Nomad.Plugin.CmdPrompt
{
  [Guid("21211CA6-8FDD-4F0D-9882-FED6CD232436")]
  [DisplayName("Command Prompt")]
  [Description("Command Prompt Colors")]
  [ToolboxBitmap(typeof(NewCmdPromptTabCommand), "application_xp_terminal.png")]
  [ExportExtension(typeof(IPersistComponentSettings))]
  public partial class CmdPromptOptionControl : UserControl, IPersistComponentSettings
  {
    public CmdPromptOptionControl()
    {
      InitializeComponent();
    }

    private void cmbColor_DrawItem(object sender, DrawItemEventArgs e)
    {
      if (e.BackColor != Color.Empty)
        e.DrawBackground();

      Color ItemColor = (Color)((ComboBox)sender).Items[e.Index];
      Rectangle ColorRect = new Rectangle(e.Bounds.Left + 2, e.Bounds.Top + 2, e.Bounds.Height - 5, e.Bounds.Height - 5);
      using (Brush ColorBrush = new SolidBrush(ItemColor))
      {
        e.Graphics.FillRectangle(ColorBrush, ColorRect);
        e.Graphics.DrawRectangle(Pens.Black, ColorRect);
      }

      Rectangle TextRect = Rectangle.FromLTRB(ColorRect.Right + 6, e.Bounds.Top, e.Bounds.Right, e.Bounds.Bottom - 1);
      TextRenderer.DrawText(e.Graphics, ItemColor.Name, e.Font, TextRect, e.ForeColor, e.BackColor,
        TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);

      if ((e.State & DrawItemState.Focus) > 0)
        e.DrawFocusRectangle();
    }

    private Color[] CreateColors(Color current)
    {
      // Default colors
      Color[] Result = new Color[]
      {
        Color.Black,
        Color.Silver,
        Color.Gray,
        Color.White,
        Color.Maroon,
        Color.Red,
        Color.Purple,
        Color.Fuchsia,
        Color.Green,
        Color.Lime,
        Color.Olive,
        Color.Yellow,
        Color.Navy,
        Color.Blue,
        Color.Teal,
        Color.Aqua
      };
      if (Array.IndexOf(Result, current) < 0)
      {
        Array.Resize(ref Result, Result.Length + 1);
        Result[Result.Length - 1] = current;
      }
      return Result;
    }

    public void LoadComponentSettings()
    {
      cmbBackColor.DataSource = CreateColors(Settings.Default.BackColor);
      cmbBackColor.SelectedItem = Settings.Default.BackColor;
      cmbBackColor.Tag = Settings.Default.BackColor;

      cmbForeColor.DataSource = CreateColors(Settings.Default.ForeColor);
      cmbForeColor.SelectedItem = Settings.Default.ForeColor;
      cmbForeColor.Tag = Settings.Default.ForeColor;
    }

    public void ResetComponentSettings()
    {
      cmbBackColor.SelectedItem = cmbBackColor.Tag;
      cmbForeColor.SelectedItem = cmbForeColor.Tag;
    }

    public void SaveComponentSettings()
    {
      Settings.Default.BackColor = (Color)cmbBackColor.SelectedItem;
      cmbBackColor.Tag = cmbBackColor.SelectedItem;

      Settings.Default.ForeColor = (Color)cmbForeColor.SelectedItem;
      cmbForeColor.Tag = cmbForeColor.SelectedItem;
    }

    public bool SaveSettings
    {
      get
      {
        return
          ((Color)cmbBackColor.SelectedItem != (Color)cmbBackColor.Tag) ||
          ((Color)cmbForeColor.SelectedItem != (Color)cmbForeColor.Tag);
      }
      set { }
    }

    public string SettingsKey
    {
      get { return string.Empty; }
      set { }
    }
  }
}
namespace Nomad.Plugin.CmdPrompt
{
  partial class CmdPromptOptionControl
  {
    /// <summary> 
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary> 
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      System.Windows.Forms.TableLayoutPanel tlpBack;
      System.Windows.Forms.Label lblBackColor;
      System.Windows.Forms.Label lblForeColor;
      this.cmbBackColor = new Nomad.Commons.Controls.ComboBoxEx();
      this.cmbForeColor = new Nomad.Commons.Controls.ComboBoxEx();
      tlpBack = new System.Windows.Forms.TableLayoutPanel();
      lblBackColor = new System.Windows.Forms.Label();
      lblForeColor = new System.Windows.Forms.Label();
      tlpBack.SuspendLayout();
      this.SuspendLayout();
      // 
      // tlpBack
      // 
      tlpBack.AutoSize = true;
      tlpBack.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
      tlpBack.ColumnCount = 2;
      tlpBack.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
      tlpBack.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
      tlpBack.Controls.Add(lblBackColor, 0, 0);
      tlpBack.Controls.Add(lblForeColor, 0, 1);
      tlpBack.Controls.Add(this.cmbBackColor, 1, 0);
      tlpBack.Controls.Add(this.cmbForeColor, 1, 1);
      tlpBack.Location = new System.Drawing.Point(0, 0);
      tlpBack.Name = "tlpBack";
      tlpBack.RowCount = 2;
      tlpBack.RowStyles.Add(new System.Windows.Forms.RowStyle());
      tlpBack.RowStyles.Add(new System.Windows.Forms.RowStyle());
      tlpBack.Size = new System.Drawing.Size(227, 58);
      tlpBack.TabIndex = 0;
      // 
      // lblBackColor
      // 
      lblBackColor.AutoSize = true;
      lblBackColor.Dock = System.Windows.Forms.DockStyle.Left;
      lblBackColor.Location = new System.Drawing.Point(3, 0);
      lblBackColor.Name = "lblBackColor";
      lblBackColor.Size = new System.Drawing.Size(94, 29);
      lblBackColor.TabIndex = 0;
      lblBackColor.Text = "Background color:";
      lblBackColor.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
      // 
      // lblForeColor
      // 
      lblForeColor.AutoSize = true;
      lblForeColor.Dock = System.Windows.Forms.DockStyle.Left;
      lblForeColor.Location = new System.Drawing.Point(3, 29);
      lblForeColor.Name = "lblForeColor";
      lblForeColor.Size = new System.Drawing.Size(90, 29);
      lblForeColor.TabIndex = 1;
      lblForeColor.Text = "Foreground color:";
      lblForeColor.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
      // 
      // cmbBackColor
      // 
      this.cmbBackColor.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
      this.cmbBackColor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cmbBackColor.FormattingEnabled = true;
      this.cmbBackColor.ItemHeight = 17;
      this.cmbBackColor.Location = new System.Drawing.Point(103, 3);
      this.cmbBackColor.Name = "cmbBackColor";
      this.cmbBackColor.Size = new System.Drawing.Size(121, 23);
      this.cmbBackColor.TabIndex = 2;
      this.cmbBackColor.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.cmbColor_DrawItem);
      // 
      // cmbForeColor
      // 
      this.cmbForeColor.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
      this.cmbForeColor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cmbForeColor.FormattingEnabled = true;
      this.cmbForeColor.ItemHeight = 17;
      this.cmbForeColor.Location = new System.Drawing.Point(103, 32);
      this.cmbForeColor.Name = "cmbForeColor";
      this.cmbForeColor.Size = new System.Drawing.Size(121, 23);
      this.cmbForeColor.TabIndex = 3;
      this.cmbForeColor.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.cmbColor_DrawItem);
      // 
      // CmdPromptOptionControl
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.AutoSize = true;
      this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
      this.Controls.Add(tlpBack);
      this.Name = "CmdPromptOptionControl";
      this.Size = new System.Drawing.Size(230, 61);
      tlpBack.ResumeLayout(false);
      tlpBack.PerformLayout();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private Nomad.Commons.Controls.ComboBoxEx cmbBackColor;
    private Nomad.Commons.Controls.ComboBoxEx cmbForeColor;
  }
}

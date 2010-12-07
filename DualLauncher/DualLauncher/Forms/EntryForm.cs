#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2010  Gerald Evans
// 
// Dual Monitor Tools is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace DualLauncher
{
	public partial class EntryForm : Form
	{
		private bool shutDown = false;

		private const int ID_HOTKEY_ACTIVATE = 0x501;

		//private HotKey dualLauncherHotKey;
		private HotKeyController activateHotKeyController;
		public HotKeyController ActivateHotKeyController
		{
			get { return activateHotKeyController; }
		}


		public EntryForm()
		{
			InitializeComponent();

			InitHotKey();
			string filename = Program.MyDbFnm;
			try
			{
				MagicWords.Instance.Load(filename);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Program.MyTitle);
			}
		}

		private void EntryForm_Load(object sender, EventArgs e)
		{
			// initially position ourselves at the center of the primay screen
			Rectangle screenRect = Screen.PrimaryScreen.Bounds;
			this.Location = new Point((screenRect.Width - Size.Width) / 2, (screenRect.Height - Size.Height) / 2);

			// need to be notified whenever the magic words change
			MagicWords.Instance.ListChanged += new ListChangedEventHandler(OnMagicWordsChanged);

			SetAutoComplete();
			HideEntryForm();

			Input.TextChanged += new EventHandler(Input_TextChanged);

			timer.Interval = 500;
			timer.Start();
		}

		void Input_TextChanged(object sender, EventArgs e)
		{
			ShowAliasIcon();
		}

		private void ShowAliasIcon()
		{
			Icon fileIcon = null;
			string alias = Input.Text;
			if (alias.Length > 0)
			{
				try
				{
					// first find magic word for alias 
					MagicWord magicWord = MagicWords.Instance.FindByAlias(alias);
					if (magicWord != null)
					{
						fileIcon = Icon.ExtractAssociatedIcon(magicWord.Filename);
					}
				}
				catch (Exception)
				{
				}
			}
			if (fileIcon != null)
			{
				pictureBoxIcon.Image = fileIcon.ToBitmap();
			}
			else
			{
				pictureBoxIcon.Image = null;
			}
		}

		private void OnMagicWordsChanged(object o, ListChangedEventArgs args)
		{
			SetAutoComplete();
		}

		private void SetAutoComplete()
		{
			this.Input.AutoCompleteCustomSource = MagicWords.Instance.GetAutoCompleteWords();
			Input.AutoCompleteSource = AutoCompleteSource.CustomSource;
			Input.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
		}

		private void EntryForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			// don't shutdown if the form is just being closed 
			if (shutDown || e.CloseReason != CloseReason.UserClosing)
			{
				CleanUp();
			}
			else
			{
				// just hide the form and stop it from closing
				HideEntryForm();
				e.Cancel = true;
			}
		}

		private void CleanUp()
		{
			// make sure magic word list is saved if needed
			try
			{
				MagicWords.Instance.SaveIfDirty(Program.MyDbFnm);
			}
			catch (Exception)
			{
				// TODO: is it too late to do anything about this?
			}
			TermHotKey();
		}

		private void InitHotKey()
		{
			//KeyCombo defaultKeyCombo = new KeyCombo();
			//defaultKeyCombo.FromPropertyValue(Properties.Settings.Default.HotKeyValue);

			//dualLauncherHotKey = new HotKey(this, ID_HOTKEY_DUALLAUNCHER);
			//dualLauncherHotKey.RegisterHotKey(defaultKeyCombo);

			//dualLauncherHotKey.HotKeyPressed += new HotKey.HotKeyHandler(ShowEntryForm);

			activateHotKeyController = new HotKeyController(this, ID_HOTKEY_ACTIVATE,
				"ActivateHotKey",
				Properties.Resources.ActivateDescription,
				"",		// no Windows 7 key
				new HotKey.HotKeyHandler(ShowEntryForm));

		}

		private void TermHotKey()
		{
			//dualLauncherHotKey.Dispose();
			activateHotKeyController.Dispose();
		}

		private void ShowEntryForm()
		{
			this.Activate();
			this.Visible = true;
			this.Input.Focus();
		}

		private void HideEntryForm()
		{
			this.Visible = false;
		}


		private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			ShowEntryForm();
		}

		#region context menu handlers

		private void enterMagicWordToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ShowEntryForm();
		}

		private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OptionsForm dlg = new OptionsForm(this);
			dlg.ShowDialog();
		}

		private void aboutDualLauncherToolStripMenuItem_Click(object sender, EventArgs e)
		{
			AboutForm dlg = new AboutForm();
			// TODO: why doesn't this appear to be modal?
			dlg.ShowDialog();
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			shutDown = true;
			this.Close();
			Application.Exit();
		}
		#endregion

		private void Input_KeyDown(object sender, KeyEventArgs e)
		{
			Trace.WriteLine(string.Format("KeyCode: {0} KeyValue: {1} KeyData: {2}",
				e.KeyCode, e.KeyValue, e.KeyData));
			if (e.KeyCode == Keys.Enter)
			{
				ProcessInput(1);
			}
			else if (e.KeyCode == Keys.F1)
			{
				ProcessInput(1);
			}
			else if (e.KeyCode == Keys.F2)
			{
				ProcessInput(2);
			}
			else if (e.KeyCode == Keys.F3)
			{
				ProcessInput(3);
			}
			else if (e.KeyCode == Keys.F4)
			{
				ProcessInput(4);
			}
			else if (e.KeyCode == Keys.Escape)
			{
				HideEntryForm();
			}
		}

		private void ProcessInput(int position)
		{
			string alias = Input.Text;
			MagicWord magicWord = MagicWords.Instance.FindByAlias(alias);

			if (magicWord != null)
			{
				magicWord.UseCount++;
				magicWord.LastUsed = DateTime.Now;
				StartupPosition startPosition;
				if (position == 2)
				{
					startPosition = magicWord.StartupPosition2;
				}
				else if (position == 3)
				{
					startPosition = magicWord.StartupPosition3;
				}
				else if (position == 4)
				{
					startPosition = magicWord.StartupPosition4;
				}
				else
				{
					startPosition = magicWord.StartupPosition1;
				}
				StartupController.Instance.Launch(magicWord, startPosition);
				Input.Text = "";
				HideEntryForm();
			}
		}

		private void timer_Tick(object sender, EventArgs e)
		{
			StartupController.Instance.Poll();
		}
	}
}
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
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace DualLauncher.Controls
{
	public partial class StartupPositionControl : UserControl
	{
		public StartupPositionControl()
		{
			InitializeComponent();
		}

		private void StartupPositionControl_Load(object sender, EventArgs e)
		{

		}

		public void InitControl(StartupPosition startupPosition)
		{
			// called from load of containing form
			if (startupPosition != null)
			{
				checkBoxEnable.Checked = startupPosition.EnablePosition;
				textBoxX.Text = startupPosition.Position.Left.ToString();
				textBoxY.Text = startupPosition.Position.Top.ToString();
				textBoxCX.Text = startupPosition.Position.Width.ToString();
				textBoxCY.Text = startupPosition.Position.Height.ToString();

				// TODO: checkbox
			}

			// initialise the window picker
			this.windowPicker.InitControl(DualLauncher.Properties.Resources.TargetCursor,
				Properties.Resources.WinNoCrossHairs,
				Properties.Resources.WinCrossHairs);

			this.windowPicker.HoveredWindowChanged += new WindowPicker.HoverHandler(windowPicker_HoveredWindowChanged);

			UpdateEnableState();
		}

		void windowPicker_HoveredWindowChanged(IntPtr hWnd)
		{
			Win32.RECT rect;
			if (Win32.GetWindowRect(hWnd, out rect))
			{
				Rectangle rectangle = ScreenHelper.RectToRectangle(ref rect);
				SetWindowRect(rectangle);
			}
		}

		public void SetWindowRect(Rectangle rectangle)
		{
			this.textBoxX.Text = rectangle.X.ToString();
			this.textBoxY.Text = rectangle.Y.ToString();
			this.textBoxCX.Text = rectangle.Width.ToString();
			this.textBoxCY.Text = rectangle.Height.ToString();
		}

		public StartupPosition GetPosition()
		{
			StartupPosition startupPosition = new StartupPosition();
			GetPosition(startupPosition);

			return startupPosition;
		}

		public bool GetPosition(StartupPosition position)
		{
			bool isValid = true;

			position.EnablePosition = checkBoxEnable.Checked;

			int left = TextBoxToInt(textBoxX, ref isValid);
			int top = TextBoxToInt(textBoxY, ref isValid);
			int width = TextBoxToInt(textBoxCX, ref isValid);
			int height = TextBoxToInt(textBoxCY, ref isValid);

			if (position.EnablePosition)
			{
				// co-ords must be valid
			}
			else
			{
				// not worried if co-ords are not valid, but want to keep any valid values
				isValid = true;
			}

			if (isValid)
			{
				position.Position = new Rectangle(left, top, width, height);
			}

			return isValid;
		}

		private int TextBoxToInt(Control control, ref bool isValid)
		{
			// only set isValid to false
			int ret = 0;	// defalt value if not a number
			try
			{
				ret = Convert.ToInt32(control.Text);
			}
			catch (Exception)
			{
				// if this is the first control that is invalid, set focus to it
				if (isValid)
				{
					control.Focus();
				}
				isValid = false;
			}

			return ret;
		}

		private void checkBoxEnable_CheckedChanged(object sender, EventArgs e)
		{
			UpdateEnableState();
		}

		private void UpdateEnableState()
		{
			textBoxX.Enabled = checkBoxEnable.Checked;
			textBoxY.Enabled = checkBoxEnable.Checked;
			textBoxCX.Enabled = checkBoxEnable.Checked;
			textBoxCY.Enabled = checkBoxEnable.Checked;
			comboBoxWinType.Enabled = checkBoxEnable.Checked;
		}
	}
}
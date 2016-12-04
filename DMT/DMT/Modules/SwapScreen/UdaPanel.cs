﻿#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2015  Gerald Evans
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

namespace DMT.Modules.SwapScreen
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Data;
	using System.Drawing;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Windows.Forms;

	/// <summary>
	/// Panel to show user defined area
	/// </summary>
	partial class UdaPanel : UserControl
	{
		UdaController _udaController = null;

		/// <summary>
		/// Initialises a new instance of the <see cref="UdaPanel" /> class.
		/// </summary>
		public UdaPanel()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Set the controller associated with the user defined area
		/// </summary>
		/// <param name="udaController">User defined area controller</param>
		public void SetUdaController(UdaController udaController)
		{
			_udaController = udaController;

			UpdateDisplay();
		}

		public void UpdateDisplay()
		{
			labelDescription.Text = _udaController.Description;
			labelKeyCombo.Text = _udaController.ToString();
		}

		private void buttonChange_Click(object sender, EventArgs e)
		{
			// show edit box
			if (_udaController != null)
			{
				if (_udaController.Edit())
				{
					UpdateDisplay();
				}
			}
		}
	}
}

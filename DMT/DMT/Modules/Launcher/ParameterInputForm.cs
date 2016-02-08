﻿#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2009-2015 Gerald Evans
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

namespace DMT.Modules.Launcher
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
	/// Dialog to enter a parameter required by a magic word
	/// </summary>
	public partial class ParameterInputForm : Form
	{
		/// <summary>
		/// Initialises a new instance of the <see cref="ParameterInputForm" /> class.
		/// </summary>
		public ParameterInputForm()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Gets or sets the prompt for the parameter
		/// </summary>
		public string ParameterPrompt { get; set; }

		/// <summary>
		/// Gets or sets the value the user entered for the parameter
		/// </summary>
		public string ParameterValue { get; protected set; }
	
		private void ParameterInputForm_Load(object sender, EventArgs e)
		{
			labelPrompt.Text = ParameterPrompt + ":";
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			ParameterValue = textBoxParameter.Text;
		}
	}
}

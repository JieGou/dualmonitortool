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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMT.Library.Settings
{
	interface ISettingsService
	{
		bool SettingExists(string moduleName, string settingName);

		int GetSettingAsInt(string moduleName, string settingName, int defaultValue = 0);
		void SetSetting(string moduleName, string settingName, int value);

		uint GetSettingAsUInt(string moduleName, string settingName, uint defaultValue = 0);
		void SetSetting(string moduleName, string settingName, uint value);

		bool GetSettingAsBool(string moduleName, string settingName, bool defaultValue = false);
		void SetSetting(string moduleName, string settingName, bool set);

		string GetSetting(string moduleName, string settingName);
		void SetSetting(string moduleName, string settingName, string settingValue);

		void SaveSettings();
	}
}
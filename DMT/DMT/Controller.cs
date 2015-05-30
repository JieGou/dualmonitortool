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

using DMT.Library.HotKeys;
using DMT.Modules;
using DMT.Library.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DMT.Library.Logging;

namespace DMT
{
	class Controller
	{
		AppForm _appForm;
		ISettingsService _settingsService;
		IHotKeyService _hotKeyService;
		IModuleService _moduleService;
		ICommandRunner _commandRunner;
		ILogger _logger;

		public Controller(AppForm appForm)
		{
			_appForm = appForm;
		}

		public void Start()
		{
			_settingsService = new SettingsRepository();
			_hotKeyService = new HotKeyRepository(_appForm, _settingsService);
			// ModuleRepository provides both IModuleService and ICommandRunner
			ModuleRepository moduleRepository = new ModuleRepository();
			_moduleService = moduleRepository;
			_commandRunner = moduleRepository;
			_logger = new Logger();

			// now add the modules
			_moduleService.AddModule(new DMT.Modules.General.GeneralModule(_settingsService, _hotKeyService, _logger));
			_moduleService.AddModule(new DMT.Modules.Cursor.CursorModule(_settingsService, _hotKeyService, _logger));
			_moduleService.AddModule(new DMT.Modules.Launcher.LauncherModule(_settingsService, _hotKeyService, _logger, _appForm, _commandRunner));
			_moduleService.AddModule(new DMT.Modules.Snap.SnapModule(_settingsService, _hotKeyService, _logger, _appForm));
			_moduleService.AddModule(new DMT.Modules.SwapScreen.SwapScreenModule(_settingsService, _hotKeyService, _logger));
			_moduleService.AddModule(new DMT.Modules.WallpaperChanger.WallpaperChangerModule(_settingsService, _hotKeyService, _logger, _appForm));
		}

		public void Stop()
		{
			// stop each module in turn
			_moduleService.TerminateAllModules();

			// release all hotkeys in one go
			_hotKeyService.Stop();
		}

		public OptionsForm CreateOptionsForm()
		{
			return new OptionsForm(_moduleService);
		}
	}
}
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
using DMT.Library.Logging;
using DMT.Library.PInvoke;
using DMT.Library.Settings;
using DMT.Library.Utils;
using DMT.Resources;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DMT.Modules.Launcher
{
	class LauncherModule : Module
	{
		//const string _moduleName = "Launcher";
		const int _defaultMaxIcons = 8;

		ISettingsService _settingsService;
		//IHotKeyService _hotKeyService;
		ILogger _logger;
		AppForm _appForm;
		//IModuleService _moduleService;
		ICommandRunner _commandRunner;
		StartupHandler _startupHandler;

		MagicWords _magicWords = null;
		public MagicWords MagicWords { get { return GetMagicWords(); } }

		EntryForm _entryForm = null;

		public ICommandRunner CommandRunner { get { return _commandRunner; } }

		// hotkey to start entry of magic word
		public HotKeyController ActivateHotKeyController { get; protected set; }
		// hotkey to create a magic word for application that started the current window
		public HotKeyController AddMagicWordHotKeyController { get; protected set; }

		IntSetting MaxIconsSetting { get; set; }
		public int MaxIcons 
		{
			get { return MaxIconsSetting.Value; }
			set { MaxIconsSetting.Value = value; }
		}

		BoolSetting UseMruSetting { get; set; }
		public bool UseMru
		{
			get { return UseMruSetting.Value; }
			set { UseMruSetting.Value = value; }
		}

		BoolSetting LoadWordsOnStartupSetting { get; set; }
		public bool LoadWordsOnStartup
		{
			get { return LoadWordsOnStartupSetting.Value; }
			set { LoadWordsOnStartupSetting.Value = value; }
		}

		IntSetting IconViewSetting { get; set; }
		public View IconView
		{
			get { return (View)IconViewSetting.Value; }
			set { IconViewSetting.Value = (int)value; }
		}

		IntSetting StartupTimeoutSetting { get; set; }
		public int StartupTimeout
		{
			get { return StartupTimeoutSetting.Value; }
			set { StartupTimeoutSetting.Value = value; }
		}

		UIntSetting Position1KeySetting { get; set; }
		public uint Position1Key
		{
			get { return Position1KeySetting.Value; }
			set { Position1KeySetting.Value = value; }
		}

		UIntSetting Position2KeySetting { get; set; }
		public uint Position2Key
		{
			get { return Position2KeySetting.Value; }
			set { Position2KeySetting.Value = value; }
		}

		UIntSetting Position3KeySetting { get; set; }
		public uint Position3Key
		{
			get { return Position3KeySetting.Value; }
			set { Position3KeySetting.Value = value; }
		}

		UIntSetting Position4KeySetting { get; set; }
		public uint Position4Key
		{
			get { return Position4KeySetting.Value; }
			set { Position4KeySetting.Value = value; }
		}

		public Rectangle EntryFormPosition
		{
			get
			{
				Rectangle rect = new Rectangle(0, 0, 0, 0);
				if (_settingsService.SettingExists(ModuleName, "EntryFormPosition"))
				{
					rect = StringUtils.ToRectangle(_settingsService.GetSetting(ModuleName, "EntryFormPosition"));
				}
				return rect;
			}
			set
			{
				string settingString = StringUtils.FromRectangle(value);
				_settingsService.SetSetting(ModuleName, "EntryFormPosition", settingString);
			}
		}

		public LauncherModule(ISettingsService settingsService, IHotKeyService hotKeyService, ILogger logger, AppForm appForm, ICommandRunner commandRunner)
			: base(hotKeyService)
		{
			_settingsService = settingsService;
			//_hotKeyService = hotKeyService;
			_logger = logger;
			_appForm = appForm;
			_commandRunner = commandRunner;

			ModuleName = "Launcher";

			Start();
		}


		public override ModuleOptionNode GetOptionNodes()
		{
			ModuleOptionNodeBranch options = new ModuleOptionNodeBranch("Launcher", new LauncherRootOptionsPanel());
			options.Nodes.Add(new ModuleOptionNodeLeaf("Magic Words", new LauncherMagicWordsOptionsPanel(this)));
			options.Nodes.Add(new ModuleOptionNodeLeaf("HotKeys", new LauncherHotKeysOptionsPanel(this)));
			options.Nodes.Add(new ModuleOptionNodeLeaf("General", new LauncheGeneralOptionsPanel(this)));
			options.Nodes.Add(new ModuleOptionNodeLeaf("Import / Export", new LauncherImportOptionsPanel(this)));

			return options;
		}

		public void ShowOptions()
		{
			// TODO: would be nicer if it opened on the launcher root page
			_appForm.ShowOptions();
		}

		void Start()
		{
			// this handles the actual starting up of applications and moving their windows
			_startupHandler = new StartupHandler(_appForm, _commandRunner);

			// hot keys
			// don't want a magic word to show the magic word entry form!
			ActivateHotKeyController = AddCommand("Activate", LauncherStrings.ActivateDescription, "", ShowEntryForm, true, false);
			AddMagicWordHotKeyController = AddCommand("AddMagicWord", LauncherStrings.AddMagicWordDescription, "", AddNewMagicWordForActiveWindow);
			//ActivateHotKeyController = CreateHotKeyController("ActivateHotKey", LauncherStrings.ActivateDescription, "", ShowEntryForm);
			//AddMagicWordHotKeyController = CreateHotKeyController("AddMagicWordHotKey", LauncherStrings.AddMagicWordDescription, "", AddNewMagicWordForActiveWindow);
			//base.RegisterHotKeys();


			// settings
			MaxIconsSetting = new IntSetting(_settingsService, ModuleName, "MaxIcons", _defaultMaxIcons);
			UseMruSetting = new BoolSetting(_settingsService, ModuleName, "UseMru");
			LoadWordsOnStartupSetting = new BoolSetting(_settingsService, ModuleName, "LoadWordsOnStartup");
			IconViewSetting = new IntSetting(_settingsService, ModuleName, "IconView", (int)View.LargeIcon);
			StartupTimeoutSetting = new IntSetting(_settingsService, ModuleName, "StartupTimeout", 60);

			Position1KeySetting = new UIntSetting(_settingsService, ModuleName, "Position1Key", (uint)Keys.F1);
			Position2KeySetting = new UIntSetting(_settingsService, ModuleName, "Position2Key", (uint)Keys.F2);
			Position3KeySetting = new UIntSetting(_settingsService, ModuleName, "Position3Key", (uint)Keys.F3);
			Position4KeySetting = new UIntSetting(_settingsService, ModuleName, "Position4Key", (uint)Keys.F4);


			// add our menu items to the notifcation icon
			_appForm.AddMenuItem(LauncherStrings.EnterMagicWord, null, enterMagicWordToolStripMenuItem_Click);
			_appForm.AddMenuItem(LauncherStrings.AddMagicWord, null, addMagicWordToolStripMenuItem_Click);
			_appForm.AddMenuItem("-", null, null);


			if (LoadWordsOnStartup)
			{
				// load magic words now
				// (otherwise they will be loaded when first needed)
				GetMagicWords();
			}
		}

		public override void Terminate()
		{
			if (_entryForm != null)
			{
				_entryForm.Terminate();
			}

			SaveMagicWords();
		}

		public void SaveMagicWords()
		{
			if (_magicWords != null)
			{
				string filename = FileLocations.Instance.MagicWordsFilename;
				_magicWords.SaveIfDirty(filename);
			}
		}

		/// <summary>
		/// Starts a new process
		/// </summary>
		/// <param name="magicWord">The magic word being started</param>
		/// <param name="startPosition">The StartupPosition to use</param>
		/// <param name="map">A ParameterMap to use for any dynamic input</param>
		/// <returns>true if application started</returns>
		public bool Launch(MagicWord magicWord, StartupPosition startPosition, ParameterMap map)
		{
			return _startupHandler.Launch(magicWord, startPosition, map, StartupTimeout);
		}
	
		//HotKeyController CreateHotKeyController(string settingName, string description, string win7Key, HotKey.HotKeyHandler handler)
		//{
		//	return _hotKeyService.CreateHotKeyController(ModuleName, settingName, description, win7Key, handler);
		//}

		void enterMagicWordToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ShowEntryForm();
		}

		void addMagicWordToolStripMenuItem_Click(object sender, EventArgs e)
		{
			AddNewMagicWord(new MagicWord());
		}

		void AddNewMagicWordForActiveWindow()
		{
			MagicWord newMagicWord = new MagicWord();
			// get the active window
			IntPtr hWnd = Win32.GetForegroundWindow();

			if (hWnd != IntPtr.Zero)
			{
				// use details from the active window to prefill a new MagicWord
				MagicWordForm.GetWindowDetails(hWnd, newMagicWord);
			}
			AddNewMagicWord(newMagicWord);
		}

		private void AddNewMagicWord(MagicWord newMagicWord)
		{
			// let the user edit the details

			// need to activate, or the MagicWordForm will be hidden under whatever was the active window
			GetEntryForm().Activate();
			MagicWordForm dlg = new MagicWordForm(this, newMagicWord);
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				// user wants this word, so insert it
				MagicWords.Insert(newMagicWord);
			}
		}

		void ShowEntryForm()
		{
			GetEntryForm().ShowEntryForm();
		}

		EntryForm GetEntryForm()
		{
			if (_entryForm == null)
			{
				_entryForm = new EntryForm(this, _commandRunner);
			}
			return _entryForm;
		}

		MagicWords GetMagicWords()
		{
			if (_magicWords == null)
			{
				_magicWords = new MagicWords();
				string filename = FileLocations.Instance.MagicWordsFilename;
				_magicWords.Load(filename);
			}

			return _magicWords;
		}
	}
}
#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2010-2015  Gerald Evans
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

namespace DMT.Library.Wallpaper
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Drawing;
	using System.IO;
	using System.Text;
	using System.Threading;
	using System.Windows.Forms;

	using DMT.Library.Environment;
	using DMT.Library.PInvoke;
	using DMT.Library.Settings;
	using Microsoft.Win32;

	/// <summary>
	/// This handles Windows specific aspects of wallpaper.
	/// This includes handling the case where you have a monitor to the left
	/// or above the primary monitor, as Windows requires that (0,0) in
	/// the wallpaper corresponds to (0,0) on your primary monitor.
	/// </summary>
	class WindowsWallpaper
	{
		ILocalEnvironment _localEnvironment;
		private Image srcImage;
		private Rectangle virtualDesktop;

		/// <summary>
		/// Initialises a new instance of the <see cref="WindowsWallpaper" /> class.
		/// Takes the virtual desktop rectangle and
		/// an image which is laid out corresponding to the virtual desktop,
		/// so the TLHC of the image corresponds to the TLHC of the virtual desktop
		/// which may not be the same as the TLHC of the primary monitor.
		/// </summary>
		/// <param name="localEnvironment">The local environment</param>
		/// <param name="srcImage">image for the wallpaper</param>
		/// <param name="virtualDesktop">virtual desktop rectangle</param>
		public WindowsWallpaper(ILocalEnvironment localEnvironment, Image srcImage, Rectangle virtualDesktop)
		{
			_localEnvironment = localEnvironment;
			Debug.Assert(srcImage.Size == virtualDesktop.Size, "Image size is wrong");
			this.srcImage = srcImage;
			this.virtualDesktop = virtualDesktop;
		}

		/// <summary>
		/// Sets the Windows wallpaper.
		/// This will create a new image if the primary monitor
		/// is not both the leftmost and topmost monitor.
		/// </summary>
		/// <param name="useFade">If true, tries to use a smooth fade between wallpapers</param>
		public void SetWallpaper(bool useFade)
		{
			bool wrapped;
			Image image = WrapImage(out wrapped);

			SetWallpaper(image, useFade);

			if (wrapped)
			{
				image.Dispose();
			}
		}

		/// <summary>
		/// Saves the wallpaper to a file in a format usable by most? 
		/// automatic screen changers.
		/// This will create a new image if the primary monitor
		/// is not both the leftmost and topmost monitor.
		/// </summary>
		/// <param name="fnm">Filename to save the wallpaper too</param>
		public void SaveWallpaper(string fnm)
		{
			bool wrapped;
			Image image = WrapImage(out wrapped);

			SaveWallpaper(image, fnm);

			if (wrapped)
			{
				image.Dispose();
			}
		}

		static void SetActiveDesktopWallpaperThread(string path)
		{
			EnableActiveDesktop();
			ActiveDesktop.IActiveDesktop activeDesktop = ActiveDesktop.GetActiveDesktop();
			activeDesktop.SetWallpaper(path, 0);
			//activeDesktop.ApplyChanges(ActiveDesktop.AD_Apply.ALL | ActiveDesktop.AD_Apply.FORCE);
			// Using FORCE seems to cause some applications/windows to repaint themselves causing flicker
			activeDesktop.ApplyChanges(ActiveDesktop.AD_Apply.ALL);
		}

		static void EnableActiveDesktop()
		{
			IntPtr hWndProgman = NativeMethods.FindWindow("Progman", null);
			uint msg = 0x52C;	// TODO: need a const in Win32
			IntPtr wParam = IntPtr.Zero;
			IntPtr lParam = IntPtr.Zero;
			uint fuFlags = 0; // SMTO_NORMAL // TODO: need a const in Win32
			uint uTimeout = 500;	// in ms
			IntPtr lpdwResult = IntPtr.Zero;
			NativeMethods.SendMessageTimeout(hWndProgman, msg, wParam, lParam, fuFlags, uTimeout, out lpdwResult);
		}

		void SetWallpaper(Image wallpaper, bool useFade)
		{
			string path = FileLocations.Instance.WallpaperFilename;

			try
			{
				wallpaper.Save(path, System.Drawing.Imaging.ImageFormat.Bmp);

				// make sure image is tiled (must do this for both normal and ActiveDesktop wallpaper)
				SetTiledWallpaper();

				if (useFade)
				{
					SetActiveDesktopWallpaper(path);
				}
				else
				{
					SetDesktopWallpaper(path);
				}

				// save the location of the wallpaper bitmap so that the screen saver can pick it up
				DmtRegistry.SetDmtWallpaperFilename(path);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		void SetTiledWallpaper()
		{
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
			{
				key.SetValue("TileWallpaper", "1");
				key.SetValue("WallpaperStyle", "0");
			}
		}

		void SetDesktopWallpaper(string path)
		{
			// now set the wallpaper
			NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETDESKWALLPAPER, 0, path, NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDWININICHANGE);
		}

		void SetActiveDesktopWallpaper(string path)
		{
			Thread thread = new Thread(() => SetActiveDesktopWallpaperThread(path));
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();

			// don't see any need to wait for the thread to complete
		}

		private void SaveWallpaper(Image wallpaper, string fnm)
		{
			try
			{
				wallpaper.Save(fnm, System.Drawing.Imaging.ImageFormat.Bmp);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private Image WrapImage(out bool wrapped)
		{
			if (NeedsWrapping())
			{
				// must wrap image
				// so that the four quadrants
				// ab
				// cd
				// where d would be the primary monitor to
				// dc
				// ba
				wrapped = true;
				Image image = new Bitmap(srcImage.Width, srcImage.Height);
				int xWrap = -virtualDesktop.Left;
				int xNotWrap = srcImage.Width - xWrap;
				int yWrap = -virtualDesktop.Top;
				int yNotWrap = srcImage.Height - yWrap;

				using (Graphics g = Graphics.FromImage(image))
				{
					// quadrant a
					if (xWrap > 0 && yWrap > 0)
					{
						g.DrawImage(
							srcImage,
							new Rectangle(xNotWrap, yNotWrap, xWrap, yWrap),
							new Rectangle(0, 0, xWrap, yWrap),
							GraphicsUnit.Pixel);
					}

					// quadrant b
					if (yWrap > 0 && xNotWrap > 0)
					{
						g.DrawImage(
							srcImage,
							new Rectangle(0, yNotWrap, xNotWrap, yWrap),
							new Rectangle(xWrap, 0, xNotWrap, yWrap),
							GraphicsUnit.Pixel);
					}

					// quadrant c
					if (xWrap > 0 && yNotWrap > 0)
					{
						g.DrawImage(
							srcImage,
							new Rectangle(xNotWrap, 0, xWrap, yNotWrap),
							new Rectangle(0, yWrap, xWrap, yNotWrap),
							GraphicsUnit.Pixel);
					}

					// quadrant d
					if (xNotWrap > 0 && yNotWrap > 0)
					{
						g.DrawImage(
							srcImage,
							new Rectangle(0, 0, xNotWrap, yNotWrap),
							new Rectangle(xWrap, yWrap, xNotWrap, yNotWrap),
							GraphicsUnit.Pixel);
					}
				}

				wrapped = true;
				return image;
			}
			else
			{
				// can use original src image
				wrapped = false;
				return srcImage;
			}
		}

		bool NeedsWrapping()
		{
			// On Windows versions prior to 8, (0,0) in the wallpaper corresponds to (0,0) on your primary monitor
			// On 8 (0,0) in the wallpaper corresponds to the TLHC of your monitors
			if (virtualDesktop.Left < 0 || virtualDesktop.Top < 0)
			{
				// TLHC is not (0,0)
				if (_localEnvironment.IsWin8OrLater())
				{
					// Win 8 and later want a direct mapping
					return false;
				}
				else
				{
					// earlier versions expect the primary TLHC to be (0,0)
					return true;
				}
			}
			else
			{
				// TLHC is (0,0) so direct mapping
				return false;
			}
		}
	}
}

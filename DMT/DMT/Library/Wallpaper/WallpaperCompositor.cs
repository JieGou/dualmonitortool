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

using DMT.Library.Environment;
using DMT.Library.Transform;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DMT.Library.Wallpaper
{
	/// <summary>
	/// The top-level non-gui logic is in here
	/// </summary>
	class WallpaperCompositor : IWallpaperCompositor
	{
		Monitors _monitors;

		private List<ScreenMapping> _allScreens = new List<ScreenMapping>();
		/// <summary>
		/// List of all of the screens available
		/// </summary>
		public List<ScreenMapping> AllScreens
		{
			get { return _allScreens; }
		}

		private List<ScreenMapping> _activeScreens = new List<ScreenMapping>();

		private Rectangle desktopRect;
		/// <summary>
		/// The rectangle that covers all of the screens
		/// </summary>
		public Rectangle DesktopRect
		{
			get { return desktopRect; }
		}

        private Color _desktopRectBackColor = Color.Black;
        /// <summary>
        /// The background color of the rectangle that covers all of the screens
        /// </summary>

        public Color DesktopRectBackColor
        {
            get { return _desktopRectBackColor; }
            set { _desktopRectBackColor = value; }
        }
	
		/// <summary>
		/// ctor
		/// </summary>
		public WallpaperCompositor(Monitors monitors)
		{
			_monitors = monitors;
			Init();
		}

		/// <summary>
		/// Sets which screens are currently active
		/// </summary>
		/// <param name="screenIndexes">List of zero based screen indexes</param>
		public void SetActiveScreens(List<int> screenIndexes)
		{
			_activeScreens.Clear();
			foreach (int screenIndex in screenIndexes)
			{
				Debug.Assert(screenIndex >= 0 && screenIndex < _allScreens.Count);
				_activeScreens.Add(_allScreens[screenIndex]);
			}
		}

		/// <summary>
		/// Adds the specified image to cover all active screens
		/// </summary>
		/// <param name="image">The image to add</param>
		/// <param name="fit">What to do if the image size and rectangle covering all the
		/// active screens have different aspect ratios.</param>
		public void AddImage(Image image, StretchType.Fit fit)
		{
			Debug.Assert(_activeScreens.Count > 0);
			GenerateMappings(image, fit);
		}

		public void AddImage(Image image, List<int> screenIndexes, StretchType.Fit fit)
		{
			SetActiveScreens(screenIndexes);
			Debug.Assert(_activeScreens.Count > 0);
			GenerateMappings(image, fit);
		}


		/// <summary>
		/// Creates the image to use as the wallpaper
		/// Note this image is laid out the same way as the monitors are laid out.
		/// </summary>
		/// <returns>Image containing the wallpaper</returns>
		public Image CreateWallpaperImage()
		{
			Bitmap image = new Bitmap(desktopRect.Width, desktopRect.Height);

			using (Graphics g = Graphics.FromImage(image))
			{
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.Clear(_desktopRectBackColor);

				foreach (ScreenMapping screenMapping in _allScreens)
				{
					if (screenMapping.SourceImage != null)
					{
						// the image starts at (0,0)
						// but screen co-ords do not necessarily start at (0,0)
						// so determine destination rectangle based on (0,0)
						Rectangle destRect = screenMapping.DestRect;
						Point offset = new Point(-desktopRect.Left, -desktopRect.Top);
						destRect.Offset(offset);
						g.DrawImage(screenMapping.SourceImage,
									destRect,
									screenMapping.SourceRect,
									GraphicsUnit.Pixel);
					}
				}
			}

			return image;
		}

		/// <summary>
		/// Moves the image on the active screens
		/// </summary>
		/// <param name="deltaX">Number of pixels to move the wallpaper image to the right by</param>
		/// <param name="deltaY">Number of pixels to move the wallpaper image down by</param>
		public void MoveActiveScreens(int deltaX, int deltaY)
		{
			foreach (ScreenMapping screenMapping in _activeScreens)
			{
				if (deltaX != 0)
				{
					screenMapping.DisplaceX(deltaX);
				}
				if (deltaY != 0)
				{
					screenMapping.DisplaceY(deltaY);
				}
			}
		}

		public void MoveScreens(List<int> screenIndexes, int deltaX, int deltaY)
		{
			SetActiveScreens(screenIndexes);
			MoveActiveScreens(deltaX, deltaY);
		}


		public void ZoomActiveScreens(double factor)
		{
			// determine center of zoom
			Debug.Assert(_activeScreens.Count > 0);
			Rectangle boundingRect = GetBoundingRect(_activeScreens);
			Point center = new Point(boundingRect.Left + boundingRect.Width / 2,
			                         boundingRect.Top + boundingRect.Height / 2);
			
			// now zoom each of the active screens around this point
			foreach (ScreenMapping screenMapping in _activeScreens)
			{
				screenMapping.Zoom(center, factor);
			}
		}

		public void ZoomScreens(List<int> screenIndexes, double factor)
		{
			SetActiveScreens(screenIndexes);
			ZoomActiveScreens(factor);
		}

		private void GenerateMappings(Image image, StretchType.Fit fit)
		{
			Debug.Assert(_activeScreens.Count > 0);
			Rectangle boundingRect = GetBoundingRect(_activeScreens);

			Rectangle imageRect = new Rectangle(new Point(0, 0), image.Size);
			Rectangle virtualDestRect = GetvirtualDestRect(image, fit, boundingRect);

			// imageRect gets mapped to virtualDestRect
			// now need to work out the mappings for the individual screens
			foreach (ScreenMapping screenMapping in _activeScreens)
			{
				//GenerateMapping(screenMapping, imageRect, virtualDestRect);
				//screenMapping.SourceImage = image;
				screenMapping.GenerateMapping(image, virtualDestRect);
			}
		}

		/// <summary>
		/// Given the co-ords of a rectangle in different spaces,
		/// using the same mapping calculates the co-ords of a second rectangle
		/// in the second space.
		/// </summary>
		/// <param name="src1">First rectangle in first space</param>
		/// <param name="dest1">First rectangle in second space</param>
		/// <param name="src2">Second rectangle in first space</param>
		/// <returns>Second rectangle in second space</returns>
		public static Rectangle CalcDestRect(Rectangle src1, Rectangle dest1, Rectangle src2)
		{
			// TODO: use CalcDestPoint
			int left = ScaleDest(src1.Left, src1.Right, dest1.Left, dest1.Right, src2.Left);
			int right = ScaleDest(src1.Left, src1.Right, dest1.Left, dest1.Right, src2.Right);
			int top = ScaleDest(src1.Top, src1.Bottom, dest1.Top, dest1.Bottom, src2.Top);
			int bottom = ScaleDest(src1.Top, src1.Bottom, dest1.Top, dest1.Bottom, src2.Bottom);

			Rectangle dest2 = new Rectangle(left, top, right - left, bottom - top);

			return dest2;
		}

		/// <summary>
		/// Given the co-ords of a point in different spaces,
		/// using the same mapping calculates the co-ords of a second point
		/// in the second space.
		/// </summary>
		/// <param name="src1">First point in firts space</param>
		/// <param name="dest1">First point in second space</param>
		/// <param name="src2">Second point in first space</param>
		/// <returns>Second point in second space</returns>
		public static Point CalcDestPoint(Rectangle src1, Rectangle dest1, Point src2)
		{
			int x = ScaleDest(src1.Left, src1.Right, dest1.Left, dest1.Right, src2.X);
			int y = ScaleDest(src1.Top, src1.Bottom, dest1.Top, dest1.Bottom, src2.Y);

			return new Point(x, y);
		}

		private static int ScaleDest(int s1, int s2, int d1, int d2, int s3)
		{
			// TODO: use Scaler
			int srcDelta = s2 - s1;

			// + destDelta / 2 to minimise rounding errors
			int d3 = d1 + ((s3 - s1) * (d2 - d1) + srcDelta / 2) / srcDelta;

			return d3;
		}

		private Rectangle GetvirtualDestRect(Image image, StretchType.Fit fit, Rectangle boundingRect)
		{
			Rectangle virtualDestRect = Rectangle.Empty;

			switch (fit)
			{
				case StretchType.Fit.Center:
					virtualDestRect = Center(image.Size, boundingRect);
					break;

				case StretchType.Fit.StretchToFit:
					virtualDestRect = boundingRect;
					break;

				case StretchType.Fit.UnderStretch:
					virtualDestRect = UnderStretch(image.Size, boundingRect);
					break;

				case StretchType.Fit.OverStretch:
					virtualDestRect = OverStretch(image.Size, boundingRect);
					break;

				default:
					Debug.Fail("Unknown type: " + fit.ToString());
					break;
			}


			return virtualDestRect;
		}

		public static Rectangle Center(Size sourceSize, Rectangle destRect)
		{
			Rectangle rect;

			// center of image gets mapped to center of destination
			// so work out the movement involved in doing this
			// remember image is at (0, 0)
			int xShift = destRect.Left + destRect.Width / 2 - sourceSize.Width / 2;
			int yShift = destRect.Top + destRect.Height / 2 - sourceSize.Height / 2;

			rect = new Rectangle(xShift, yShift, sourceSize.Width, sourceSize.Height);

			return rect;
		}


		/// <summary>
		/// Determines the destination rectangle to use to maintain the source aspect ratio
		/// and to fill the destination as much as possible without clipping.
		/// This may result in the need to add bars top and bottom, or left and right
		/// to keep the aspect ratio constant.
		/// </summary>
		/// <param name="sourceSize">Size of source image</param>
		/// <param name="destRect">Area we have available to display the image in</param>
		/// <returns>rectangle for the under stretched image</returns>
		public static Rectangle UnderStretch(Size sourceSize, Rectangle destRect)
		{
			Rectangle rect;

			// check if we need to add either vertical or horizontal bars 
			// either side of the image to keep the source aspect ratio
			int widthFactor = destRect.Width * sourceSize.Height;
			int heightFactor = destRect.Height * sourceSize.Width;
			if (widthFactor > heightFactor)
			{
				// need to add vertical bars
				int newWidth = (sourceSize.Width * destRect.Height) / sourceSize.Height;
				int barSize = (destRect.Width - newWidth) / 2;
				rect = new Rectangle(destRect.Left + barSize,
									 destRect.Top,
									 newWidth,
									 destRect.Height);
			}
			else if (heightFactor > widthFactor)
			{
				// need to add horizontal bars
				int newHeight = (sourceSize.Height * destRect.Width) / sourceSize.Width;
				int barSize = (destRect.Height - newHeight) / 2;
				rect = new Rectangle(destRect.Left,
									 destRect.Top + barSize,
									 destRect.Width,
									 newHeight);
			}
			else
			{
				// perfect type with no need to add bars
				rect = destRect;
			}

			return rect;
		}

		/// <summary>
		/// Determines the destination rectangle to use to maintain the source aspect ratio
		/// and to fill the destination entirley, but keeping the clipping to a minimum.
		/// </summary>
		/// <param name="sourceSize">Size of source image</param>
		/// <param name="destRect">Area we have available to display the image in</param>
		/// <returns>rectangle for the over stretched image</returns>
		public static Rectangle OverStretch(Size sourceSize, Rectangle destRect)
		{
			Rectangle rect;

			// check which sides we need to clip 
			// to keep the source aspect ratio
			int widthFactor = destRect.Width * sourceSize.Height;
			int heightFactor = destRect.Height * sourceSize.Width;
			if (widthFactor > heightFactor)
			{
				// need to clip top and bottom
				int newHeight = (sourceSize.Height * destRect.Width) / sourceSize.Width;
				int clipSize = (newHeight - destRect.Height) / 2;
				rect = new Rectangle(destRect.Left,
									 destRect.Top - clipSize,
									 destRect.Width,
									 newHeight);
			}
			else if (heightFactor > widthFactor)
			{
				// need to clip srcLeft and srcRight
				int newWidth = (sourceSize.Width * destRect.Height) / sourceSize.Height;
				int clipSize = (newWidth - destRect.Width) / 2;
				rect = new Rectangle(destRect.Left - clipSize,
									 destRect.Top,
									 newWidth,
									 destRect.Height);
			}
			else
			{
				// perfect type with no need to add bars
				rect = destRect;
			}

			return rect;
		}

		private void Init()
		{
			//foreach (Screen screen in Screen.AllScreens)
			//{
			//	ScreenMapping screenMapping = new ScreenMapping(screen.Bounds, screen.Primary);
			//	allScreens.Add(screenMapping);
			//}
			foreach (Monitor monitor in _monitors)
			{
				ScreenMapping screenMapping = new ScreenMapping(monitor.Bounds, monitor.Primary);
				_allScreens.Add(screenMapping);
			}


			//desktopRect = GetBoundingRect(allScreens);
			desktopRect = _monitors.Bounds;
		}

		private Rectangle GetBoundingRect(List<ScreenMapping> screenMappingList)
		{
			Rectangle boundingRect = Rectangle.Empty;

			for (int screenIndex = 0; screenIndex < screenMappingList.Count; screenIndex++)
			{
				if (screenIndex == 0)
				{
					boundingRect = screenMappingList[screenIndex].ScreenRect;
				}
				else
				{
					boundingRect = Rectangle.Union(boundingRect, screenMappingList[screenIndex].ScreenRect);
				}
			}

			return boundingRect;
		}
	}
}

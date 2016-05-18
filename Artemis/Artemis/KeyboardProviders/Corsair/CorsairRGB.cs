﻿using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using Artemis.Utilities;
using CUE.NET;
using CUE.NET.Brushes;
using CUE.NET.Devices.Generic.Enums;
using CUE.NET.Devices.Headset;
using CUE.NET.Devices.Keyboard;
using CUE.NET.Devices.Mouse;
using CUE.NET.Exceptions;

namespace Artemis.KeyboardProviders.Corsair
{
    internal class CorsairRGB : KeyboardProvider
    {
        private CorsairKeyboard _keyboard;
        public CorsairRGB()
        {
            Name = "Corsair RGB Keyboards";
            CantEnableText = "Couldn't connect to your Corsair keyboard.\n" +
                             "Please check your cables and/or drivers (could be outdated) and that Corsair Utility Engine is running.\n\n" +
                             "If needed, you can select a different keyboard in Artemis under settings.";
            KeyboardRegions = new List<KeyboardRegion>();
        }

        public override bool CanEnable()
        {
            // Try for about 10 seconds, in case CUE isn't started yet
            var tries = 0;
            while (tries < 9)
            {
                try
                {
                    if (CueSDK.ProtocolDetails == null)
                        CueSDK.Initialize();
                }
                catch (CUEException e)
                {
                    if (e.Error == CorsairError.ServerNotFound)
                    {
                        tries++;
                        Thread.Sleep(1000);
                        continue;
                    }
                }
                catch (WrapperException)
                {
                    CueSDK.Reinitialize();
                    return true;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Enables the SDK and sets updatemode to manual as well as the color of the background to black.
        /// </summary>
        public override void Enable()
        {
            try
            {
                if (CueSDK.ProtocolDetails == null)
                    CueSDK.Initialize(true);
            }
            catch (WrapperException)
            {
                /*CUE is already initialized*/
            }
            _keyboard = CueSDK.KeyboardSDK;
            switch (_keyboard.DeviceInfo.Model)
            {
                case "K95 RGB":
                    Height = 7;
                    Width = 25;
                    KeyboardRegions.Add(new KeyboardRegion("TopRow", new Point(0, 1), new Point(20, 1)));
                    KeyboardRegions.Add(new KeyboardRegion("NumPad", new Point(21, 2), new Point(25, 7)));
                    KeyboardRegions.Add(new KeyboardRegion("QWER", new Point(5, 3), new Point(8, 3)));
                    break;
                case "K70 RGB":
                    Height = 7;
                    Width = 21;
                    KeyboardRegions.Add(new KeyboardRegion("TopRow", new Point(0, 1), new Point(18, 1)));
                    KeyboardRegions.Add(new KeyboardRegion("NumPad", new Point(17, 2), new Point(21, 7)));
                    KeyboardRegions.Add(new KeyboardRegion("QWER", new Point(2, 3), new Point(5, 3)));
                    break;
                case "K65 RGB":
                    Height = 7;
                    Width = 18;
                    KeyboardRegions.Add(new KeyboardRegion("TopRow", new Point(0, 1), new Point(18, 1)));
                    KeyboardRegions.Add(new KeyboardRegion("NumPad", new Point(17, 2), new Point(20, 7)));
                    KeyboardRegions.Add(new KeyboardRegion("QWER", new Point(2, 3), new Point(5, 3)));
                    break;
                case "STRAFE RGB":
                    Height = 6;
                    KeyboardRegions.Add(new KeyboardRegion("TopRow", new Point(0, 1), new Point(18, 1)));
                    KeyboardRegions.Add(new KeyboardRegion("NumPad", new Point(18, 2), new Point(22, 7)));
                    KeyboardRegions.Add(new KeyboardRegion("QWER", new Point(1, 3), new Point(4, 3)));
                    Width = 22;
                    break;
            }
        }

        public override void Disable()
        {
            CueSDK.Reinitialize();
        }
        
        /// <summary>
        ///     Properly resizes any size bitmap to the keyboard by creating a rectangle whose size is dependent on the bitmap
        ///     size.
        /// </summary>
        /// <param name="bitmap"></param>
        public override void DrawBitmap(Bitmap bitmap)
        {
            var fixedBmp = new Bitmap(bitmap.Width, bitmap.Height);
            using (var g = Graphics.FromImage(fixedBmp))
            {
                g.Clear(Color.Black);
                g.DrawImage(bitmap, 0,0);
            }

            var fixedImage = ImageUtilities.ResizeImage(fixedBmp, Width, Height);
            var brush = new ImageBrush
            {
                Image = fixedImage
            };

            _keyboard.Brush = brush;
            _keyboard.Update();
        }
    }
}

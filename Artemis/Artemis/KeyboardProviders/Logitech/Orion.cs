﻿using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows;
using Artemis.KeyboardProviders.Logitech.Utilities;
using Artemis.Utilities;
using Artemis.Utilities.LogitechDll;
using Point = System.Drawing.Point;

namespace Artemis.KeyboardProviders.Logitech
{
    internal class Orion : KeyboardProvider
    {
        public Orion()
        {
            Name = "Logitech G910 RGB";
            CantEnableText = "Couldn't connect to your Logitech G910.\n" +
                             "Please check your cables and updating the Logitech Gaming Software\n" +
                             "A minimum version of 8.81.15 is required).\n\n" +
                             "If needed, you can select a different keyboard in Artemis under settings.";
            Height = 6;
            Width = 21;
            KeyboardRegions = new List<KeyboardRegion>
            {
                new KeyboardRegion("TopRow", new Point(0, 0), new Point(18, 0)),
                new KeyboardRegion("NumPad", new Point(17, 1), new Point(21, 6)),
                new KeyboardRegion("QWER", new Point(2, 2), new Point(5, 2))
        };
        }

        public override bool CanEnable()
        {
            //if (DllManager.RestoreDll())
            //    RestoreDll();
            int majorNum = 0, minorNum = 0, buildNum = 0;

            LogitechGSDK.LogiLedInit();
            LogitechGSDK.LogiLedGetSdkVersion(ref majorNum, ref minorNum, ref buildNum);
            LogitechGSDK.LogiLedShutdown();

            // Turn it into one long number...
            var version = int.Parse($"{majorNum}{minorNum}{buildNum}");
            return version >= 88115;
        }

        private void RestoreDll()
        {
            MessageBox.Show(
                "Artemis couldn't enable your Logitech keyboard, because the required files are not in place.\n\n" +
                "This happens when you run The Division and shut down Artemis before shutting down The Division\n" +
                "It can be fixed automatically by clicking OK, but to avoid this message in the future please\n" +
                "shut down The Division before shutting down Artemis.\n\n" +
                "Click OK to fix the issue and restart Artemis");

            GeneralHelpers.RunAsAdministrator();
        }

        public override void Enable()
        {
            // Initialize the SDK
            LogitechGSDK.LogiLedInit();
            Thread.Sleep(200);

            LogitechGSDK.LogiLedSaveCurrentLighting();
            LogitechGSDK.LogiLedSetTargetDevice(LogitechGSDK.LOGI_DEVICETYPE_PERKEY_RGB);

            // Disable keys we can't color untill the SDK will allow it
            LogitechGSDK.LogiLedSetLighting(0, 0, 0);
        }

        public override void Disable()
        {
            // Shutdown the SDK
            LogitechGSDK.LogiLedRestoreLighting();
            LogitechGSDK.LogiLedShutdown();
        }

        public override void DrawBitmap(Bitmap bitmap)
        {
            var fixedBmp = new Bitmap(bitmap.Width, bitmap.Height);
            using (var g = Graphics.FromImage(fixedBmp))
            {
                g.Clear(Color.Black);
                g.DrawImage(bitmap, 0, 0);
            }
            LogitechGSDK.LogiLedSetLightingFromBitmap(OrionUtilities.BitmapToByteArray(fixedBmp));
        }
    }
}
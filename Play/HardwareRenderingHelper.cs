using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media;
using NLog;

namespace Play
{
    public static class HardwareRenderingHelper
    {
        static readonly Logger log = LogManager.GetCurrentClassLogger();
        static readonly string[] videoCardBlacklist = { };

        public static bool IsInSoftwareMode { get; private set; }

        public static void DisableHwRenderingForCrapVideoCards()
        {
            if (!String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GH_FORCE_HW_RENDERING")))
            {
                return;
            }

            if (!String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GH_FORCE_SW_RENDERING")))
            {
                EnableSoftwareMode();
                return;
            }

            int osVersion = Environment.OSVersion.Version.Major * 100 + Environment.OSVersion.Version.Minor;
            if (osVersion < 601)
            {
                log.Warn("Hardware acceleration is much more glitchy on OS's earlier than Vista");
                log.Warn("If you believe this isn't the case, set the GH_FORCE_HW_RENDERING environment variable");
                EnableSoftwareMode();
                return;
            }

            var re = new Regex(@"VEN_([0-9A-Z]{4})&DEV_([0-9A-Z]{4})");
            var wmiSearch = new ManagementObjectSearcher("root\\cimv2", "SELECT * FROM win32_videocontroller");
            var items = wmiSearch.Get();
            var venIds = new List<string>();

            foreach (var item in items)
            {
                var val = (string)item.GetPropertyValue("PNPDeviceID");
                var m = re.Match(val);
                if (!m.Success || m.Captures.Count != 3) continue;

                venIds.Add(m.Groups[1].Value);

                var pnpId = String.Format("{0}:{1}", m.Groups[1].Value, m.Groups[2].Value);
                log.Info("Video PNPID: {0}", pnpId);
                if (videoCardBlacklist.Contains(pnpId))
                {
                    log.Warn("Your video card is known to cause graphical glitches, so we have disabled hardware rendering");
                    log.Warn("If you believe this isn't the case, set the GH_FORCE_HW_RENDERING environment variable");
                    EnableSoftwareMode();
                }
            }

            // NB: If the machine has more than one video card by different
            // vendors (i.e. one monitor is driven by an Intel card, one by
            // ATI), glitches happen when the window crosses devices
            if (venIds.Distinct().Count() > 1)
            {
                log.Warn("You appear to have two active video cards by separate manufacturers, so we have disabled hardware rendering");
                log.Warn("This is known to cause graphical issues, but if you want to enable hardware rendering anyways,");
                log.Warn("set the GH_FORCE_HW_RENDERING environment variable.");
            }

            log.Info("Your video card appears to support hardware rendering. If this isn't the case and you see glitches");
            log.Info("set the GH_FORCE_SW_RENDERING environment variable to 1");
        }

        static void EnableSoftwareMode()
        {
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            IsInSoftwareMode = true;
        }
    }
}

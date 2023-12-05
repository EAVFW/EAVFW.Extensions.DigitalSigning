using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EAVFW.Extensions.DigitalSigning
{
    public static class Constants
    {
        public const int SigningProviderInitializing = 0;

        public const int SigningProviderInitialized = 10;

        public const int SigningProviderReady = 50;

        public static class InputTypes
        {
            public const string MultilineText = "MultilineText";
            public const string Text = "Text";
            public const string Date = "Date";
            public const string RTF = "RTF";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silk.WPF.OpenGL
{
    public class RadarConfig
    {
        public const int SECTIONS = 512;
        public const int CELLS = 6000;
        public const float AZI_SPAN = 65536 / (float)SECTIONS;
    }
}

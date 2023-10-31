﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silk.WPF.OpenGL
{
    public class RadarInfoModel
    {
        public RadarInfoModel(int radarId) {
            RadarID = radarId;
        }
        public int RadarID { get; set; }
        public float MapHeight { get; set; } = 90f;
        public float MapWidth { get; set; } = 160f;
        public float MapHeightOffCenter { get; set; } = 0f;
        public float MapWidthOffCenter { get; set; } = 0f;
        public int UIHeight { get; set; } = 1000;
        public int UIWidth { get; set; } = 1600;
        public float RadarMaxDistance { get; set; } = 60.0f;
        public int RealCells { get; set; } = 6000;


        // config
        public double Longitude { get; set; } = 0;
        public double Latitude { get; set; } = 0;
        public double RadarOrientation { get; set; } = 0.0f;

        public bool IsDisplay { get; set; }
    }
}

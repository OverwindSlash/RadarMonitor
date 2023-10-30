using Silk.NET.Direct3D9;
using Silk.NET.OpenGL;
using Silk.WPF.OpenGL.Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silk.WPF.OpenGL
{
    public class RadarOpenGlModel
    {
        private float[] DataArray = null;

        private List<RadarDataReceivedEventArgs> DataList = new List<RadarDataReceivedEventArgs>();
        private object Lock = new object();

        public float MapHeight { get; set; } = 90f;
        public float MapWidth { get; set; } = 160f;
        public float MapHeightOffCenter { get; set; } = 0f;
        public float MapWidthOffCenter { get; set; } = 0f;
        public int UIHeight { get; set; } = 1000;
        public int UIWidth { get; set; } = 1600;
        public float RadarMaxDistance { get; set; } = 60.0f;


        private uint lastId = 0;

        private int _realCells = RadarConfig.CELLS;

        public int RealCells
        {
            get { return _realCells; }
            set
            {
                if (value <= 0)
                {
                    return;
                }
                if (value != _realCells)
                {
                    DataArray = new float[RadarConfig.SECTIONS * (value + RadarConfig.HEAD)];
                    TextureData = new OpenGLSharp.Texture(gl, DataArray, (uint)(value + RadarConfig.HEAD), RadarConfig.SECTIONS, InternalFormat.R32f, PixelFormat.Red, PixelType.Float, TextureUnit);
                    lastId = 0;

                }
                _realCells = value;
            }
        }


        public int IndexOffset { get; private set; } = 0;
        private double _orientation = 0;
        public double RadarOrientation
        {
            get { return _orientation; }
            set
            {
                if (value == _orientation)
                {
                    return;
                }
                _orientation = value;
                IndexOffset = (int)(value / 360 * RadarConfig.SECTIONS);
            }
        }

        public int RadarID { get; set; } 
        public RadarOpenGlModel(int radarID)
        {
            RadarID = radarID;
            TextureUnit = (TextureUnit)(TextureUnit.Texture0 + radarID+1);
            gl = RenderContext.Gl;
            DataArray = new float[RadarConfig.SECTIONS * (RadarConfig.CELLS + RadarConfig.HEAD)];
            TextureData = new OpenGLSharp.Texture(gl, DataArray, (uint)(_realCells + RadarConfig.HEAD), RadarConfig.SECTIONS, InternalFormat.R32f, PixelFormat.Red, PixelType.Float, TextureUnit);

        }

        #region opengl
        private GL gl;
        private TextureUnit TextureUnit = TextureUnit.Texture0;
        private OpenGLSharp.Texture TextureData;

        public void UpdateData(RadarDataReceivedEventArgs e)
        {
            int idx = (RadarConfig.SECTIONS - 1 - (int)(e.Azimuth / 65536.0f * (float)RadarConfig.SECTIONS) - IndexOffset);
            if (idx < 0)
            {
                idx = idx + RadarConfig.SECTIONS;
            }
            else if (idx > RadarConfig.SECTIONS - 1)
            {
                idx -= RadarConfig.SECTIONS;
            }
            e.SectionId = idx;

            //Array.Copy( e.DataArray.ToArray(), 0,  DataArray, idx* (RealCells+1),  e.DataArray.Count);
            lock (Lock)
            {
                DataList.Add(e);
            }
        }

        public void BindTexture()
        {
            TextureData.Bind(TextureUnit);
        }
        public unsafe void UpdateTexture()
        {
            lock (Lock)
            {
                if (DataList.Count > 0)
                {
                    foreach (var item in DataList)
                    {
                        fixed (void* d = &item.DataArray.ToArray()[0])
                        {
                            gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, item.SectionId, (uint)(RealCells + RadarConfig.HEAD), 1, PixelFormat.Red, PixelType.Float, d);

                        }
                    }
                    DataList.Clear();
                }
            }


            //fixed (void* d = &radar.DataArray.ToArray()[0])
            //{
            //    gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (uint)(radar?.RealCells + 1), RadarConfig.SECTIONS, PixelFormat.Red, PixelType.Float, d);
            //}
        }
        #endregion
    }
}

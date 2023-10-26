using CAT240Parser;
using OpenGLSharp;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Silk.WPF.OpenGL.Scene;
public class RadarDataReceivedEventArgs
{
    public int RadarID { get; set; } = 0;
    public int SectionId { get; set; } = 0;
    public int Azimuth { get; set; }
    public List<float> DataArray { get; set; } = new List<float>();

    public RadarDataReceivedEventArgs(int radarId, int azimuth, List<float> data)
    {
        RadarID = radarId;
        this.Azimuth = azimuth;
        this.DataArray = data;
    }
}

public partial class ExampleScene : UserControl
{

    private System.Windows.Media.Color _echoColor = System.Windows.Media.Colors.Green;

    private bool isInitialized = false;

    private BufferObject<float> Vbo;
    private BufferObject<uint> Ebo;
    private VertexArrayObject<float, uint> Vao;
    private OpenGLSharp.Shader Shader;

    private Vector3 CameraPosition = new Vector3(0.0f, 0.0f, 0.5f / MathF.Tan(CameraZoom / 2 / 180 * MathF.PI));
    private Vector3 CameraFront = new Vector3(0.0f, 0.0f, -1.0f);
    private Vector3 CameraUp = Vector3.UnitY;
    private Vector3 CameraRight = Vector3.UnitX;
    private Vector3 CameraDirection = Vector3.Zero;
    private const float CameraZoom = 45f;
    private float heightScale = 0.5f / MathF.Tan(CameraZoom / 2 / 180 * MathF.PI);
    //private float l = 2* 10 * CELLS / 1000f;

    private bool _isDisplay;

    public bool IsDisplay
    {
        get { return _isDisplay; }
        set
        {
            _isDisplay = value;
        }
    }

    private bool radarChanging = false;


    public System.Windows.Media.Color EchoColor
    {
        get => _echoColor;
        set
        {
            _echoColor = value;
        }
    }

    private int fade_duration = 5;

    public int FadeDuration
    {
        get { return fade_duration; }
        set { fade_duration = value; }
    }

    private System.Windows.Media.Imaging.WriteableBitmap _bitmap;

    private Dictionary<int, RadarOpenGlModel> _radarModels = new Dictionary<int, RadarOpenGlModel>();

    public ExampleScene()
    {
        InitializeComponent();

        GLControl.Setting = new Settings()
        {
            MajorVersion = 4,
            MinorVersion = 5,
            OpenGlProfile = Silk.NET.GLFW.OpenGlProfile.Compat
        };
        GLControl.Loaded += OnLoaded;
        GLControl.Render += OnRender;
        GLControl.Start();
    }

    private unsafe void OnLoaded(object sender, RoutedEventArgs e)
    {
        GL gl = RenderContext.Gl;
        gl.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);

        float[] vertices =
            {
              // aPosition--------   aTexCoords
                 0.5f,  0.5f, 0.0f,  1.0f, 1.0f,
                 0.5f, -0.5f, 0.0f,  1.0f, 0.0f,
                -0.5f, -0.5f, 0.0f,  0.0f, 0.0f,
                -0.5f,  0.5f, 0.0f,  0.0f, 1.0f
            };

        uint[] indices =
            {
                0u, 1u, 3u,
                1u, 2u, 3u
            };
        Ebo = new BufferObject<uint>(gl, indices.ToArray(), BufferTargetARB.ElementArrayBuffer);
        Vbo = new BufferObject<float>(gl, vertices.ToArray(), BufferTargetARB.ArrayBuffer);
        Vao = new VertexArrayObject<float, uint>(gl, Vbo, Ebo);

        Vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
        Vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);

        Shader = new OpenGLSharp.Shader(gl, "shaders/vert.shader", "shaders/frag.shader");

        //for (int i = 0; i < 15; i++)
        //{
        //    _radarModels.Add(i, new RadarOpenGlModel(i));

        //}

        _bitmap = new System.Windows.Media.Imaging.WriteableBitmap((int)GLControl.ActualWidth, (int)GLControl.ActualHeight, 92, 92, System.Windows.Media.PixelFormats.Bgra32, null);
        this.ImageView.Source = _bitmap;
    }

    //private const int fps = 25;
    //private int unit = 1000 / fps;
    //private int durationMS = 0;
    private unsafe void OnRender(TimeSpan delta)
    {
        //durationMS += delta.Milliseconds;
        //if (durationMS < unit)
        //{
        //    return;
        //}

        if (!IsDisplay)
        {
            return;
        }
        GL gl = RenderContext.Gl;

        //gl.ClearColor(12.0f/255, 6.0f/255, 66.0f/255, 0.0f);
        gl.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);

        gl.Clear((uint)(ClearBufferMask.ColorBufferBit));



        foreach (var model in _radarModels)
        {
            var radar = model.Value;
            radar.TextureData.Bind(radar.TextureUnit);

            lock (radar.Lock)
            {
                if (radar.DataList.Count>0)
                {
                    foreach (var item in radar.DataList)
                    {
                        fixed (void* d = &item.DataArray.ToArray()[0])
                        {
                            gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, item.SectionId, (uint)(radar?.RealCells + 1), 1, PixelFormat.Red, PixelType.Float, d);

                        }
                    }
                    radar.DataList.Clear();
                }
            }

            //fixed (void* d = &DataArray.ToArray()[0])
            //{
            //    gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (uint)(_realCells + 1), SECTIONS, PixelFormat.Red, PixelType.Float, d);
            //}


            Vao.Bind();
            Shader.Use();

            Shader.SetUniform("uTexture0", model.Key);
            Shader.SetUniform("uSection", RadarConfig.SECTIONS);
            Shader.SetUniform("uCell", (int)radar?.RealCells);

            //Shader.SetUniform("uLastSection", LastSectionId);
            Shader.SetUniform("uFadeDuration", fade_duration);
            var t = DateTime.Now;
            float nowSecond = t.Hour * 60 * 60 * 1000 + t.Minute * 60 * 1000 + t.Second * 1000 + t.Millisecond;
            Shader.SetUniform("uNow", nowSecond);
            Shader.SetUniform("uColor", new Vector3(EchoColor.R / 255.0f, EchoColor.G / 255.0f, EchoColor.B / 255.0f));

            //var model = Matrix4x4.Identity;
            CameraPosition.Z = (float)(radar.MapHeight / 2.0f / radar.RadarMaxDistance * heightScale);
            //Trace.WriteLine($"W:{MapWidth}, H:{MapHeight}, MD:{RadarMaxDistance}, UIW:{UIWidth}, UIH:{UIHeight}, MCW:{MapWidthOffCenter}, MCH:{MapHeightOffCenter} , Scale:{heightScale}");
            CameraPosition.X = -(float)(radar.MapWidthOffCenter / 2.0f / radar.RadarMaxDistance / heightScale * 1.03f);
            CameraPosition.Y = -(float)(radar.MapHeightOffCenter / 2.0f / radar.RadarMaxDistance / heightScale * 1.03f);

            var view = Matrix4x4.CreateLookAt(CameraPosition, CameraPosition + CameraFront, CameraUp);
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(CameraZoom), (float)radar.UIWidth / radar.UIHeight, 0.01f, 100.0f);

            //Shader.SetUniform("uModel", model);
            Shader.SetUniform("uView", view);
            Shader.SetUniform("uProjection", projection);

            gl.DrawElements(GLEnum.Triangles, (uint)6, GLEnum.UnsignedInt, null);

        }
        isInitialized = true;
        //durationMS = 0;

        //_bitmap.Lock();
        //byte* p = (byte*)_bitmap.BackBuffer.ToPointer();
        //gl.ReadPixels(0, 0, (uint)GLControl.ActualWidth, (uint)GLControl.ActualHeight, GLEnum.Bgra, GLEnum.UnsignedByte, p);
        //for (int i = 0; i < (uint)GLControl.ActualWidth * (uint)GLControl.ActualHeight; i++)
        //{
        //    byte* alpha = p + i * 4 + 3;
        //    if (*(p + i * 4 + 2) == (byte)0 && *(p + i * 4 + 1) == (byte)0 && *(p + i * 4) == (byte)0)
        //    {
        //        *(p + i * 4 + 3) = (byte)0;
        //    }
        //    else
        //    {
        //        *alpha = *(alpha - 2) * 2 > 255 ? (byte)255 : (byte)(*(alpha - 2) * 2);
        //        *(alpha - 2) = (byte)255;
        //    }

        //}
        //_bitmap.AddDirtyRect(new Int32Rect(0, 0, (int)GLControl.ActualWidth, (int)GLControl.ActualHeight));
        //_bitmap.Unlock();
    }

    public void OnReceivedRadarData(object sender, RadarDataReceivedEventArgs e)
    {
        //if (!isInitialized)
        //{
        //    return;
        //}
        if (radarChanging)
        {
            return;
        }

        //foreach (var radar in _radarModels)
        //{

        var radar = _radarModels[e.RadarID];
        int idx = (RadarConfig.SECTIONS - 1 - (int)(e.Azimuth / 65536.0f * (float)RadarConfig.SECTIONS) - radar.IndexOffset);
            if (idx < 0)
            {
                idx = idx + RadarConfig.SECTIONS;
            }
            else if (idx > RadarConfig.SECTIONS - 1)
            {
                idx -= RadarConfig.SECTIONS;
            }
            e.SectionId = idx;

            if (radar != null)
            {

                lock (radar.Lock)
                {
                    radar.DataList.Add(e);

                }
            }
        //}

    }
    public void CreateUpdateRadar(RadarInfoModel radarInfo)
    {
        if (_radarModels.Keys.Contains(radarInfo.RadarID))
        {
            //update
            var radar = _radarModels[radarInfo.RadarID];
            radar.UIWidth = radarInfo.UIWidth;
            radar.UIHeight = radarInfo.UIHeight;
            radar.MapWidth = radarInfo.MapWidth;
            radar.MapHeight = radarInfo.MapHeight;
            radar.MapWidthOffCenter = radarInfo.MapWidthOffCenter;
            radar.MapHeightOffCenter = radarInfo.MapHeightOffCenter;
            radar.RadarOrientation = radarInfo.RadarOrientation;
            radar.RadarMaxDistance = radarInfo.RadarMaxDistance;
        }
        else
        {
            // create
            RadarOpenGlModel radar = new RadarOpenGlModel(radarInfo.RadarID);
            radar.UIWidth = radarInfo.UIWidth;
            radar.UIHeight = radarInfo.UIHeight;
            radar.MapWidth = radarInfo.MapWidth;
            radar.MapHeight = radarInfo.MapHeight;
            radar.MapWidthOffCenter = radarInfo.MapWidthOffCenter;
            radar.MapHeightOffCenter = radarInfo.MapHeightOffCenter;
            radar.RadarOrientation = radarInfo.RadarOrientation;
            radar.RadarMaxDistance = radarInfo.RadarMaxDistance;

            _radarModels.Add(radar.RadarID, radar);
        }
    }
}

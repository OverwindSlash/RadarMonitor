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

public partial class RadarScene : UserControl
{
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

    public double ControlOpacity
    {
        get => GLControl.Opacity;
        set => GLControl.Opacity = value;
    }

    private Dictionary<int, RadarOpenGlModel> _radarModels = new Dictionary<int, RadarOpenGlModel>();

    public RadarScene()
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

        string vertexShader = @"#version 330 core
                    layout (location = 0) in
                    vec3 vPos;
                    layout (location = 1) in
                    vec2 vUv;

                    uniform mat4 uView;
                    uniform mat4 uProjection;

                    out vec2 fUv;

                    void main()
                    {
                        //Multiplying our uniform with the vertex position, the multiplication order here does matter.
                        gl_Position = uProjection * uView  * vec4(vPos, 1.0);
                        fUv = vUv;
                    }";
        string fragmentShader = @"#version 330 core
                    in vec2 fUv;

                    uniform sampler2D uTexture0;

                    uniform int uSection;
                    uniform int uCell;
                    uniform vec3 uColor;
                    uniform int uFadeDuration;
                    uniform float uNow;
                    uniform float uEchoRadius;
                    uniform float uEchoThreshold;

                    out vec4 FragColor;

                    const float PI = 3.1415926535897932384626433832795;
                    float GetAngle(float x, float y)
                    {
                        float angle = atan(y, x);
                        if (angle < 0.0f)
                            angle = PI * 2 + angle;

                        return angle;
                    }



                    void main()
                    {
                        float x = fUv.x - 0.5;
                        float y = fUv.y - 0.5;
    
                        float r = sqrt(x * x + y * y);
                        float theta = GetAngle(x, y);
    
                        float range = 2 * PI / uSection;
                        int row = int(theta / range);
                        int col = int(r / (0.5f / uCell));
    
                        // attention: col,row
                        float g = 0.0;
                        if(r< uEchoRadius)
                        {
                            g = texelFetch(uTexture0, ivec2(col, row), 0).r;
                            g = g > uEchoThreshold ? g : 0.0;
                            float prev = texelFetch(uTexture0, ivec2(0, row), 0).r;
                            float delta = uNow - prev;
                            g = (1.0 - delta / float(uFadeDuration * 1000)) * g;
                        }
    
                        FragColor = vec4(uColor, min(g * 2, 1.0));

                    }";

        //Shader = new OpenGLSharp.Shader(gl, "shaders/vert.shader", "shaders/frag.shader");
        Shader = new OpenGLSharp.Shader(gl, vertexShader, fragmentShader,ShaderSrcType.FromString);


    }

    //private const int fps = 1;
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

        gl.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);

        gl.Clear((uint)(ClearBufferMask.ColorBufferBit));

        foreach (var model in _radarModels)
        {
            var radar = model.Value;
            if (!radar.IsDisplay)
            {
                continue;
            }
            radar.BindTexture();
            radar.UpdateTexture();

            Vao.Bind();
            Shader.Use();

            Shader.SetUniform("uTexture0", model.Key +1 );
            Shader.SetUniform("uSection", RadarConfig.SECTIONS);
            Shader.SetUniform("uCell", (int)radar?.RealCells);

            Shader.SetUniform("uFadeDuration", radar.FadingInterval);
            var t = DateTime.Now;
            float nowSecond = t.Hour * 60 * 60 * 1000 + t.Minute * 60 * 1000 + t.Second * 1000 + t.Millisecond;
            Shader.SetUniform("uNow", nowSecond);
            Shader.SetUniform("uColor", new Vector3(radar.ScanlineColor.R / 255.0f, radar.ScanlineColor.G / 255.0f, radar.ScanlineColor.B / 255.0f));

            Shader.SetUniform("uEchoRadius", radar.EchoRadius);
            Shader.SetUniform("uEchoThreshold", radar.EchoThreshold);

            CameraPosition.Z = (float)(radar.MapHeight / 2.0f / radar.RadarMaxDistance * heightScale);
            CameraPosition.X = -(float)(radar.MapWidthOffCenter / 2.0f / radar.RadarMaxDistance / heightScale * 1.03f);
            CameraPosition.Y = -(float)(radar.MapHeightOffCenter / 2.0f / radar.RadarMaxDistance / heightScale * 1.03f);

            var view = Matrix4x4.CreateLookAt(CameraPosition, CameraPosition + CameraFront, CameraUp);
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(CameraZoom), (float)radar.UIWidth / radar.UIHeight, 0.01f, 100.0f);

            Shader.SetUniform("uView", view);
            Shader.SetUniform("uProjection", projection);

            gl.DrawElements(GLEnum.Triangles, (uint)6, GLEnum.UnsignedInt, null);

        }
        //durationMS = 0;

    }

    public void OnReceivedRadarData(object sender, RadarDataReceivedEventArgs e)
    {
        
        var radar = _radarModels[e.RadarID];

        if (radar != null)
        {
            if (radar.IsChanging)
            {
                return;
            }

            radar.UpdateData(e);
        }

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
            radar.RealCells = radarInfo.RealCells;
            radar.IsDisplay = radarInfo.IsDisplay;
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
            radar.IsDisplay = radarInfo.IsDisplay;
            radar.ScanlineColor = radarInfo.ScanlineColor;
            radar.IsFadingEnabled = radarInfo.IsFadingEnabled;
            radar.FadingInterval = radarInfo.FadingInterval;
            radar.EchoThreshold = radarInfo.EchoThreshold;
            radar.EchoRadius = radarInfo.EchoRadius;
            _radarModels.Add(radar.RadarID, radar);
        }
    }

    public void OnDisplayed(int radarId, bool isDisplay)
    {
        var radar = _radarModels[radarId];
        radar.IsDisplay = isDisplay;
    }


    public void OnConfigChanged(RadarInfoModel radarInfo)
    {
        var radar = _radarModels[radarInfo.RadarID];
        if (radar != null)
        {
            radar.ScanlineColor = radarInfo.ScanlineColor;
            radar.IsFadingEnabled = radarInfo.IsFadingEnabled;
            radar.FadingInterval = radarInfo.FadingInterval;
            radar.EchoThreshold = radarInfo.EchoThreshold;
            radar.EchoRadius = radarInfo.EchoRadius;
        }
    }

    public void OnSessionChanged()
    {
        GLControl.OnSessionChanged();
    }
}

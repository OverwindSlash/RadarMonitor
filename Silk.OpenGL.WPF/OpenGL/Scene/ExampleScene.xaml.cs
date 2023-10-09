﻿using CAT240Parser;
using OpenGLSharp;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
//using System.Windows.Media;

namespace Silk.WPF.OpenGL.Scene;
public class RadarDataReceivedEventArgs
{
    public int Id { get; set; } = 0;
    public List<float> DataArray { get; set; } = new List<float>();

    public RadarDataReceivedEventArgs(int id, List<float> data)
    {
        this.Id = id;
        this.DataArray = data;
    }
}

    public partial class ExampleScene : UserControl
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    //uint vao;
    //uint shaderProgram;

    private System.Windows.Media.Color _echoColor = System.Windows.Media.Colors.Green;


    private static uint lastId = 0;
    private const int SECTIONS = 1024;
    private const int CELLS = 6000;
    private const float AZI_SPAN = 65536 / (float)SECTIONS;

    private static bool isInitialized = false;
    private static bool[] IsFilled = new bool[SECTIONS];
    private static int LastSectionId = 0;
    private static float[] DataArray = new float[SECTIONS * CELLS];

    private static List<RadarDataReceivedEventArgs> DataList = new List<RadarDataReceivedEventArgs>();
    private static object lk = new object();

    private BufferObject<float> Vbo;
    private BufferObject<uint> Ebo;
    private VertexArrayObject<float, uint> Vao;
    private OpenGLSharp.Shader Shader;
    private OpenGLSharp.Texture TextureData;

    private Vector3 CameraPosition = new Vector3(0.0f, 0.0f, 0.5f / MathF.Tan(CameraZoom / 2 / 180 * MathF.PI));
    private Vector3 CameraFront = new Vector3(0.0f, 0.0f, -1.0f);
    private Vector3 CameraUp = Vector3.UnitY;
    private Vector3 CameraRight = Vector3.UnitX;
    private Vector3 CameraDirection = Vector3.Zero;
    private const float CameraZoom = 45f;
    private float heightScale = 0.5f / MathF.Tan(CameraZoom / 2 /180 *MathF.PI);
    //private float l = 2* 10 * CELLS / 1000f;

    // properties to be modified by radar monitor viewmodel
    public float MapHeight { get; set; } = 50f;
    public float MapWidth { get; set; }
    public float MapHeightOffCenter { get; set; } = 0f;
    public float MapWidthOffCenter { get; set; } = 0f;
    public int UIHeight { get; set; } = 1000;
    public int UIWidth { get; set; } = 1600;
    //public double RadarOrientation { get; set; } = 0.0;
    public double RadarMaxDistance { get; set; } = 60.0;

    private bool _isDisplay;

    public bool IsDisplay
    {
        get { return _isDisplay; }
        set 
        { 
            _isDisplay = value;
            //Array.Clear(DataArray);
            //TextureData = new OpenGLSharp.Texture(RenderContext.Gl, DataArray, (uint)_realCells, SECTIONS, InternalFormat.R32f, PixelFormat.Red, PixelType.Float, TextureUnit.Texture0);
            //lastId = 0;

        }
    }

    private static bool radarChanging = false;
    private static int _realCells = CELLS;

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
                radarChanging = true;
                DataArray = new float[SECTIONS * value];
                TextureData = new OpenGLSharp.Texture(RenderContext.Gl, DataArray, (uint)value, SECTIONS, InternalFormat.R32f, PixelFormat.Red, PixelType.Float, TextureUnit.Texture0);
                lastId = 0;
                radarChanging = false;

            }
            _realCells = value; 
        }
    }



    public System.Windows.Media.Color EchoColor
    {
        get => _echoColor;
        set
        {
            _echoColor = value;
        }
    }
    private static int IndexOffset = 0;
    private double _orientation;
    public double RadarOrientation
    {
        get { return _orientation; }
        set
        {
            _orientation = value;
            IndexOffset = (int)(value / 360 * SECTIONS);
        }
    }

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

        #region demo

        //float[] vertices = {
        //        -0.5f, -0.5f, 0.0f,
        //         0.5f, -0.5f, 0.0f,
        //         0.0f,  0.5f, 0.0f
        //    };

        //gl.GenBuffers(1, out uint vbo);
        //gl.BindBuffer(GLEnum.ArrayBuffer, vbo);
        //gl.BufferData<float>(GLEnum.ArrayBuffer, (nuint)vertices.Length * sizeof(float), vertices, GLEnum.StaticDraw);

        //gl.GenVertexArrays(1, out vao);
        //gl.BindVertexArray(vao);
        //gl.VertexAttribPointer(0, 3, GLEnum.Float, false, 3 * sizeof(float), null);
        //gl.EnableVertexAttribArray(0);

        //string vertexShaderSource = @"
        //        #version 330 core
        //        layout (location = 0) in vec3 aPos;
        //        void main()
        //        {
        //            gl_Position = vec4(aPos.x, aPos.y, aPos.z, 1.0);
        //        }
        //    ";

        //string fragmentShaderSource = @"
        //        #version 330 core
        //        out vec4 FragColor;
        //        void main()
        //        {
        //            FragColor = vec4(0.0f, 1.0f, 0.0f, 1.0f);
        //        }
        //    ";

        //uint vertexShader = gl.CreateShader(GLEnum.VertexShader);
        //gl.ShaderSource(vertexShader, vertexShaderSource);
        //gl.CompileShader(vertexShader);

        //uint fragmentShader = gl.CreateShader(GLEnum.FragmentShader);
        //gl.ShaderSource(fragmentShader, fragmentShaderSource);
        //gl.CompileShader(fragmentShader);

        //shaderProgram = gl.CreateProgram();
        //gl.AttachShader(shaderProgram, vertexShader);
        //gl.AttachShader(shaderProgram, fragmentShader);
        //gl.LinkProgram(shaderProgram);

        //gl.DeleteShader(vertexShader);
        //gl.DeleteShader(fragmentShader);

        #endregion
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

        TextureData = new OpenGLSharp.Texture(gl, DataArray, (uint)_realCells, SECTIONS, InternalFormat.R32f, PixelFormat.Red, PixelType.Float, TextureUnit.Texture0);


    }

    private const int fps = 25;
    private int unit = 1000/fps;
    private int durationMS = 0;
    private unsafe void OnRender(TimeSpan delta)
    {
        durationMS += delta.Milliseconds;
        if (durationMS < unit )
        {
            return;
        }

        if (!IsDisplay)
        {
            return;
        }
        GL gl = RenderContext.Gl;

        gl.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
        gl.Clear((uint)(ClearBufferMask.ColorBufferBit));


        //gl.UseProgram(shaderProgram);
        //gl.BindVertexArray(vao);
        //gl.DrawArrays(GLEnum.Triangles, 0, 3);


        lock (lk)
        {
            foreach (var item in DataList)
            {
                fixed (void* d = &item.DataArray.ToArray()[0])
                {
                    gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, item.Id, (uint)_realCells, 1, PixelFormat.Red, PixelType.Float, d);

                }
            }
            DataList.Clear();
        }



        //fixed (void* d = &DataArray.ToArray()[0])
        //{
        //    gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, CELLS, SECTIONS, PixelFormat.Red, PixelType.Float, d);
        //}

        Vao.Bind();
        TextureData.Bind(TextureUnit.Texture0);

        Shader.Use();

        Shader.SetUniform("uTexture0", 0);
        Shader.SetUniform("uSection", SECTIONS);
        Shader.SetUniform("uCell", (int)_realCells);

        Shader.SetUniform("uLastSection", LastSectionId);
        Shader.SetUniform("uColor", new Vector3(EchoColor.R / 255.0f, EchoColor.G / 255.0f, EchoColor.B / 255.0f));

        var model = Matrix4x4.Identity;
        CameraPosition.Z = (float)(MapHeight / 2.0f / RadarMaxDistance * heightScale);
        //Trace.WriteLine($"W:{MapWidth}, H:{MapHeight}, MD:{RadarMaxDistance}, UIW:{UIWidth}, UIH:{UIHeight}, MCW:{MapWidthOffCenter}, MCH:{MapHeightOffCenter} , Scale:{heightScale}");
        CameraPosition.X = -(float)(MapWidthOffCenter / 2.0f / RadarMaxDistance / heightScale * 1.03f);
        CameraPosition.Y = -(float)(MapHeightOffCenter / 2.0f / RadarMaxDistance / heightScale * 1.03f);

        var view = Matrix4x4.CreateLookAt(CameraPosition, CameraPosition + CameraFront, CameraUp);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(CameraZoom), (float)UIWidth / UIHeight, 0.01f, 100.0f);

        //Console.WriteLine($"CameraZoom: {CameraZoom}");
        Shader.SetUniform("uModel", model);
        Shader.SetUniform("uView", view);
        Shader.SetUniform("uProjection", projection);


        gl.DrawElements(GLEnum.Triangles, (uint)6, GLEnum.UnsignedInt, null);

        isInitialized = true;

        durationMS = 0;
    }

    public static void OnReceivedCat240DataBlock(object sender, Cat240DataBlock dataBlock)
    {
        if (!isInitialized)
        {
            return;
        }
        if (radarChanging)
        {
            return;
        }
        if (dataBlock.Items.MessageIndex > lastId)
        {
            //Console.WriteLine($"SAzi:{dataBlock.Items.StartAzimuth},MsgId:{dataBlock.Items.MessageIndex}," +
            //             $" ValidC:{dataBlock.Items.ValidCellsInDataBlock}");
            lastId = dataBlock.Items.MessageIndex;

            //ThreadPool.QueueUserWorkItem(new WaitCallback((obj) =>
            //{

            List<float> DataArr = new List<float>((int)_realCells);
            int idx = (SECTIONS - 1 - (int)(dataBlock.Items.StartAzimuth / 65536.0f * (float)SECTIONS) - IndexOffset);
            if (idx < 0)
            {
                idx = idx + SECTIONS;
            }
            else if (idx > SECTIONS - 1)
            {
                idx -= SECTIONS;
            }

            Console.WriteLine($"AziSpan:{idx}");

                for (int i = 0; i < dataBlock.Items.ValidCellsInDataBlock; i++)
                {
                    var color = (float)dataBlock.Items.GetCellData(i) / 255;
                    DataArr.Add(color);
                    DataArray[idx * _realCells + i] = color;
                }


            var e = new RadarDataReceivedEventArgs(idx, DataArr);

            lock (lk)
            {
                DataList.Add(e);
                LastSectionId = e.Id;
                IsFilled[e.Id] = true;
                // fill up 
                for (int i = e.Id - 1; i >= e.Id - 3 && i > 0; i--)
                {
                    if (e.Id > 1 && !IsFilled[i])
                    {
                        //Array.Copy(DataMat, (i + 1) * CELLS, DataMat, i * CELLS, CELLS);
                        var item = new RadarDataReceivedEventArgs(i, e.DataArray);
                        DataList.Add(item);
                        LastSectionId = i;
                        IsFilled[i] = true;
                    }
                }
            }

            //}));

        }
    }
}

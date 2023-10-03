﻿//using OpenTK.Windowing.Common;
using Silk.NET.GLFW;
using System.Diagnostics.CodeAnalysis;

namespace Silk.WPF.OpenGL;

public class Settings
{
    public int MajorVersion { get; set; } = 3;

    public int MinorVersion { get; set; } = 3;

    //public ContextFlags GraphicsContextFlags { get; set; } = ContextFlags.Default;

    //public ContextProfile GraphicsProfile { get; set; } = ContextProfile.Core;

    public OpenGlProfile OpenGlProfile { get; set; } = OpenGlProfile.Core;

    //public IGraphicsContext ContextToUse { get; set; }

    public static bool WouldResultInSameContext([NotNull] Settings a, [NotNull] Settings b)
    {
        if (a.MajorVersion != b.MajorVersion)
        {
            return false;
        }

        if (a.MinorVersion != b.MinorVersion)
        {
            return false;
        }

        //if (a.GraphicsProfile != b.GraphicsProfile)
        //{
        //    return false;
        //}

        //if (a.GraphicsContextFlags != b.GraphicsContextFlags)
        //{
        //    return false;
        //}

        return true;

    }
}

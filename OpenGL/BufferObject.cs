using Silk.NET.OpenGL;
using System;

namespace OpenGLSharp
{
    public class BufferObject<TDataType> : IDisposable
        where TDataType : unmanaged
    {
        private uint _handle;
        private BufferTargetARB _bufferType;
        private GL _gl;

        public unsafe BufferObject(GL gl, Span<TDataType> data, BufferTargetARB bufferType)
        {
            _gl = gl;
            _bufferType = bufferType;

            _handle = _gl.GenBuffer();
            Bind();
            fixed (void* d = data)
            {
                _gl.BufferData(bufferType, (nuint) (data.Length * sizeof(TDataType)), d, BufferUsageARB.StaticDraw);
            }
        }
        public unsafe BufferObject(GL gl, Span<TDataType> data, uint size, BufferTargetARB bufferType, BufferUsageARB bufferUsage)
        {
            _gl = gl;
            _bufferType = bufferType;

            _handle = _gl.GenBuffer();
            Bind();
            fixed (void* d = data)
            {
                _gl.BufferData(bufferType, size, d, bufferUsage);
            }

        }

        public unsafe void BufferSubData(BufferTargetARB bufferType, Span<TDataType> data, int offset)
        {
            fixed (void* d = data)
            {
                _gl.BufferSubData(bufferType, (nint)offset, (nuint)(data.Length * sizeof(TDataType)), d);
            }
        }

        public void Bind()
        {
            _gl.BindBuffer(_bufferType, _handle);
        }

        public void Dispose()
        {
            _gl.DeleteBuffer(_handle);
        }
    }
}

using System;
using System.Runtime.InteropServices;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Exercise2
{
    public class ObjMesh
    {
        public ObjMesh(string fileName)
        {
            ObjMeshLoader.Load(this, fileName);
        }

        public ObjVertex[] Vertices
        {
            get { return vertices; }
            set { vertices = value; }
        }
        ObjVertex[] vertices;

        public ObjTriangle[] Triangles
        {
            get { return triangles; }
            set { triangles = value; }
        }
        ObjTriangle[] triangles;

        int verticesBufferId;
        int trianglesBufferId;
        int vaoHandle = 0;

        public void Prepare()
        {
            if (vaoHandle > 0)
                return;

            if (verticesBufferId == 0)
            {
                GL.GenBuffers(1, out verticesBufferId);
                GL.BindBuffer(BufferTarget.ArrayBuffer, verticesBufferId);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * Marshal.SizeOf(typeof(ObjVertex))), vertices, BufferUsageHint.StaticDraw);
            }

            if (trianglesBufferId == 0)
            {
                GL.GenBuffers(1, out trianglesBufferId);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, trianglesBufferId);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(triangles.Length * Marshal.SizeOf(typeof(ObjTriangle))), triangles, BufferUsageHint.StaticDraw);
            }

            vaoHandle = GL.GenVertexArray();
            GL.BindVertexArray(vaoHandle);

            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, verticesBufferId);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(ObjVertex)), Vector3.SizeInBytes+Vector2.SizeInBytes);

            GL.EnableVertexAttribArray(1);
            GL.BindBuffer(BufferTarget.ArrayBuffer, verticesBufferId);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(ObjVertex)), Vector2.SizeInBytes);

            GL.EnableVertexAttribArray(2);
            GL.BindBuffer(BufferTarget.ArrayBuffer, verticesBufferId);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(ObjVertex)), 0);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, trianglesBufferId);

            GL.BindVertexArray(0);
        }

        public void Render()
        {
            Prepare();

            GL.BindVertexArray(vaoHandle);

            GL.DrawElements(PrimitiveType.Triangles, triangles.Length * 3, DrawElementsType.UnsignedInt, IntPtr.Zero);

            GL.BindVertexArray(0);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ObjVertex
        {
            public Vector2 TexCoord;
            public Vector3 Normal;
            public Vector3 Vertex;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ObjTriangle
        {
            public int Index0;
            public int Index1;
            public int Index2;
        }
        
    }

}
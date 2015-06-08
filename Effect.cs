using System;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Exercise2
{
    public class Effect : GameWindow
    {
        #region Shader code

        string vertexShaderSource = @"
#version 330

precision highp float;

uniform mat4 projection_matrix;
uniform mat4 modelview_matrix;
uniform float elapsed_time;
uniform vec3 emitter_position;

in vec3 in_point;
in float in_time;

out float time;
out vec4 color;

void main(void)
{  
    gl_PointSize = 4.0;

    float time = max(elapsed_time - in_time, 0.0);
    time = mod(time , 10);
    vec3 position = emitter_position;
    vec3 gravity = vec3(0f, -1f, 0f);
    position += in_point * time + 0.2f * gravity * time * time;
    color = vec4(1f - time / 15f,  1f - time / 4, 0.4 - time / 4, 1f - time / 8);
    gl_Position = projection_matrix * modelview_matrix * vec4(position, 1);  
}";

        string fragmentShaderSource = @"
#version 330

precision highp float;

in float time;
in vec4 color;
out vec4 frag_color;

void main(void)
{ 
  frag_color = color;
}";

        #endregion

        #region Member variables

        int vertexShaderHandle,
            fragmentShaderHandle,
            shaderProgramHandle,
            modelviewMatrixLocation,
            projectionMatrixLocation,
            texture,
            pointsVBOhandle,
            timesVBOhandle,
            VAOHandle,
            totalTimeLocation,
            emitterPositionLocation;

        const int particleCount = 2000;

        Matrix4 projectionMatrix, viewMatrix;

        ObjMesh objMesh;

        float frame_counter = 0f;

        #endregion 

        #region Constructor of window
        public Effect()
            : base(800, 600,
            new GraphicsMode(), "Tutorial: OpenGL 3.3", 0,
            DisplayDevice.Default, 3, 3,
            GraphicsContextFlags.ForwardCompatible | GraphicsContextFlags.Debug)
        { }
        #endregion

        #region Initialization of rendering
        // Called after the GL context was created, but before entering main loop
        protected override void OnLoad(System.EventArgs e)
        {
            // Enable VSync to release CPU time during the render loop
            VSync = VSyncMode.On;

            // Set up general GL states
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(0f,0f,0f,1.0f);
            GL.Enable(EnableCap.ProgramPointSize);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            // Set up shaders
            CreateShaders();

            // Create objects to be rendered
            CreateObjects();

            Console.WriteLine("Effect " + GL.GetError().ToString());
        }


        void CreateShaders()
        {
            // Create shader
            vertexShaderHandle = GL.CreateShader(ShaderType.VertexShader);
            fragmentShaderHandle = GL.CreateShader(ShaderType.FragmentShader);

            GL.ShaderSource(vertexShaderHandle, vertexShaderSource);
            GL.ShaderSource(fragmentShaderHandle, fragmentShaderSource);

            GL.CompileShader(vertexShaderHandle);
            GL.CompileShader(fragmentShaderHandle);

            Console.WriteLine(GL.GetShaderInfoLog(vertexShaderHandle));
            Console.WriteLine(GL.GetShaderInfoLog(fragmentShaderHandle));

            shaderProgramHandle = GL.CreateProgram();

            GL.AttachShader(shaderProgramHandle, vertexShaderHandle);
            GL.AttachShader(shaderProgramHandle, fragmentShaderHandle);

            GL.BindAttribLocation(shaderProgramHandle, 0, "in_point");
            GL.BindAttribLocation(shaderProgramHandle, 1, "in_time");
            //GL.BindAttribLocation(shaderProgramHandle, 2, "in_tex");            

            GL.LinkProgram(shaderProgramHandle);
            Console.WriteLine(GL.GetProgramInfoLog(shaderProgramHandle));
            GL.UseProgram(shaderProgramHandle);

            // Set uniforms
            projectionMatrixLocation = GL.GetUniformLocation(shaderProgramHandle, "projection_matrix");
            modelviewMatrixLocation = GL.GetUniformLocation(shaderProgramHandle, "modelview_matrix");
            totalTimeLocation = GL.GetUniformLocation(shaderProgramHandle, "elapsed_time");
            emitterPositionLocation = GL.GetUniformLocation(shaderProgramHandle, "emitter_position");

            // Set projection matrix
            GL.Viewport(0, 0, Width, Height);
            float aspect = (float)Width / (float)Height;
            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(0.7f, aspect, 0.1f, 100.0f);
            GL.UniformMatrix4(projectionMatrixLocation, false, ref projectionMatrix);

            // For the start, set modelview matrix of shader to view matrix of opengl camera
            viewMatrix = Matrix4.LookAt(new Vector3(-5.0f, 0f, -5.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f));
            GL.UniformMatrix4(modelviewMatrixLocation, false, ref viewMatrix);


            GL.Uniform1(totalTimeLocation, frame_counter);
            //GL.Uniform1(totalTimeLocation, (float)Process.GetCurrentProcess().TotalProcessorTime.Milliseconds);
            GL.Uniform3(emitterPositionLocation, new Vector3(0f, 0f, 0f));

            // Set texture image location in shader to texture unit 0. We can do this, because
            // we will only use at most one texture per geometry. This texture is always bound 
            // to unit 0.
            //int texImageLoc = GL.GetUniformLocation(shaderProgramHandle, "texImage");
            //GL.Uniform1(texImageLoc, 0);
        }

        //int CreateTexture(String textureName)
        //{
            // Load bitmap
            //Bitmap bitmap = new Bitmap(textureName);
            // Flip image to match opengl tex coordinate system (0,0)->bottom left
            //bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

            // Load texture
            //int textureId;
            //GL.GenTextures(1, out textureId);
            //GL.BindTexture(TextureTarget.Texture2D, textureId);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            //BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
            //    ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            //GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
            //    OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            //bitmap.UnlockBits(data);

            //GL.BindTexture(TextureTarget.Texture2D, 0);

            //return textureId;
        //}

        void CreateObjects()
        {
            // Load obj mesh (does not support material files)
            //objMesh = new ObjMesh("../../plane.obj");

            // Load the texture of the mesh separately
            //texture = CreateTexture("../../plane.png");
            Vector3[] points = new  Vector3[particleCount];
            float[] initialTimes = new float[particleCount];
            float totalTime = 0f;
            Random rand = new Random();
            for (int i = 0; i < particleCount; i++) {
                points[i] = new Vector3(((float)rand.Next(0, 101)) / 100 - 0.5f, 1f, ((float)rand.Next(0, 101)) / 100 - 0.5f);
                initialTimes[i] = totalTime;
                totalTime += 0.005f;
            }

            pointsVBOhandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, pointsVBOhandle);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(points.Length * Vector3.SizeInBytes),
                points, BufferUsageHint.StaticDraw);

            timesVBOhandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, timesVBOhandle);
            GL.BufferData<float>(BufferTarget.ArrayBuffer,
                new IntPtr(initialTimes.Length * Vector3.SizeInBytes),
                initialTimes, BufferUsageHint.StaticDraw);

            VAOHandle = GL.GenVertexArray();
            GL.BindVertexArray(VAOHandle);

            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, pointsVBOhandle);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);

            GL.EnableVertexAttribArray(1);
            GL.BindBuffer(BufferTarget.ArrayBuffer, timesVBOhandle);
            GL.VertexAttribPointer(1, 1, VertexAttribPointerType.Float, false, sizeof(float), 0);

            
        }

        #endregion

        #region Release GL resources
        protected override void OnUnload(EventArgs e)
        {
            
        }
        #endregion

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            // Handle input
            var keyboard = OpenTK.Input.Keyboard.GetState();
            if (keyboard[OpenTK.Input.Key.Escape])
                Exit();
        }

        Matrix4 rotMat = Matrix4.Identity;
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            //// Scale cube
            //Matrix4 modelviewMatrix = Matrix4.CreateScale(4.0f, 4.0f, 4.0f) * viewMatrix;
            //GL.UniformMatrix4(modelviewMatrixLocation, false, ref modelviewMatrix);

            //GL.ActiveTexture(TextureUnit.Texture0);
            //GL.BindTexture(TextureTarget.Texture2D, texture);

            //objMesh.Render();

            //GL.BindTexture(TextureTarget.Texture2D, 0);
            //GL.Uniform1(totalTimeLocation, (float)Process.GetCurrentProcess().TotalProcessorTime.Milliseconds);


            rotMat = Matrix4.CreateRotationY((float)-Math.Sin(e.Time) / 100f) * rotMat;

            if (frame_counter >= float.MaxValue - 1 || frame_counter < 0)
                frame_counter = 0;
            frame_counter += 0.1f;

           // viewMatrix *= rotMat;
                
            //GL.UniformMatrix4(modelviewMatrixLocation, false, ref viewMatrix);

            GL.Uniform1(totalTimeLocation, frame_counter);
            
            //GL.BindVertexArray(VAOHandle);
            GL.DrawArrays(PrimitiveType.Points, 0, particleCount);

            //
            SwapBuffers();
        }

        //[STAThread]
        public static void Main()
        {
            using (Effect example = new Effect())
            {
                example.Title = "FH Salzburg | OpenGL 3 Tutorial";
                //example.Icon = OpenTK.Examples.Properties.Resources.Game;
                example.Run(30, 30);
            }
        }
    }
}
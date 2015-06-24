using System;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

using OpenTK;
using OpenTK.Input;
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

//How the shader works
//Every particle has an initial movement vector v0 and a spawnTime.
//Particles are only drawn once their lifetime (totalTime - spawnTime) is non-negative.
//Then their position is calculated via an equation of motion (x = v0 * lt + gravity * lt^2), which is added
//onto the emitter position, from which all particles originate
//This results in the position where the vertex is drawn
//The color of the vertex is dependent on its lifetime and fades from bright yellow to dark red. After a certain
//time is reached, the particles opacity reaches 0 and it is not visible anymore

uniform mat4 projection_matrix;
uniform mat4 modelview_matrix;
uniform vec3 gravity;
uniform float elapsed_time;
uniform vec3 emitter_position;

in vec3 in_point;
in float in_time;

out float time;
out vec4 color;

void main(void)
{  
    gl_PointSize = 3.0;

    float time = max(elapsed_time - in_time, 0.0);
    time = mod(time , 10); //loops the time from 0 to 10
    vec3 position = emitter_position;
    //vec3 gravity = vec3(-1f, -1f, 0f);
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
            emitterPositionLocation,
            gravityVectorPosition;

        const int particleCount = 2000;

        Matrix4 projectionMatrix, viewMatrix;

        ObjMesh objMesh;

        Vector3 gravityVec = new Vector3(0f, -1f, 0f);

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

            // Set up general GL states + alpha blending + variable point size
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
            emitterPositionLocation = GL.GetUniformLocation(shaderProgramHandle, "emitter_position"); //particle origin
            gravityVectorPosition = GL.GetUniformLocation(shaderProgramHandle, "gravity");

            // Set projection matrix
            GL.Viewport(0, 0, Width, Height);
            float aspect = (float)Width / (float)Height;
            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(0.7f, aspect, 0.1f, 100.0f);
            GL.UniformMatrix4(projectionMatrixLocation, false, ref projectionMatrix);

            // For the start, set modelview matrix of shader to view matrix of opengl camera
            viewMatrix = Matrix4.LookAt(new Vector3(-5.0f, 0f, -5f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f));
            GL.UniformMatrix4(modelviewMatrixLocation, false, ref viewMatrix);


            GL.Uniform1(totalTimeLocation, frame_counter);
            //GL.Uniform1(totalTimeLocation, (float)Process.GetCurrentProcess().TotalProcessorTime.Milliseconds);
            GL.Uniform3(emitterPositionLocation, new Vector3(0f, 0f, 0f));
            GL.Uniform3(gravityVectorPosition, gravityVec);

        }


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
            //Every particle recieves a pseudo-random motion vector ([-0.3,0.3], [0.7, 1], [-0.3,0.3]) which is used to calculate the
            //particles movement over time. It also recievs an initialTime, which decides how long it takes for each particle to spawn.
            //This time is increased after each particle so as to create an interval
            for (int i = 0; i < particleCount; i++) {
                //points[i] = new Vector3(((float)rand.Next(0, 101)) / 100 - 0.5f, 1f, ((float)rand.Next(0, 101)) / 100 - 0.5f);
                points[i] = new Vector3(((float)rand.Next(0, 61)) / 100 - 0.3f, (float)rand.Next(70, 101) / 100, ((float)rand.Next(0, 61)) / 100 - 0.3f);
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

            MouseState mouse = Mouse.GetState();
      
           // Console.WriteLine((((float)this.Mouse.X / this.Width) - 0.5) * 2);
            //Console.WriteLine(((float)this.Mouse.Y / this.Height - 0.5) * 2);
            //Take mouse position and transform coordinates to have their center in the middle of the screen, reaching from -1 to 1
            double y = (((double)this.Mouse.Y / this.Height) - 0.5) * -2; 
            double x = (((double)this.Mouse.X / this.Width) - 0.5) * -2;
            gravityVec = new Vector3((float)x, (float)y - 1, 0);
            GL.Uniform3(gravityVectorPosition, gravityVec);

            //rotMat = Matrix4.CreateRotationY((float)-Math.Sin(e.Time) / 100f) * rotMat;

            //Set emitter position to origin of left click
            if (mouse[MouseButton.Left]) {
                GL.Uniform3(emitterPositionLocation, new Vector3((float)x*5, (float)y*5, 0));
            }

            //Increase time passed after each frame
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
                example.Title = "Left Click to change emitter position";
                //example.Icon = OpenTK.Examples.Properties.Resources.Game;
                example.Run(30, 30);
            }
        }
    }
}
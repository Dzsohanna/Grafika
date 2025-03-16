using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Szeminarium1
{
    internal static class Program
    {
        private static IWindow graphicWindow;

        private static GL Gl;

        private static uint program;

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;

		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;
            gl_Position = vec4(vPos.x, vPos.y, vPos.z, 1.0); 
        }
        ";
        //hianyzok egy vesszo
        // Vertex shader failed to compile: ERROR: 0:11: '1.0' : syntax error syntax error

        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
		
		in vec4 outCol;

        void main()
        {
            FragColor = outCol;
        }
        ";
        //rossz GLSL verzio van megadva ami nem letezik ezert nem fog lefordulni
        //Error linking shader Attached fragment shader is not compiled.

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "1. szeminárium - háromszög";
            windowOptions.Size = new Silk.NET.Maths.Vector2D<int>(500, 500);

            graphicWindow = Window.Create(windowOptions);

            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;

            graphicWindow.Run();
        }

        private static void GraphicWindow_Load()
        {
            // egszeri beallitasokat
            //Console.WriteLine("Loaded");

            Gl = graphicWindow.CreateOpenGL();

            Gl.ClearColor(System.Drawing.Color.White);

            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
           // Gl.CompileShader(fshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));


            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);
            //Gl.CompileShader(vshader);
            //ha kicserelem a ket compilet akkor az expection lesz kiirva mert nem lesz compilalva a vshader

            program = Gl.CreateProgram();
            //Gl.AttachShader(program, fshader); //semmilyen hiba nem lesz
            //Gl.LinkProgram(program); //Error linking shader Link called without any attached shader objects.
            Gl.AttachShader(program, vshader);
            //Gl.LinkProgram(program); //Fekete lesz a szin mert az szin nem lesz hozzaadva
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);

            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }

        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            // NO GL
            // make it threadsave
            //Console.WriteLine($"Update after {deltaTime} [s]");
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s]");

            Gl.Clear(ClearBufferMask.ColorBufferBit);

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            float[] vertexArray = new float[] {
                -0.5f, -0.5f, 0.0f, //kikommentezve
                +0.5f, -0.5f, 0.0f,
                 0.0f, +0.5f, 0.0f,
                 1f, 1f, 0f
            };

            float[] colorArray = new float[] {
                1.0f, 0.0f, 0.0f, 1.0f, //piros //kikommentezve
                0.0f, 1.0f, 0.0f, 1.0f, //zold 
                0.0f, 0.0f, 1.0f, 1.0f, //kek
                1.0f, 0.0f, 0.0f, 1.0f, //piros
            };

            uint[] indexArray = new uint[] { 
                //0, 1, 3, //felcsereles 
                //0, 2, 3
                0, 1, 2,
                2, 1, 3
            };

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices); //komment

            /*
            var error = Gl.GetError();
            if (error != GLEnum.NoError)
            {
                Console.WriteLine($"{error}");
                //hiba lesz mert nem lesz kijelolve hogy melyik bufferrel akarunk majd dolgozni es nem leszs ahova betoltse az adatokat
            }*/

            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
            Gl.EnableVertexAttribArray(0); //0
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(0); //0
            //ha mind a kettot kiveszem fekete lesz a 3-szog ha csak egyet akkor okes

            /*var error = Gl.GetError();
            if (error != GLEnum.NoError)
            {
                Console.WriteLine($"{error}");
                //hiba mert a shaderben nincs megfelelo layout(location=2) ezert nem talalja azt
            }*/

            uint colors = Gl.GenBuffer();

            //Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            /*var error = Gl.GetError();
            if (error != GLEnum.NoError)
            {
                Console.WriteLine($"{error}");
                //hiba lesz mert az adatokat tovabbra is a vertices bufferbe tolti be nem a colorsba 
            }*/
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors); //sor csere
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1); //1
            //ha mind a kettot kiveszem fekete lesz a 3-szog ha csak egyet akkor okes 

            /*var error1 = Gl.GetError();
            if (error1 != GLEnum.NoError)
            {
                Console.WriteLine($"{error1}");
                //hiba mert a shaderben nincs megfelelo layout(location=3) ezert nem talalja azt
            }*/

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);

            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            Gl.UseProgram(program);
            
            Gl.DrawElements(GLEnum.Triangles, (uint)indexArray.Length, GLEnum.UnsignedInt, null); // we used element buffer
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(vao);

            // always unbound the vertex buffer first, so no halfway results are displayed by accident
            Gl.DeleteBuffer(vertices);
            Gl.DeleteBuffer(colors);
            Gl.DeleteBuffer(indices);
            Gl.DeleteVertexArray(vao);
        }
    }
}

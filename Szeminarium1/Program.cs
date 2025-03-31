using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Drawing;

namespace GrafikaSzeminarium
{
    internal class Program
    {
        private static IWindow graphicWindow;
        private static GL Gl;
        private static List<ModelObjectDescriptor> rubiksCubePieces = new List<ModelObjectDescriptor>();
        private static CameraDescriptor camera = new CameraDescriptor();

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string RotationMatrixVariableName = "uRotation";  
                 private static bool isAnimating = false;
        private static float currentRotationAngle = 0f;
        private static float rotationDirection = 1f;          private static float rotationSpeed = 180f;          private static List<int> piecesToRotateIndices = new List<int>();
        private static char rotationAxis = 'y';          private static int sliceIndex = 1;  
                 private static Dictionary<int, Dictionary<Direction, Color>> cubeFaceColors = new Dictionary<int, Dictionary<Direction, Color>>();

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
        layout (location = 1) in vec4 vCol;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;
        uniform mat4 uRotation;  
        out vec4 outCol;
        
        void main()
        {
            outCol = vCol;
            gl_Position = uProjection * uView * uRotation * uModel * vec4(vPos.x, vPos.y, vPos.z, 1.0);
        }
        ";

        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
        
        in vec4 outCol;

        void main()
        {
            FragColor = outCol;
        }
        ";

        private static uint program;

        private static readonly Color WHITE = Color.White;
        private static readonly Color YELLOW = Color.Yellow;
        private static readonly Color RED = Color.Red;
        private static readonly Color ORANGE = Color.Orange;
        private static readonly Color BLUE = Color.Blue;
        private static readonly Color GREEN = Color.Green;
        private static readonly Color BLACK = Color.Black;

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Grafika szeminárium";
            windowOptions.Size = new Vector2D<int>(800, 800);

            graphicWindow = Window.Create(windowOptions);

            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;
            graphicWindow.Closing += GraphicWindow_Closing;

            graphicWindow.Run();
        }

        private static void GraphicWindow_Closing()
        {
            foreach (var piece in rubiksCubePieces)
            {
                piece.Dispose();
            }
            Gl.DeleteProgram(program);
        }

        private static void GraphicWindow_Load()
        {
            Gl = graphicWindow.CreateOpenGL();

            var inputContext = graphicWindow.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            camera.DecreaseZYAngle();
            camera.IncreaseZXAngle();
            camera.IncreaseDistance();
            camera.IncreaseDistance();

            SetupShaderProgram();

            CreateRubiksCube();

            Gl.ClearColor(System.Drawing.Color.Beige);

            Gl.Enable(EnableCap.CullFace);
            Gl.CullFace(TriangleFace.Back);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        private static void SetupShaderProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed to compile: " + Gl.GetShaderInfoLog(fshader));

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
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

        private static void CreateRubiksCube()
        {
            float cubeSize = 0.25f;
            float gap = 0.01f;
            float offset = cubeSize + gap;
            int pieceIndex = 0;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        Dictionary<Direction, Color> faceColors = new Dictionary<Direction, Color>
                        {
                            { Direction.Top, y == 1 ? WHITE : BLACK },
                            { Direction.Bottom, y == -1 ? YELLOW : BLACK },
                            { Direction.Left, x == -1 ? ORANGE : BLACK },
                            { Direction.Right, x == 1 ? RED : BLACK },
                            { Direction.Front, z == 1 ? GREEN : BLACK },
                            { Direction.Back, z == -1 ? BLUE : BLACK }
                        };

                        var cube = RubiksCubeFactory.CreateSmallCube(Gl, faceColors);
                        rubiksCubePieces.Add(cube);
                        cube.Position = new Vector3D<float>(x * offset, y * offset, z * offset);

                                                 cubeFaceColors[pieceIndex] = new Dictionary<Direction, Color>(faceColors);
                        cube.Index = pieceIndex;
                        pieceIndex++;
                    }
                }
            }
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Left:
                    camera.DecreaseZYAngle();
                    break;
                case Key.Right:
                    camera.IncreaseZYAngle();
                    break;
                case Key.Down:
                    camera.IncreaseDistance();
                    break;
                case Key.Up:
                    camera.DecreaseDistance();
                    break;
                case Key.U:
                    camera.IncreaseZXAngle();
                    break;
                case Key.D:
                    camera.DecreaseZXAngle();
                    break;
                case Key.Space:
                    if (!isAnimating)
                    {
                        StartRotation('y', 1, 1f);                      
                    }
                    break;
                case Key.Backspace:
                    if (!isAnimating)
                    {
                        StartRotation('y', 1, -1f);                      
                    }
                    break;
            }
        }

        private static void StartRotation(char axis, int slice, float direction)
        {
            if (isAnimating)
                return;

            isAnimating = true;
            currentRotationAngle = 0f;
            rotationDirection = direction;
            rotationAxis = axis;
            sliceIndex = slice;
            piecesToRotateIndices.Clear();

            for (int i = 0; i < rubiksCubePieces.Count; i++)
            {
                var piece = rubiksCubePieces[i];
                bool shouldRotate = false;

                switch (axis)
                {
                    case 'x':
                        shouldRotate = Math.Abs(piece.Position.X - slice * (0.26f)) < 0.01f;
                        break;
                    case 'y':
                        shouldRotate = Math.Abs(piece.Position.Y - slice * (0.26f)) < 0.01f;
                        break;
                    case 'z':
                        shouldRotate = Math.Abs(piece.Position.Z - slice * (0.26f)) < 0.01f;
                        break;
                }

                if (shouldRotate)
                {
                    piecesToRotateIndices.Add(i);
                }
            }
        }

        private static void FinishRotation()
        {
            if (!isAnimating)
                return;

            isAnimating = false;
            currentRotationAngle = 0f;

            foreach (int pieceIndex in piecesToRotateIndices)
            {
                var piece = rubiksCubePieces[pieceIndex];

                                 Vector3D<float> rotatedPosition = RotatePoint(piece.Position);
                piece.Position = rotatedPosition;

                                 UpdateFaceColors(pieceIndex);
            }

                         RecreateCubePieces();

            piecesToRotateIndices.Clear();
        }

        private static void UpdateFaceColors(int pieceIndex)
        {
            var faceColors = new Dictionary<Direction, Color>(cubeFaceColors[pieceIndex]);
            var newFaceColors = new Dictionary<Direction, Color>();

            if (rotationAxis == 'y' && sliceIndex == 1)
            {
                newFaceColors[Direction.Top] = faceColors[Direction.Top];
                newFaceColors[Direction.Bottom] = faceColors[Direction.Bottom];

                if (rotationDirection > 0)
                {
                    newFaceColors[Direction.Front] = faceColors[Direction.Left];
                    newFaceColors[Direction.Left] = faceColors[Direction.Back];
                    newFaceColors[Direction.Back] = faceColors[Direction.Right];
                    newFaceColors[Direction.Right] = faceColors[Direction.Front];
                }
                else
                {
                    newFaceColors[Direction.Front] = faceColors[Direction.Right];
                    newFaceColors[Direction.Right] = faceColors[Direction.Back];
                    newFaceColors[Direction.Back] = faceColors[Direction.Left];
                    newFaceColors[Direction.Left] = faceColors[Direction.Front];
                }
            }
            else
            {
                foreach (var kvp in faceColors)
                {
                    newFaceColors[kvp.Key] = kvp.Value;
                }
            }

            cubeFaceColors[pieceIndex] = newFaceColors;
        }

        private static void RecreateCubePieces()
        {
            var newCubePieces = new List<ModelObjectDescriptor>();

            for (int i = 0; i < rubiksCubePieces.Count; i++)
            {
                var piece = rubiksCubePieces[i];
                if (!cubeFaceColors.ContainsKey(piece.Index))
                {
                    cubeFaceColors[piece.Index] = new Dictionary<Direction, Color>
                    {
                        { Direction.Top, BLACK },
                        { Direction.Bottom, BLACK },
                        { Direction.Left, BLACK },
                        { Direction.Right, BLACK },
                        { Direction.Front, BLACK },
                        { Direction.Back, BLACK }
                    };
                }
                var cube = RubiksCubeFactory.CreateSmallCube(Gl, new Dictionary<Direction, Color>(cubeFaceColors[piece.Index]));
                cube.Position = piece.Position;
                cube.Index = piece.Index;
                newCubePieces.Add(cube);
            }

            foreach (var piece in rubiksCubePieces)
            {
                piece.Dispose();
            }

            rubiksCubePieces = newCubePieces;
        }

        private static Vector3D<float> RotatePoint(Vector3D<float> point)
        {
            Matrix4X4<float> rotation;
            float angle = MathF.PI / 2 * rotationDirection; 

            switch (rotationAxis)
            {
                case 'x':
                    rotation = Matrix4X4.CreateRotationX(angle);
                    break;
                case 'y':
                    rotation = Matrix4X4.CreateRotationY(angle);
                    break;
                case 'z':
                    rotation = Matrix4X4.CreateRotationZ(angle);
                    break;
                default:
                    return point;
            }
            Vector4D<float> rotatedPoint = Vector4D.Transform(new Vector4D<float>(point.X, point.Y, point.Z, 1f),rotation);

            return new Vector3D<float>(
                (float)Math.Round(rotatedPoint.X * 100) / 100,
                (float)Math.Round(rotatedPoint.Y * 100) / 100,
                (float)Math.Round(rotatedPoint.Z * 100) / 100);
        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            if (isAnimating)
            {
                currentRotationAngle += (float)(rotationSpeed * deltaTime * rotationDirection);
                if (Math.Abs(currentRotationAngle) >= 90f)
                {
                    FinishRotation();
                }
            }
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            var viewMatrix = Matrix4X4.CreateLookAt(camera.Position, camera.Target, camera.UpVector);
            SetMatrix(viewMatrix, ViewMatrixVariableName);

            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)(Math.PI / 4), 1.0f, 0.1f, 100f);
            SetMatrix(projectionMatrix, ProjectionMatrixVariableName);

            for (int i = 0; i < rubiksCubePieces.Count; i++)
            {
                var piece = rubiksCubePieces[i];

                Matrix4X4<float> modelMatrix = Matrix4X4.CreateScale(0.25f) *
                                              Matrix4X4.CreateTranslation(piece.Position);
                SetMatrix(modelMatrix, ModelMatrixVariableName);

                bool shouldRotate = piecesToRotateIndices.Contains(i);
                Matrix4X4<float> rotationMatrix = Matrix4X4<float>.Identity;

                if (shouldRotate && isAnimating)
                {
                    float angleInRadians = currentRotationAngle * (MathF.PI / 180.0f);

                    switch (rotationAxis)
                    {
                        case 'x':
                            rotationMatrix = Matrix4X4.CreateRotationX(angleInRadians);
                            break;
                        case 'y':
                            rotationMatrix = Matrix4X4.CreateRotationY(angleInRadians);
                            break;
                        case 'z':
                            rotationMatrix = Matrix4X4.CreateRotationZ(angleInRadians);
                            break;
                    }
                }

                SetMatrix(rotationMatrix, RotationMatrixVariableName);
                DrawModelObject(piece);
            }
        }

        private static unsafe void DrawModelObject(ModelObjectDescriptor modelObject)
        {
            Gl.BindVertexArray(modelObject.Vao);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, modelObject.Indices);
            Gl.DrawElements(PrimitiveType.Triangles, modelObject.IndexArrayLength, DrawElementsType.UnsignedInt, null);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(0);
        }

        private static unsafe void SetMatrix(Matrix4X4<float> mx, string uniformName)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&mx);
            CheckError();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }
    }
}
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace Szeminarium1_24_02_17_2
{
    internal static class Program
    {
        private static CameraDescriptor cameraDescriptor = new();

        private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;

        private static IInputContext inputContext;

        private static GL Gl;

        private static ImGuiController controller;

        private static uint program;

        private static GlObject table;
        private static GlObject car;
        private static Vector3D<float> carPosition = new Vector3D<float>(0f, 0.5f, 3f);
        private static float carSpeed = 0f;
        private static float carRotation = 0f;
        private const float MaxSpeed = 0.5f;
        private const float Acceleration = 0.01f;
        private const float RotationSpeed = 0.05f;
        private static List<CoinInstance> coins = new();
        private static int score = 0;
        private const float CoinCollectionDistance = 1.0f;
        private const int MaxCoins = 200;
         private static float tableSize = 400f; 
        private static float visibleDistance = 200f;

        private static List<BuildingInstance> buildings = new();
        private const int MaxBuildings = 200;
        private const float BuildingGenerationDistance = 150f; 
        private static float lastBuildingGenPosition = 0f; 

        private static float Shininess = 50;

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;
        layout (location = 2) in vec3 vNorm;

        uniform mat4 uModel;
        uniform mat3 uNormal;
        uniform mat4 uView;
        uniform mat4 uProjection;

		out vec4 outCol;
        out vec3 outNormal;
        out vec3 outWorldPosition;
        
        void main()
        {
			outCol = vCol;
            gl_Position = uProjection*uView*uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0);
            outNormal = uNormal*vNorm;
            outWorldPosition = vec3(uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0));
        }
        ";

        private const string LightColorVariableName = "lightColor";
        private const string LightPositionVariableName = "lightPos";
        private const string ViewPosVariableName = "viewPos";
        private const string ShininessVariableName = "shininess";

        private static readonly string FragmentShaderSource = @"
        #version 330 core

        uniform vec3 lightColor;
        uniform vec3 lightPos;
        uniform vec3 viewPos;
        uniform float shininess;
        uniform bool isCoin;

        out vec4 FragColor;

        in vec4 outCol;
        in vec3 outNormal;
        in vec3 outWorldPosition;

        void main()
        {
            float ambientStrength = isCoin ? 0.8 : 0.4;
            vec3 ambient = ambientStrength * outCol.rgb * 2.0;
    
            float specularStrength = isCoin ? 2.0 : 1.5;
            float shininessFactor = isCoin ? shininess * 2.0 : shininess * 0.3;
    
            vec3 norm = normalize(outNormal);
            vec3 lightDir = normalize(lightPos - outWorldPosition);
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = diff * lightColor * outCol.rgb * 0.6 * 2.0;

            vec3 viewDir = normalize(viewPos - outWorldPosition);
            vec3 reflectDir = reflect(-lightDir, norm);
            float spec = pow(max(dot(viewDir, reflectDir), 0.0), shininessFactor);
            vec3 specular = specularStrength * spec * lightColor * outCol.rgb;

            vec3 result = (ambient + diffuse + specular) * outCol.rgb;
    
            if (isCoin) {
                result = outCol.rgb * 1.5;
            } else {
                result = pow(result, vec3(1.0/1.2));
            }
    
            FragColor = vec4(result, outCol.w);
        }
        ";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Neon Racer";
            windowOptions.Size = new Vector2D<int>(1000, 800);
             windowOptions.PreferredDepthBufferBits = 24;

            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;

            window.Run();
        }

        private static void Window_Load()
        {
              inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            Gl = window.CreateOpenGL();

            controller = new ImGuiController(Gl, window, inputContext);
             window.FramebufferResize += s =>
            {
                 Gl.Viewport(s);
            };


            Gl.ClearColor(System.Drawing.Color.Black);

            SetUpObjects();

            LinkProgram();
 
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
            cameraDescriptor.SetTopRearView();

        }

        private static void LinkProgram()
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

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Left:
                    carRotation += RotationSpeed; 
                    break;
                case Key.Right:
                    carRotation -= RotationSpeed; 
                    break;
                case Key.Space:
                    carSpeed = Math.Min(carSpeed + Acceleration, MaxSpeed);
                    break;
                case Key.Down:
                    carSpeed = Math.Min(carSpeed - Acceleration, MaxSpeed);
                    break;
            }
        }

        private static void Window_Update(double deltaTime)
        {
            if (carSpeed > 0)
            {
                carPosition.X -= (float)(carSpeed * Math.Sin(carRotation));
                carPosition.Z -= (float)(carSpeed * Math.Cos(carRotation)); 
                cameraDescriptor.FollowTarget(carPosition, carRotation);
                CheckBuildingCollisions();
            }
            cameraDescriptor.FollowTarget(carPosition, carRotation);
            cubeArrangementModel.AdvanceTime(deltaTime);
             GenerateBuildingsAhead();
             foreach (var coin in coins.Where(c => !c.Collected))
            {
                coin.Rotation += 0.1f; 
                 float distance = Vector3D.Distance(carPosition, coin.Position);
                if (distance < CoinCollectionDistance)
                {
                    coin.Collected = true;
                    score++;
                }
            }
             UpdateCoinsAhead();

            controller.Update((float)deltaTime);
        }

        private static void CheckBuildingCollisions()
        {
            foreach (var building in buildings)
            {
                 float carBuildingDistance = Vector3D.Distance(
                    carPosition,
                    building.Position
                );
                  float collisionThreshold = 1.0f + building.Width / 2;

                if (carBuildingDistance < collisionThreshold)
                {
                     Console.WriteLine("Ütközés történt egy épülettel! Játék vége.");
                    window.Close();
                    break;
                }
            }
        }

        private static void GenerateBuildingsAhead()
        {
             if (Math.Abs(carPosition.Z - lastBuildingGenPosition) < 50)
                return;

            lastBuildingGenPosition = carPosition.Z;
             buildings.RemoveAll(b => Vector3D.Distance(carPosition, b.Position) > visibleDistance * 1.5f);
             while (buildings.Count < MaxBuildings)
            {
                GenerateRandomBuilding();
            }
        }

        private static void GenerateRandomBuilding()
        {
             float[] neonColor = [0.0f, 1.0f, 1.0f, 1.0f]; 
             float width = 5f + (float)Random.Shared.NextDouble() * 1.0f;
            float height = 10f + (float)Random.Shared.NextDouble() * 2.0f;
            float depth = 5f + (float)Random.Shared.NextDouble() * 0.5f;
             float minDistance = 10.0f; 
            float angle = (float)Random.Shared.NextDouble() * MathF.PI * 2;
            float distance = minDistance + (float)Random.Shared.NextDouble() * BuildingGenerationDistance;
             float posX = carPosition.X + MathF.Sin(angle) * distance;
            float posZ = carPosition.Z - MathF.Cos(angle) * distance; 
             posX = Math.Clamp(posX, -tableSize / 2 + width / 2, tableSize / 2 - width / 2);
            posZ = Math.Clamp(posZ, -tableSize / 2 + depth / 2, tableSize / 2 - depth / 2);
             Vector3D<float> buildingPos = new Vector3D<float>(posX, height / 2, posZ);
            if (Vector3D.Distance(carPosition, buildingPos) < minDistance)
                return;
             var building = GlCube.CreateCubeWithFaceColors(Gl,
                neonColor, neonColor, neonColor,
                neonColor, neonColor, neonColor);

            buildings.Add(new BuildingInstance
            {
                Mesh = building,
                Width = width,
                Height = height,
                Depth = depth,
                Position = buildingPos
            });
        }

        private static void UpdateCoinsAhead()
        {
             coins.RemoveAll(c => c.Collected || Vector3D.Distance(carPosition, c.Position) > visibleDistance * 1.5f);
             while (coins.Count < MaxCoins)
            {
                GenerateRandomCoin();
            }
        }

        private static void GenerateRandomCoin()
        {
             float[] pureGoldColor = [1.0f, 0.84f, 0.0f, 1.0f];
             float minDistance = 10.0f; 
            float maxDistance = visibleDistance * 0.8f;
            float angle = (float)Random.Shared.NextDouble() * MathF.PI * 2; 
            float distance = minDistance + (float)Random.Shared.NextDouble() * (maxDistance - minDistance);
             float posX = carPosition.X + MathF.Sin(angle) * distance;
            float posZ = carPosition.Z - MathF.Cos(angle) * distance; 
             Vector3D<float> coinPos = new Vector3D<float>(posX, 0.3f, posZ);
             if (buildings.Any(b => Vector3D.Distance(coinPos, b.Position) < 2.0f))
                return;
             var coinMesh = ObjResourceReader.CreateModelFromObjFile(Gl, "Resources/coin.obj", pureGoldColor);

            coins.Add(new CoinInstance
            {
                Mesh = coinMesh,
                Position = coinPos,
                Collected = false,
                Rotation = Random.Shared.NextSingle() * MathF.PI * 2,
                Scale = 0.5f
            });
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            SetLightColor();
            SetLightPosition();
            SetViewerPosition();
            SetShininess();
             DrawGameWorld();
             ImGui.SetNextWindowPos(new System.Numerics.Vector2(window.Size.X - 150, 10), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(140, 60), ImGuiCond.Always);
            ImGui.Begin("Score", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse);
            ImGui.Text($"Coins collected: {score}");
            ImGui.End();

            controller.Render();
        }

        private static unsafe void SetLightColor()
        {
            int location = Gl.GetUniformLocation(program, LightColorVariableName);
            if (location == -1)
            {
                throw new Exception($"{LightColorVariableName} uniform not found on shader.");
            }
             Gl.Uniform3(location, 1.5f, 1.5f, 1.5f); 
            CheckError();
        }

        private static unsafe void SetLightPosition()
        {
            int location = Gl.GetUniformLocation(program, LightPositionVariableName);
            if (location == -1)
            {
                throw new Exception($"{LightPositionVariableName} uniform not found on shader.");
            }
             Gl.Uniform3(location, carPosition.X, carPosition.Y + 6f, carPosition.Z);

            CheckError();
        }

        private static unsafe void SetViewerPosition()
        {
            int location = Gl.GetUniformLocation(program, ViewPosVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewPosVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, cameraDescriptor.Position.X, cameraDescriptor.Position.Y, cameraDescriptor.Position.Z);
            CheckError();
        }

        private static unsafe void SetShininess()
        {
            int location = Gl.GetUniformLocation(program, ShininessVariableName);

            if (location == -1)
            {
                throw new Exception($"{ShininessVariableName} uniform not found on shader.");
            }

            Gl.Uniform1(location, Shininess);
            CheckError();
        }

        private static unsafe void DrawGameWorld()
        {
             var modelMatrixForTable = Matrix4X4.CreateScale(tableSize, 1f, tableSize);
            SetModelMatrix(modelMatrixForTable);
            Gl.BindVertexArray(table.Vao);
            Gl.DrawElements(GLEnum.Triangles, table.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
             foreach (var building in buildings)
            {
                var modelMatrix = Matrix4X4.CreateScale(building.Width, building.Height, building.Depth) *
                                Matrix4X4.CreateTranslation(building.Position);

                SetModelMatrix(modelMatrix);
                Gl.BindVertexArray(building.Mesh.Vao);
                Gl.DrawElements(GLEnum.Triangles, building.Mesh.IndexArrayLength, GLEnum.UnsignedInt, null);
            }
             var modelMatrixForCar = Matrix4X4.CreateScale(0.7f) *
                          Matrix4X4.CreateRotationY(carRotation) *
                          Matrix4X4.CreateTranslation(carPosition);
            SetModelMatrix(modelMatrixForCar);
            Gl.BindVertexArray(car.Vao);
            Gl.DrawElements(GLEnum.Triangles, car.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
             int isCoinLocation = Gl.GetUniformLocation(program, "isCoin");
            Gl.Uniform1(isCoinLocation, 1); // True for coins
             foreach (var coin in coins.Where(c => !c.Collected))
            {
                var modelMatrix = Matrix4X4.CreateScale(coin.Scale) *
                                Matrix4X4.CreateRotationX(MathF.PI / 2) * 
                                Matrix4X4.CreateRotationY(coin.Rotation) * 
                                Matrix4X4.CreateTranslation(coin.Position);

                SetModelMatrix(modelMatrix);
                Gl.BindVertexArray(coin.Mesh.Vao);
                Gl.DrawElements(GLEnum.Triangles, coin.Mesh.IndexArrayLength, GLEnum.UnsignedInt, null);
            }
             Gl.Uniform1(isCoinLocation, 0);
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
            CheckError();

            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInvers;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));
            location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
            }
            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        private static unsafe void SetUpObjects()
        {
             float[] tableColor = [0.32f, 0.3f, 0.32f, 1.0f];
            table = GlCube.CreateSquare(Gl, tableColor);
             float[] neonCarColor = [1.0f, 0.08f, 0.58f, 1.0f];
            float[] windowColor = [0.1f, 0.1f, 0.2f, 0.4f];
            float[] wheelColor = [0.01f, 0.01f, 0.01f, 1.0f];

            string objFilePath = "Resources/car.obj";
            car = ObjResourceReader.CreateModelFromObjFile(Gl, objFilePath, neonCarColor);
             for (int i = 0; i < MaxBuildings / 2; i++)
            {
                GenerateRandomBuilding();
            }

            for (int i = 0; i < MaxCoins / 2; i++)
            {
                GenerateRandomCoin();
            }
        }

        private static void Window_Closing()
        {
            car.ReleaseGlObject();
            table.ReleaseGlObject();

            foreach (var building in buildings)
            {
                building.Mesh.ReleaseGlObject();
            }

            foreach (var coin in coins)
            {
                coin.Mesh.ReleaseGlObject();
            }
        }

        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, 1024f / 768f, 0.1f, 100);
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
            CheckError();
        }

        private static unsafe void SetViewMatrix()
        {
            var viewMatrix = Matrix4X4.CreateLookAt(cameraDescriptor.Position, cameraDescriptor.Target, cameraDescriptor.UpVector);
            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
            CheckError();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }

        class BuildingInstance
        {
            public GlObject Mesh;
            public float Width;
            public float Height;
            public float Depth;
            public Vector3D<float> Position;
        }

        class CoinInstance
        {
            public GlObject Mesh { get; set; }
            public Vector3D<float> Position { get; set; }
            public bool Collected { get; set; }
            public float Rotation { get; set; }
            public float Scale { get; set; } = 0.5f;
        }
    }
}
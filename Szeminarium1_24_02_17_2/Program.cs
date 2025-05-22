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
        private const float MaxSpeed = 1f;
        private const float Acceleration = 0.05f;
        private const float RotationSpeed = 0.05f;
        private static List<diamondInstance> diamonds = new();
        private static int score = 0;
        private const float diamondCollectionDistance = 1.0f;
        private const int Maxdiamonds = 200;
        private static float tableSize = 100f;
        private static float visibleDistance = 100f;

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
        uniform bool isdiamond;

        out vec4 FragColor;

        in vec4 outCol;
        in vec3 outNormal;
        in vec3 outWorldPosition;

        void main()
        {
            float ambientStrength = isdiamond ? 0.8 : 0.4;
            vec3 ambient = ambientStrength * outCol.rgb * 2.0;
    
            float specularStrength = isdiamond ? 2.0 : 1.5;
            float shininessFactor = isdiamond ? shininess * 2.0 : shininess * 0.3;
    
            vec3 norm = normalize(outNormal);
            vec3 lightDir = normalize(lightPos - outWorldPosition);
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = diff * lightColor * outCol.rgb * 0.6 * 2.0;

            vec3 viewDir = normalize(viewPos - outWorldPosition);
            vec3 reflectDir = reflect(-lightDir, norm);
            float spec = pow(max(dot(viewDir, reflectDir), 0.0), shininessFactor);
            vec3 specular = specularStrength * spec * lightColor * outCol.rgb;

            vec3 result = (ambient + diffuse + specular) * outCol.rgb;
    
            if (isdiamond) {
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
                    if (carSpeed >= 0)
                    {
                        carRotation += RotationSpeed;
                    }
                    else
                    {
                        carRotation -= RotationSpeed;
                    }
                        break;
                case Key.Right:
                    if (carSpeed >= 0)
                    {
                        carRotation -= RotationSpeed;
                    }
                    else
                    {
                        carRotation += RotationSpeed;
                    }
                    break;
                case Key.Up:
                    carSpeed = Math.Min(carSpeed + Acceleration, MaxSpeed);
                    break;
                case Key.Down:
                    carSpeed = Math.Max(carSpeed - Acceleration, -MaxSpeed);
                    break;
                case Key.Space:
                    cameraDescriptor.ToggleCameraView();
                    break;
            }
        }

        private static void Window_Update(double deltaTime)
        {
            if (carSpeed != 0)
            {
                carPosition.X -= (float)(Math.Abs(carSpeed) * Math.Sin(carRotation)) * Math.Sign(carSpeed);
                carPosition.Z -= (float)(Math.Abs(carSpeed) * Math.Cos(carRotation)) * Math.Sign(carSpeed);
                CheckBuildingCollisions();
            }
            cameraDescriptor.FollowTarget(carPosition, carRotation);
            cubeArrangementModel.AdvanceTime(deltaTime);
            GenerateBuildingsAhead();
            foreach (var diamond in diamonds.Where(c => !c.Collected))
            {
                diamond.Rotation += 0.1f;
                float distance = Vector3D.Distance(carPosition, diamond.Position);
                if (distance < diamondCollectionDistance)
                {
                    diamond.Collected = true;
                    score++;
                }
            }
            UpdatediamondsAhead();

            controller.Update((float)deltaTime);
        }

        private static void CheckBuildingCollisions()
        {
            float collisionOffset = carSpeed > 0 ? 1.0f : -1.0f;
            Vector3D<float> carCollisionPoint = new Vector3D<float>(
                carPosition.X - collisionOffset * (float)Math.Sin(carRotation),
                carPosition.Y,
                carPosition.Z - collisionOffset * (float)Math.Cos(carRotation)
            );

            foreach (var building in buildings)
            {
                float distance = Vector3D.Distance(carCollisionPoint, building.Position);
                float collisionThreshold = 1.0f + building.Width / 2;

                if (distance < collisionThreshold)
                {
                    Console.WriteLine("GAME OVER");
                    window.Close();
                    break;
                }
            }
        }

        private static void GenerateBuildingsAhead()
        {
            // Track the last check position to avoid frequent checks
            float distanceMoved = Vector3D.Distance(new Vector3D<float>(carPosition.X, 0, carPosition.Z),
                                                  new Vector3D<float>(lastBuildingGenPosition, 0, 0));

            // Only generate new buildings if the car has moved a significant distance
            if (distanceMoved < 10)
                return;

            // Update the generation position reference
            lastBuildingGenPosition = carPosition.Z;

            // Remove buildings that are too far away
            buildings.RemoveAll(b => Vector3D.Distance(carPosition, b.Position) > visibleDistance * 1.5f);

            // Calculate the area where we want to generate buildings
            float minX = carPosition.X - visibleDistance;
            float maxX = carPosition.X + visibleDistance;
            float minZ = carPosition.Z - visibleDistance;
            float maxZ = carPosition.Z + visibleDistance;

            // Divide the area into a grid
            const int gridSize = 20; // Number of cells in each direction
            float cellWidth = (maxX - minX) / gridSize;
            float cellDepth = (maxZ - minZ) / gridSize;

            // Generate buildings in a more structured way
            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    // Calculate the center position of this grid cell
                    float centerX = minX + (i + 0.5f) * cellWidth;
                    float centerZ = minZ + (j + 0.5f) * cellDepth;

                    // Distance from car to the center of this cell
                    float distance = Vector3D.Distance(
                        new Vector3D<float>(carPosition.X, 0, carPosition.Z),
                        new Vector3D<float>(centerX, 0, centerZ));

                    // Skip if too close to the car or outside generation range
                    if (distance < 10 || distance > visibleDistance)
                        continue;

                    // Check if there's already a building near this cell
                    bool buildingExists = buildings.Any(b =>
                        Vector3D.Distance(
                            new Vector3D<float>(centerX, 0, centerZ),
                            new Vector3D<float>(b.Position.X, 0, b.Position.Z)) < cellWidth * 2);

                    // If no building exists and we pass a random check, generate a building
                    if (!buildingExists && Random.Shared.NextDouble() < 0.2)
                    {
                        // Random offset within the cell
                        float offsetX = (float)(Random.Shared.NextDouble() - 0.5) * cellWidth * 0.5f;
                        float offsetZ = (float)(Random.Shared.NextDouble() - 0.5) * cellDepth * 0.5f;

                        // Final position with offset
                        float posX = centerX + offsetX;
                        float posZ = centerZ + offsetZ;

                        // Check if within table bounds
                        if (posX < -tableSize / 2 + 5 || posX > tableSize / 2 - 5 ||
                            posZ < -tableSize / 2 + 5 || posZ > tableSize / 2 - 5)
                            continue;

                        // Create building with neon color
                        float[] neonColor = GetRandomNeonColor();
                        float width = 3f + (float)Random.Shared.NextDouble() * 3.0f;
                        float height = 5f + (float)Random.Shared.NextDouble() * 15.0f;
                        float depth = 3f + (float)Random.Shared.NextDouble() * 3.0f;

                        Vector3D<float> buildingPos = new Vector3D<float>(posX, height / 2, posZ);

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

                        // Stop when max buildings is reached
                        if (buildings.Count >= MaxBuildings)
                            break;
                    }
                }

                if (buildings.Count >= MaxBuildings)
                    break;
            }
        }

        // Helper method to generate random neon colors
        private static float[] GetRandomNeonColor()
        {
            // Define a set of vibrant neon colors
            float[][] neonColors = new float[][]
            {
        new float[] { 1.0f, 0.0f, 1.0f, 1.0f },  // Magenta
        new float[] { 0.0f, 1.0f, 1.0f, 1.0f },  // Cyan
        new float[] { 1.0f, 0.5f, 0.0f, 1.0f },  // Orange
        new float[] { 0.0f, 1.0f, 0.5f, 1.0f },  // Green
        new float[] { 0.5f, 0.0f, 1.0f, 1.0f },  // Purple
        new float[] { 1.0f, 1.0f, 0.0f, 1.0f },  // Yellow
        new float[] { 0.0f, 0.5f, 1.0f, 1.0f },  // Blue
            };

            return neonColors[Random.Shared.Next(neonColors.Length)];
        }

        private static void GenerateRandomBuilding()
        {
            float[] neonColor = [0.0f, 1.0f, 1.0f, 1.0f];
            float width = 5f + (float)Random.Shared.NextDouble() * 1.0f;
            float height = 5f + (float)Random.Shared.NextDouble() * 2.0f;
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

        private static void UpdatediamondsAhead()
        {
            diamonds.RemoveAll(c => c.Collected || Vector3D.Distance(carPosition, c.Position) > visibleDistance * 1.5f);
            while (diamonds.Count < Maxdiamonds)
            {
                GenerateRandomdiamond();
            }
        }

        private static void GenerateRandomdiamond()
        {
            float[] pureGoldColor = [1.0f, 0.84f, 0.0f, 1.0f];
            float minDistance = 10.0f;
            float maxDistance = visibleDistance * 0.8f;
            float angle = (float)Random.Shared.NextDouble() * MathF.PI * 2;
            float distance = minDistance + (float)Random.Shared.NextDouble() * (maxDistance - minDistance);
            float posX = carPosition.X + MathF.Sin(angle) * distance;
            float posZ = carPosition.Z - MathF.Cos(angle) * distance;
            Vector3D<float> diamondPos = new Vector3D<float>(posX, 1f, posZ);
            if (buildings.Any(b => Vector3D.Distance(diamondPos, b.Position) < 2.0f))
                return;
            var diamondMesh = ObjResourceReader.CreateModelFromObjFile(Gl, "Resources/diamond.obj", pureGoldColor);

            diamonds.Add(new diamondInstance
            {
                Mesh = diamondMesh,
                Position = diamondPos,
                Collected = false,
                Rotation = Random.Shared.NextSingle() * MathF.PI * 2,
                Scale = 0.005f
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
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(window.Size.X - 180, 10), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(170, 60), ImGuiCond.Always);
            ImGui.Begin("Score", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse);
            ImGui.Text($"Diamonds collected: {score}");
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
            int isdiamondLocation = Gl.GetUniformLocation(program, "isdiamond");
            Gl.Uniform1(isdiamondLocation, 1);
            foreach (var diamond in diamonds.Where(c => !c.Collected))
            {
                var modelMatrix = Matrix4X4.CreateScale(diamond.Scale) *
                                Matrix4X4.CreateRotationX(MathF.PI / 2) *
                                Matrix4X4.CreateRotationY(diamond.Rotation) *
                                Matrix4X4.CreateTranslation(diamond.Position);

                SetModelMatrix(modelMatrix);
                Gl.BindVertexArray(diamond.Mesh.Vao);
                Gl.DrawElements(GLEnum.Triangles, diamond.Mesh.IndexArrayLength, GLEnum.UnsignedInt, null);
            }
            Gl.Uniform1(isdiamondLocation, 0);
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

            for (int i = 0; i < Maxdiamonds / 2; i++)
            {
                GenerateRandomdiamond();
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

            foreach (var diamond in diamonds)
            {
                diamond.Mesh.ReleaseGlObject();
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

        class diamondInstance
        {
            public GlObject Mesh { get; set; }
            public Vector3D<float> Position { get; set; }
            public bool Collected { get; set; }
            public float Rotation { get; set; }
            public float Scale { get; set; } = 0.5f;
        }
    }
}
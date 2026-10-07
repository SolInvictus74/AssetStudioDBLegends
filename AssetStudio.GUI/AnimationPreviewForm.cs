using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.WinForms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace AssetStudio.GUI
{
    internal sealed class AnimationPreviewForm : Form
    {
        private readonly ImportedFrame rootFrame;
        private readonly System.Collections.Generic.List<ImportedKeyframedAnimation> animations;
        private readonly System.Collections.Generic.List<ImportedMesh> meshes;
        private readonly AnimationPlayer animationPlayer;
        private readonly SkeletonRenderer skeletonRenderer;
        //AnimationPreview 07-10-26
        private readonly MeshRenderer meshRenderer;
        private readonly System.Collections.Generic.List<Vector3>
            originalMeshVertices = new();
        private readonly System.Collections.Generic.List<int>
            meshVertexOffsets = new();
        private readonly List<ImportedMesh> renderableMeshes = new();
        private readonly System.Collections.Generic.List<Vector3>
            animatedMeshVertices = new();
        //AnimationPreviev 07-10-26

        private readonly GLControl glControl;
        private readonly Label statusLabel;
        private readonly TrackBar timelineTrackBar;
        private readonly Label timeLabel;

        private readonly ComboBox animationComboBox;
        private readonly Label frameLabel;

        private readonly Button playPauseButton;
        private readonly CheckBox loopCheckBox;
        private readonly Timer playbackTimer;

        private readonly ComboBox playbackSpeedComboBox;

        private float playbackSpeed = 1.0f;

        private bool isPlaying;
        private DateTime lastPlaybackTime;

        private ImportedKeyframedAnimation currentAnimation;
        private float animationStartTime;
        private float animationEndTime;
        private bool timelineReady;

        private const int TimelineResolution = 1000;


        private Matrix4 modelMatrix = Matrix4.Identity;
        private Matrix4 viewMatrix = Matrix4.Identity;
        private Matrix4 projectionMatrix = Matrix4.Identity;

        private bool glLoaded;
        private bool leftMouseDown;
        private bool rightMouseDown;

        private int mouseX;
        private int mouseY;

        public AnimationPreviewForm(
            ImportedFrame rootFrame,
            System.Collections.Generic.List<ImportedMesh> meshes,
            System.Collections.Generic.List<ImportedKeyframedAnimation> animations)
        {
            this.rootFrame = rootFrame
                ?? throw new ArgumentNullException(nameof(rootFrame));

            this.animations = animations
                ?? throw new ArgumentNullException(nameof(animations));

            this.meshes = meshes
                ?? throw new ArgumentNullException(nameof(meshes));



            animationPlayer =
                new AnimationPlayer(rootFrame);
            //AnimPreviewer - Diagnostic 07-10-26
            //var bindPoseReport = animationPlayer.DiagnoseBindPoses(meshes);

            //System.Windows.Forms.MessageBox.Show(
            //    bindPoseReport,
            //    "SITools - Bind Pose Diagnostic",
            //    System.Windows.Forms.MessageBoxButtons.OK,
            //    System.Windows.Forms.MessageBoxIcon.Information);

            //var cpuSkinner = new CpuSkinner(animationPlayer);
            //var worldMatrices = animationPlayer.GetWorldMatrices(null, 0f);

            //var report = new System.Text.StringBuilder();

            //foreach (var mesh in meshes)
            //{
            //    if (mesh?.BoneList == null ||
            //        mesh.BoneList.Count == 0)
            //        continue;

            //    if (!worldMatrices.TryGetValue(mesh.Path, out var meshWorld))
            //        continue;

            //    var inverseMeshWorld =
            //        AnimationPlayer.InvertMatrix(meshWorld);

            //    var skinnedVertices =
            //        cpuSkinner.SkinMesh(
            //            mesh,
            //            worldMatrices,
            //            inverseMeshWorld);

            //    double maxDisplacement = 0;
            //    double totalDisplacement = 0;
            //    int worstVertexIndex = -1;

            //    for (int i = 0; i < mesh.VertexList.Count; i++)
            //    {
            //        var original = mesh.VertexList[i].Vertex;
            //        var transformed = skinnedVertices[i];

            //        double dx = transformed.X - original.X;
            //        double dy = transformed.Y - original.Y;
            //        double dz = transformed.Z - original.Z;

            //        double displacement = Math.Sqrt(
            //            dx * dx + dy * dy + dz * dz);

            //        if (displacement > maxDisplacement)
            //        {
            //            maxDisplacement = displacement;
            //            worstVertexIndex = i;
            //        }

            //        totalDisplacement += displacement;
            //    }

            //    double averageDisplacement =
            //        mesh.VertexList.Count > 0
            //            ? totalDisplacement / mesh.VertexList.Count
            //            : 0;

            //    report.AppendLine(
            //        $"Worst vertex index: {worstVertexIndex}");

            //    if (worstVertexIndex >= 0)
            //    {
            //        var vertex = mesh.VertexList[worstVertexIndex];

            //        report.AppendLine(
            //            $"Original position: " +
            //            $"{vertex.Vertex.X:F4}, " +
            //            $"{vertex.Vertex.Y:F4}, " +
            //            $"{vertex.Vertex.Z:F4}");

            //        if (vertex.BoneIndices != null &&
            //            vertex.Weights != null)
            //        {
            //            for (int k = 0;
            //                 k < Math.Min(
            //                     vertex.BoneIndices.Length,
            //                     vertex.Weights.Length);
            //                 k++)
            //            {
            //                int boneIndex = vertex.BoneIndices[k];

            //                string bonePath =
            //                    mesh.BoneList != null &&
            //                    boneIndex >= 0 &&
            //                    boneIndex < mesh.BoneList.Count
            //                        ? mesh.BoneList[boneIndex].Path
            //                        : "INVALID BONE INDEX";

            //                string boneName =
            //                    bonePath.Substring(
            //                        bonePath.LastIndexOf('/') + 1);

            //                report.AppendLine(
            //                    $"Influence {k}: " +
            //                    $"Bone={boneIndex}, " +
            //                    $"Name={boneName}, " +
            //                    $"Weight={vertex.Weights[k]:F6}");

            //                if (vertex.Weights[k] > 0 &&
            //                    boneIndex >= 0 &&
            //                    boneIndex < mesh.BoneList.Count &&
            //                    worldMatrices.TryGetValue(
            //                        mesh.BoneList[boneIndex].Path,
            //                        out var boneWorld))
            //                {
            //                    var skinMatrix =
            //                        inverseMeshWorld *
            //                        boneWorld *
            //                        mesh.BoneList[boneIndex].Matrix;

            //                    var transformed =
            //                        AnimationPlayer.TransformPoint(
            //                            skinMatrix,
            //                            vertex.Vertex);

            //                    double dx = transformed.X - vertex.Vertex.X;
            //                    double dy = transformed.Y - vertex.Vertex.Y;
            //                    double dz = transformed.Z - vertex.Vertex.Z;

            //                    double boneDisplacement = Math.Sqrt(
            //                        dx * dx + dy * dy + dz * dz);

            //                    report.AppendLine(
            //                        $"  Bone displacement: {boneDisplacement:F6}");

            //                    report.AppendLine(
            //                        $"  Transformed position: " +
            //                        $"{transformed.X:F4}, " +
            //                        $"{transformed.Y:F4}, " +
            //                        $"{transformed.Z:F4}");
            //                }
            //            }
            //        }
            //    }

            //    report.AppendLine(
            //        $"Mesh: {mesh.Path.Split('/').Last()}");

            //    report.AppendLine(
            //        $"Vertices: {mesh.VertexList.Count}");

            //    report.AppendLine(
            //        $"Maximum displacement: {maxDisplacement:F6}");

            //    report.AppendLine(
            //        $"Average displacement: {averageDisplacement:F6}");

            //    report.AppendLine();
            //}

            //System.Windows.Forms.MessageBox.Show(
            //    report.ToString(),
            //    "RazTools - CPU Skinning Diagnostic",
            //    System.Windows.Forms.MessageBoxButtons.OK,
            //    System.Windows.Forms.MessageBoxIcon.Information);

            skeletonRenderer =
                new SkeletonRenderer();

            //AnimationPreview 07-10-26
            meshRenderer =
                new MeshRenderer();
            //AnimationPreview 07-10-26

            Text = "Animation Preview - Skeleton";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(900, 700);
            MinimumSize = new Size(500, 400);

            glControl = new GLControl
            {
                API = OpenTK.Windowing.Common.ContextAPI.OpenGL,
                APIVersion = new Version(3, 3, 0, 0),
                BackColor = SystemColors.ControlDarkDark,
                Dock = DockStyle.Fill,
                Flags = OpenTK.Windowing.Common.ContextFlags.Default,
                IsEventDriven = true,
                Profile = OpenTK.Windowing.Common.ContextProfile.Core
            };

            statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Skeleton preview",
                Padding = new Padding(6, 0, 0, 0)
            };

            var animationPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 34,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(6, 5, 0, 0)
            };

            var animationLabel = new Label
            {
                Text = "Animation:",
                AutoSize = true,
                Margin = new Padding(0, 4, 6, 0)
            };

            animationComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 350
            };

            foreach (var animation in animations)
            {
                animationComboBox.Items.Add(animation.Name);
            }

            animationComboBox.SelectedIndexChanged +=
                AnimationComboBox_SelectedIndexChanged;

            animationPanel.Controls.Add(animationLabel);
            animationPanel.Controls.Add(animationComboBox);

            timeLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "0.00 / 0.00 s"
            };

            frameLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "Frame 0 / 0"
            };

            timelineTrackBar = new TrackBar
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                Minimum = 0,
                Maximum = TimelineResolution,
                Value = 0,
                TickStyle = TickStyle.None,
                SmallChange = 1,
                LargeChange = 10,
                Enabled = false
            };

            timelineTrackBar.ValueChanged +=
                TimelineTrackBar_ValueChanged;

            playPauseButton = new Button
            {
                Text = "Play",
                Width = 90,
                Height = 26,
                Enabled = false,
                TextAlign = ContentAlignment.MiddleRight
            };
            playPauseButton.Paint +=
                PlayPauseButton_Paint;

            playPauseButton.Click +=
                PlayPauseButton_Click;

            loopCheckBox = new CheckBox
            {
                Text = "Loop",
                AutoSize = true,
                Checked = true,
                Margin = new Padding(10, 6, 0, 0)
            };

            var speedLabel = new Label
            {
                Text = "Speed:",
                AutoSize = true,
                Margin = new Padding(14, 6, 4, 0)
            };

            playbackSpeedComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 70
            };

            playbackSpeedComboBox.Items.Add("0.25x");
            playbackSpeedComboBox.Items.Add("0.5x");
            playbackSpeedComboBox.Items.Add("1x");
            playbackSpeedComboBox.Items.Add("2x");

            playbackSpeedComboBox.SelectedIndex = 2;

            playbackSpeedComboBox.SelectedIndexChanged +=
                PlaybackSpeedComboBox_SelectedIndexChanged;

            var playbackPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 34,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(6, 3, 0, 0)
            };

            playbackPanel.Controls.Add(playPauseButton);
            playbackPanel.Controls.Add(loopCheckBox);
            playbackPanel.Controls.Add(speedLabel);
            playbackPanel.Controls.Add(playbackSpeedComboBox);

            playbackTimer = new System.Windows.Forms.Timer
            {
                Interval = 16
            };

            playbackTimer.Tick +=
                PlaybackTimer_Tick;

            Controls.Add(glControl);
            Controls.Add(animationPanel);
            Controls.Add(timelineTrackBar);
            Controls.Add(timeLabel);
            Controls.Add(frameLabel);
            Controls.Add(playbackPanel);
            Controls.Add(statusLabel);

            glControl.Load += GlControl_Load;
            glControl.Paint += GlControl_Paint;
            glControl.Resize += GlControl_Resize;

            glControl.MouseDown += GlControl_MouseDown;
            glControl.MouseMove += GlControl_MouseMove;
            glControl.MouseUp += GlControl_MouseUp;
            glControl.MouseWheel += GlControl_MouseWheel;

            FormClosed += AnimationPreviewForm_FormClosed;

            ThemeManager.Apply(
                this,
                ThemeManager.CurrentTheme);
        }

        private void PlayPauseButton_Paint(
    object sender,
    PaintEventArgs e)
        {
            var button = (Button)sender;

            int centerY =
                button.ClientSize.Height / 2;

            int left = 10;

            using var brush =
                new SolidBrush(button.ForeColor);

            if (isPlaying)
            {
                // Pause icon: ||
                e.Graphics.FillRectangle(
                    brush,
                    left,
                    centerY - 6,
                    4,
                    12);

                e.Graphics.FillRectangle(
                    brush,
                    left + 7,
                    centerY - 6,
                    4,
                    12);
            }
            else
            {
                // Play icon: triangle
                Point[] triangle =
                {
            new Point(left, centerY - 7),
            new Point(left, centerY + 7),
            new Point(left + 11, centerY)
        };

                e.Graphics.FillPolygon(
                    brush,
                    triangle);
            }
        }

        private static void GetAnimationTimeRange(
            ImportedKeyframedAnimation animation,
            out float startTime,
            out float endTime)
        {
            float minTime = float.MaxValue;
            float maxTime = float.MinValue;

            if (animation != null &&
                animation.TrackList != null)
            {
                foreach (var track in animation.TrackList)
                {
                    IncludeKeys(track.Translations);
                    IncludeKeys(track.Rotations);
                    IncludeKeys(track.Scalings);

                    if (track.BlendShape != null)
                    {
                        IncludeKeys(track.BlendShape.Keyframes);
                    }
                }
            }

            if (minTime == float.MaxValue ||
                maxTime == float.MinValue)
            {
                startTime = 0.0f;
                endTime = 0.0f;
            }
            else
            {
                startTime = minTime;
                endTime = maxTime;
            }

            void IncludeKeys<T>(
                System.Collections.Generic.List<ImportedKeyframe<T>> keys)
            {
                if (keys == null ||
                    keys.Count == 0)
                {
                    return;
                }

                foreach (var key in keys)
                {
                    if (key.time < minTime)
                        minTime = key.time;

                    if (key.time > maxTime)
                        maxTime = key.time;
                }
            }
        }

        private void AnimationComboBox_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            if (!glLoaded)
                return;

            int index = animationComboBox.SelectedIndex;

            if (index < 0 ||
                index >= animations.Count)
            {
                return;
            }

            LoadAnimation(animations[index]);
        }

        private void LoadAnimation(
            ImportedKeyframedAnimation animation)
        {
            if (animation == null)
                return;

            StopPlayback();

            timelineReady = false;

            currentAnimation = animation;

            GetAnimationTimeRange(
                currentAnimation,
                out animationStartTime,
                out animationEndTime);

            float duration =
                animationEndTime - animationStartTime;

            bool hasDuration =
                duration > 0.000001f;

            timelineTrackBar.Enabled = hasDuration;
            playPauseButton.Enabled = hasDuration;

            timelineTrackBar.Value = 0;

            var lines =
                animationPlayer.GetSkeletonLines(
                    currentAnimation,
                    animationStartTime);

            skeletonRenderer.UpdateSkeleton(lines);
            UpdateAnimatedMeshes(animationStartTime);
            timeLabel.Text =
                $"{animationStartTime:0.00} / " +
                $"{animationEndTime:0.00} s";

            UpdateFrameLabel(animationStartTime);

            statusLabel.Text =
                $"{currentAnimation.Name} - " +
                $"{currentAnimation.TrackList.Count} tracks - " +
                $"{currentAnimation.SampleRate:0.##} fps - " +
            $"{lines.Count} segments";

            

            timelineReady = true;

            glControl.Invalidate();
        }

        private void UpdateFrameLabel(float time)
        {
            if (currentAnimation == null ||
                currentAnimation.SampleRate <= 0.0f)
            {
                frameLabel.Text = "Frame 0 / 0";
                return;
            }

            float duration =
                Math.Max(
                    0.0f,
                    animationEndTime - animationStartTime);

            float relativeTime =
                Math.Max(
                    0.0f,
                    time - animationStartTime);

            int totalFrames =
                (int)Math.Round(
                    duration *
                    currentAnimation.SampleRate);

            int currentFrame =
                (int)Math.Round(
                    relativeTime *
                    currentAnimation.SampleRate);

            currentFrame =
                Math.Max(
                    0,
                    Math.Min(
                        totalFrames,
                        currentFrame));

            frameLabel.Text =
                $"Frame {currentFrame} / {totalFrames}";
        }



        private void GlControl_Load(
            object sender,
            EventArgs e)
        {
            glControl.MakeCurrent();

            GL.ClearColor(
                System.Drawing.Color.CadetBlue);

            skeletonRenderer.Initialize();

            /*
             * Start from the base skeleton.
             * This gives us a stable initial camera fit
             * independent of the selected animation.
             */
            var lines =
                animationPlayer.GetSkeletonLines();

            skeletonRenderer.UpdateSkeleton(lines);

            meshRenderer.Initialize();

            meshVertexOffsets.Clear();
            renderableMeshes.Clear();

            var vertices = new System.Collections.Generic.List<Vector3>();
            var indices = new System.Collections.Generic.List<int>();

            var groups =
                new System.Collections.Generic.List<MeshRenderer.MeshDrawGroup>();

            int diagnosticMeshIndex = 0;

            foreach (var mesh in meshes)
            {
                diagnosticMeshIndex++;

                if (mesh?.VertexList == null)
                    continue;

                int meshOffset = vertices.Count;

                meshVertexOffsets.Add(meshOffset);
                renderableMeshes.Add(mesh);

                int startIndex = indices.Count;

                var meshFrame = rootFrame.FindFrameByPath(mesh.Path);

                float offsetX = 0.0f;
                float offsetY = 0.0f;
                float offsetZ = 0.0f;

                if (meshFrame != null && mesh.BoneList?.Count == 0)
                {
                    offsetX = meshFrame.LocalPosition.X;
                    offsetY = meshFrame.LocalPosition.Y;
                    offsetZ = meshFrame.LocalPosition.Z;
                }

                foreach (var vertex in mesh.VertexList)
                {
                    vertices.Add(new Vector3(
                        vertex.Vertex.X + offsetX,
                        vertex.Vertex.Y + offsetY,
                        vertex.Vertex.Z + offsetZ));
                }

                if (mesh.SubmeshList == null)
                    continue;

                foreach (var submesh in mesh.SubmeshList)
                {
                    if (submesh?.FaceList == null)
                        continue;

                    foreach (var face in submesh.FaceList)
                    {
                        foreach (var index in face.VertexIndices)
                        {
                            indices.Add(
                                meshOffset +
                                submesh.BaseVertex +
                                index);
                        }
                    }
                }
            


            int groupIndexCount = indices.Count - startIndex;

            Vector4 color = diagnosticMeshIndex == 3
                ? new Vector4(1.0f, 0.55f, 0.15f, 1.0f)
                : new Vector4(0.65f, 0.68f, 0.72f, 1.0f);

            groups.Add(new MeshRenderer.MeshDrawGroup(
                startIndex,
                groupIndexCount,
                color));

            }

            //MessageBox.Show(
            //    string.Join("\n", groups.Select((g, i) =>
            //        $"Mesh {i + 1}: " +
            //        $"StartIndex={g.StartIndex}, " +
            //        $"IndexCount={g.IndexCount}, " +
            //        $"Triangles={g.IndexCount / 3}")),
            //    "Mesh Draw Groups",
            //    MessageBoxButtons.OK,
            //    MessageBoxIcon.Information);

            meshRenderer.UpdateMeshes(vertices, indices, groups);
            originalMeshVertices.Clear();
            originalMeshVertices.AddRange(vertices);

            animatedMeshVertices.Clear();
            animatedMeshVertices.AddRange(vertices);
            //var meshDiagnostics = meshes.Select((mesh, index) =>
            //{
            //    var frame = rootFrame.FindFrameByPath(mesh.Path);
            //    string frameInfo = frame == null
            //        ? "Frame: NOT FOUND"
            //        : $"Frame: FOUND\n" +
            //          $"Local position: " +
            //          $"X={frame.LocalPosition.X:0.000}, " +
            //          $"Y={frame.LocalPosition.Y:0.000}, " +
            //          $"Z={frame.LocalPosition.Z:0.000}\n" +
            //          $"Local rotation: " +
            //          $"X={frame.LocalRotation.X:0.000}, " +
            //          $"Y={frame.LocalRotation.Y:0.000}, " +
            //          $"Z={frame.LocalRotation.Z:0.000}, " +
            //          $"W={frame.LocalRotation.W:0.000}\n" +
            //          $"Local scale: " +
            //          $"X={frame.LocalScale.X:0.000}, " +
            //          $"Y={frame.LocalScale.Y:0.000}, " +
            //          $"Z={frame.LocalScale.Z:0.000}";
            //    return $"Mesh {index + 1}\n" +
            //           $"Path: {mesh.Path}\n" +
            //           $"Vertices: {mesh.VertexList?.Count ?? 0}\n" +
            //           $"Bones: {mesh.BoneList?.Count ?? 0}\n" +
            //           frameInfo;
            //});
            //MessageBox.Show(
            //    string.Join("\n\n", meshDiagnostics),
            //    "Mesh Transform Diagnostics",
            //    MessageBoxButtons.OK,
            //    MessageBoxIcon.Information);

            modelMatrix =
                Matrix4.Identity;

            FitSkeletonToView(lines);

            UpdateProjectionMatrix();

            /*
             * OpenGL is now ready.
             * LoadAnimation() can safely update the renderer.
             */
            glLoaded = true;
            timelineReady = true;

            if (animations.Count > 0)
            {
                /*
                 * Selecting the first item triggers
                 * AnimationComboBox_SelectedIndexChanged(),
                 * which calls LoadAnimation().
                 */
                animationComboBox.SelectedIndex = 0;
            }
            else
            {
                currentAnimation = null;

                animationComboBox.Enabled = false;
                timelineTrackBar.Enabled = false;
                playPauseButton.Enabled = false;

                timeLabel.Text =
                    "0.00 / 0.00 s";

                frameLabel.Text =
                    "Frame 0 / 0";

                statusLabel.Text =
                    $"Skeleton preview - {lines.Count} segments";
            }

            int meshCount =
                meshes?.Count ?? 0;

            int vertexCount =
                meshes?
                    .Where(mesh => mesh?.VertexList != null)
                    .Sum(mesh => mesh.VertexList.Count)
                ?? 0;

            int triangleCount =
                meshes?
                    .Where(mesh => mesh?.SubmeshList != null)
                    .SelectMany(mesh => mesh.SubmeshList)
                    .Where(submesh => submesh?.FaceList != null)
                    .Sum(submesh => submesh.FaceList.Count)
                ?? 0;

            statusLabel.Text =
                $"{statusLabel.Text} - " +
                $"{meshCount} meshes - " +
                $"{vertexCount} vertices - " +
                $"{triangleCount} triangles";

            glControl.Invalidate();
        }

        private void GlControl_Paint(
            object sender,
            PaintEventArgs e)
        {
            if (!glLoaded ||
                glControl.IsDisposed ||
                glControl.Disposing ||
                !glControl.IsHandleCreated)
            {
                return;
            }

            try
            {
                glControl.MakeCurrent();
            }
            catch (OpenTK.Windowing.GraphicsLibraryFramework.GLFWException)
            {
                return;
            }

            GL.Clear(
                ClearBufferMask.ColorBufferBit |
                ClearBufferMask.DepthBufferBit);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);

            meshRenderer.Render(
                modelMatrix,
                viewMatrix,
                projectionMatrix);

            skeletonRenderer.Render(
                modelMatrix,
                viewMatrix,
                projectionMatrix);

            GL.Flush();

            glControl.SwapBuffers();
        }

        private void GlControl_Resize(
            object sender,
            EventArgs e)
        {
            if (!glLoaded)
                return;

            glControl.MakeCurrent();

            UpdateProjectionMatrix();

            glControl.Invalidate();
        }

        private void FitSkeletonToView(
    System.Collections.Generic.List<SkeletonLine> lines)
        {
            if (lines == null || lines.Count == 0)
            {
                viewMatrix = Matrix4.Identity;
                return;
            }

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;

            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;

            foreach (var line in lines)
            {
                IncludePoint(line.Start);
                IncludePoint(line.End);
            }

            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;
            float centerZ = (minZ + maxZ) * 0.5f;

            float sizeX = maxX - minX;
            float sizeY = maxY - minY;
            float sizeZ = maxZ - minZ;

            float maxSize =
                Math.Max(
                    sizeX,
                    Math.Max(sizeY, sizeZ));

            if (maxSize < 0.0001f)
                maxSize = 1.0f;

            /*
             * OpenGL's useful visible range with our current
             * projection is approximately -1 .. +1.
             *
             * 1.5 / maxSize leaves some space around the skeleton
             * instead of fitting it exactly against the borders.
             */
            float scale =
                1.5f / maxSize;

            /*
             * First move the skeleton's center to the origin,
             * then scale the whole result.
             */
            viewMatrix =
                Matrix4.CreateTranslation(
                    -centerX,
                    -centerY,
                    -centerZ)
                * Matrix4.CreateScale(scale);

            void IncludePoint(SkeletonPoint point)
            {
                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
                minZ = Math.Min(minZ, point.Z);

                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
                maxZ = Math.Max(maxZ, point.Z);
            }
        }

        private void UpdateProjectionMatrix()
        {
            int width = Math.Max(
                glControl.ClientSize.Width,
                1);

            int height = Math.Max(
                glControl.ClientSize.Height,
                1);

            GL.Viewport(
                0,
                0,
                width,
                height);

            /*
             * Same projection strategy currently used by MainForm.
             *
             * This only compensates for the viewport aspect ratio.
             */
            if (width <= height)
            {
                float k =
                    (float)width / height;

                projectionMatrix =
                    Matrix4.CreateScale(
                        1.0f,
                        k,
                        1.0f);
            }
            else
            {
                float k =
                    (float)height / width;

                projectionMatrix =
                    Matrix4.CreateScale(
                        k,
                        1.0f,
                        1.0f);
            }
        }

        private void PlayPauseButton_Click(
            object sender,
            EventArgs e)
        {
            if (currentAnimation == null)
                return;

            if (isPlaying)
            {
                StopPlayback();
                return;
            }

            /*
             * If we're already at the end, restart from the beginning.
             */
            if (timelineTrackBar.Value >= TimelineResolution)
            {
                timelineTrackBar.Value = 0;
            }

            isPlaying = true;
            lastPlaybackTime = DateTime.UtcNow;

            playPauseButton.Text = "Pause";
            playPauseButton.Invalidate();

            playbackTimer.Start();
        }


        private void StopPlayback()
        {
            isPlaying = false;

            playbackTimer.Stop();

            playPauseButton.Text = "Play";
            playPauseButton.Invalidate();
        }


        private void PlaybackTimer_Tick(
            object sender,
            EventArgs e)
        {
            if (!isPlaying ||
                currentAnimation == null)
            {
                return;
            }

            float duration =
                animationEndTime - animationStartTime;

            if (duration <= 0.000001f)
            {
                StopPlayback();
                return;
            }

            DateTime now =
                DateTime.UtcNow;

            double elapsedSeconds =
                (now - lastPlaybackTime).TotalSeconds;

            lastPlaybackTime = now;

            float currentNormalized =
                timelineTrackBar.Value /
                (float)TimelineResolution;

            float currentTime =
                animationStartTime +
                duration * currentNormalized;

            float newTime =
                currentTime +
                (float)elapsedSeconds *
                playbackSpeed;

            /*
             * End of clip.
             */
            if (newTime >= animationEndTime)
            {
                if (loopCheckBox.Checked)
                {
                    /*
                     * Preserve any overshoot so looping remains smooth.
                     */
                    float relativeTime =
                        newTime - animationStartTime;

                    newTime =
                        animationStartTime +
                        relativeTime % duration;
                }
                else
                {
                    timelineTrackBar.Value =
                        TimelineResolution;

                    StopPlayback();

                    return;
                }
            }

            float normalized =
                (newTime - animationStartTime) /
                duration;

            int sliderValue =
                (int)Math.Round(
                    normalized * TimelineResolution);

            sliderValue =
                Math.Max(
                    0,
                    Math.Min(
                        TimelineResolution,
                        sliderValue));

            timelineTrackBar.Value =
                sliderValue;
        }

        private void UpdateAnimatedMeshes(float time)
        {
            if (currentAnimation == null || !glLoaded)
                return;

            var restWorldMatrices =
                animationPlayer.GetWorldMatrices(null, 0f);

            var animatedWorldMatrices =
                animationPlayer.GetWorldMatrices(currentAnimation, time);

            var cpuSkinner = new CpuSkinner(animationPlayer);

            animatedMeshVertices.Clear();
            animatedMeshVertices.AddRange(originalMeshVertices);

            for (int i = 0; i < renderableMeshes.Count; i++)
            {
                var mesh = renderableMeshes[i];

                if (mesh?.VertexList == null ||
                    mesh.BoneList == null ||
                    mesh.BoneList.Count == 0)
                    continue;

                if (!restWorldMatrices.TryGetValue(
                    mesh.Path, out var restMeshWorld))
                    continue;

                Matrix4x4 inverseRestMeshWorld;

                try
                {
                    inverseRestMeshWorld =
                        AnimationPlayer.InvertMatrix(restMeshWorld);
                }
                catch (InvalidOperationException)
                {
                    continue;
                }

                var skinnedVertices = cpuSkinner.SkinMeshRelative(
                    mesh,
                    restWorldMatrices,
                    animatedWorldMatrices,
                    inverseRestMeshWorld);

                int offset = meshVertexOffsets[i];

                for (int v = 0; v < skinnedVertices.Count; v++)
                {
                    animatedMeshVertices[offset + v] =
                        new Vector3(
                            skinnedVertices[v].X,
                            skinnedVertices[v].Y,
                            skinnedVertices[v].Z);
                }
            }

            glControl.MakeCurrent();

            meshRenderer.UpdateVertexPositions(
                animatedMeshVertices);
        }

        private void TimelineTrackBar_ValueChanged(
            object sender,
            EventArgs e)
        {
            if (!timelineReady ||
                !glLoaded ||
                currentAnimation == null)
            {
                return;
            }

            float duration =
                animationEndTime - animationStartTime;

            if (duration <= 0.000001f)
                return;

            float normalized =
                timelineTrackBar.Value /
                (float)TimelineResolution;

            float time =
                animationStartTime +
                duration * normalized;

            var lines =
                animationPlayer.GetSkeletonLines(
                    currentAnimation,
                    time);

            skeletonRenderer.UpdateSkeleton(lines);
            UpdateAnimatedMeshes(time);

            timeLabel.Text =
                $"{time:0.00} / {animationEndTime:0.00} s";

            UpdateFrameLabel(time);

            glControl.Invalidate();
        }

        private void GlControl_MouseWheel(
            object sender,
            MouseEventArgs e)
        {
            if (!glLoaded)
                return;

            float scale =
                1.0f + e.Delta / 1000.0f;

            /*
             * Prevent an extreme wheel event from flipping
             * the entire view through a negative scale.
             */
            if (scale <= 0.05f)
                scale = 0.05f;

            viewMatrix *=
                Matrix4.CreateScale(scale);

            glControl.Invalidate();
        }

        private void GlControl_MouseDown(
            object sender,
            MouseEventArgs e)
        {
            mouseX = e.X;
            mouseY = e.Y;

            if (e.Button == MouseButtons.Left)
                leftMouseDown = true;

            if (e.Button == MouseButtons.Right)
                rightMouseDown = true;
        }

        private void GlControl_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (!leftMouseDown &&
                !rightMouseDown)
            {
                return;
            }

            float dx =
                mouseX - e.X;

            float dy =
                mouseY - e.Y;

            mouseX = e.X;
            mouseY = e.Y;

            if (leftMouseDown)
            {
                dx *= 0.01f;
                dy *= 0.01f;

                viewMatrix *=
                    Matrix4.CreateRotationX(dy);

                viewMatrix *=
                    Matrix4.CreateRotationY(dx);
            }

            if (rightMouseDown)
            {
                dx *= 0.003f;
                dy *= 0.003f;

                viewMatrix *=
                    Matrix4.CreateTranslation(
                        -dx,
                        dy,
                        0.0f);
            }

            glControl.Invalidate();
        }

        private void GlControl_MouseUp(
            object sender,
            MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
        new AnimationPlayer(rootFrame);        leftMouseDown = false;

            if (e.Button == MouseButtons.Right)
                rightMouseDown = false;
        }

        private void AnimationPreviewForm_FormClosed(
            object sender,
            FormClosedEventArgs e)
        {

            playbackTimer.Stop();
            isPlaying = false;
            if (!glLoaded)
                return;

            /*
             * OpenGL objects belong to this GLControl's context.
             * Make that context current before deleting them.
             */
            glControl.MakeCurrent();

            skeletonRenderer.Dispose();

            meshRenderer.Dispose();

            glLoaded = false;
        }

        private void PlaybackSpeedComboBox_SelectedIndexChanged(
    object sender,
    EventArgs e)
        {
            switch (playbackSpeedComboBox.SelectedIndex)
            {
                case 0:
                    playbackSpeed = 0.25f;
                    break;

                case 1:
                    playbackSpeed = 0.5f;
                    break;

                case 2:
                    playbackSpeed = 1.0f;
                    break;

                case 3:
                    playbackSpeed = 2.0f;
                    break;

                default:
                    playbackSpeed = 1.0f;
                    break;
            }

            /*
             * Reset the time reference while playing.
             * This prevents the first tick after a speed
             * change from including stale elapsed time.
             */
            if (isPlaying)
            {
                lastPlaybackTime =
                    DateTime.UtcNow;
            }
        }

    }
}
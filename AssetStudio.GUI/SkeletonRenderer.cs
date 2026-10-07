using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace AssetStudio.GUI
{
    /// <summary>
    /// Minimal OpenGL renderer used by Animation Preview.
    ///
    /// It renders a skeleton as independent parent -> child lines.
    /// It does not know anything about AnimationClip or ImportedFrame.
    /// </summary>
    internal sealed class SkeletonRenderer : IDisposable
    {
        private int program;
        private int vertexArray;
        private int vertexBuffer;

        private int attributeVertexPosition;
        private int attributeVertexColor;

        private int uniformModelMatrix;
        private int uniformViewMatrix;
        private int uniformProjMatrix;

        private int vertexCount;

        private bool initialized;
        private bool disposed;

        /// <summary>
        /// Creates the OpenGL resources.
        ///
        /// IMPORTANT:
        /// the caller must already have made the correct GLControl
        /// context current before calling this method.
        /// </summary>
        public void Initialize()
        {
            if (initialized)
                return;

            program = GL.CreateProgram();

            int vertexShader = CompileShader(
                ShaderType.VertexShader,
                GetResourceString("vs"));

            int fragmentShader = CompileShader(
                ShaderType.FragmentShader,
                GetResourceString("fsColor"));

            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);

            GL.LinkProgram(program);

            GL.GetProgram(
                program,
                GetProgramParameterName.LinkStatus,
                out int linkStatus);

            if (linkStatus == 0)
            {
                string log = GL.GetProgramInfoLog(program);

                GL.DeleteShader(vertexShader);
                GL.DeleteShader(fragmentShader);
                GL.DeleteProgram(program);
                program = 0;

                throw new InvalidOperationException(
                    "Unable to link skeleton shader program:\n" + log);
            }

            GL.DetachShader(program, vertexShader);
            GL.DetachShader(program, fragmentShader);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            attributeVertexPosition =
                GL.GetAttribLocation(program, "vertexPosition");

            attributeVertexColor =
                GL.GetAttribLocation(program, "vertexColor");

            uniformModelMatrix =
                GL.GetUniformLocation(program, "modelMatrix");

            uniformViewMatrix =
                GL.GetUniformLocation(program, "viewMatrix");

            uniformProjMatrix =
                GL.GetUniformLocation(program, "projMatrix");

            GL.GenVertexArrays(1, out vertexArray);
            GL.GenBuffers(1, out vertexBuffer);

            GL.BindVertexArray(vertexArray);

            GL.BindBuffer(
                BufferTarget.ArrayBuffer,
                vertexBuffer);

            GL.BufferData(
                BufferTarget.ArrayBuffer,
                IntPtr.Zero,
                IntPtr.Zero,
                BufferUsageHint.DynamicDraw);

            GL.VertexAttribPointer(
                attributeVertexPosition,
                3,
                VertexAttribPointerType.Float,
                false,
                3 * sizeof(float),
                0);

            GL.EnableVertexAttribArray(
                attributeVertexPosition);

            /*
             * We deliberately do NOT create a color VBO.
             *
             * The vertex shader expects vertexColor, but OpenGL allows
             * us to disable its vertex array and provide one constant
             * value for every vertex instead.
             */
            GL.DisableVertexAttribArray(
                attributeVertexColor);

            // Light neutral color, visible on both light and dark UI.
            GL.VertexAttrib4(
                attributeVertexColor,
                0.90f,
                0.90f,
                0.90f,
                1.00f);

            GL.BindBuffer(
                BufferTarget.ArrayBuffer,
                0);

            GL.BindVertexArray(0);

            initialized = true;
        }

        /// <summary>
        /// Uploads the skeleton geometry to the GPU.
        ///
        /// Each SkeletonLine becomes exactly two OpenGL vertices:
        ///
        /// Parent -> Child
        /// </summary>
        public void UpdateSkeleton(IReadOnlyList<SkeletonLine> lines)
        {
            EnsureInitialized();

            if (lines == null || lines.Count == 0)
            {
                vertexCount = 0;
                return;
            }

            var vertices =
                new Vector3[lines.Count * 2];

            int vertexIndex = 0;

            for (int i = 0; i < lines.Count; i++)
            {
                SkeletonLine line = lines[i];

                vertices[vertexIndex++] =
                    ToOpenTK(line.Start);

                vertices[vertexIndex++] =
                    ToOpenTK(line.End);
            }

            vertexCount = vertices.Length;

            GL.BindBuffer(
                BufferTarget.ArrayBuffer,
                vertexBuffer);

            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(vertices.Length * 3 * sizeof(float)),
                vertices,
                BufferUsageHint.DynamicDraw);

            GL.BindBuffer(
                BufferTarget.ArrayBuffer,
                0);
        }

        /// <summary>
        /// Draws the currently uploaded skeleton.
        /// </summary>
        public void Render(
            Matrix4 modelMatrix,
            Matrix4 viewMatrix,
            Matrix4 projectionMatrix)
        {
            EnsureInitialized();

            if (vertexCount == 0)
                return;

            GL.UseProgram(program);

            GL.UniformMatrix4(
                uniformModelMatrix,
                false,
                ref modelMatrix);

            GL.UniformMatrix4(
                uniformViewMatrix,
                false,
                ref viewMatrix);

            GL.UniformMatrix4(
                uniformProjMatrix,
                false,
                ref projectionMatrix);

            GL.BindVertexArray(vertexArray);

            /*
             * Every pair of vertices forms one independent line:
             *
             * 0 -> 1
             * 2 -> 3
             * 4 -> 5
             * ...
             */
            GL.DrawArrays(
                PrimitiveType.Lines,
                0,
                vertexCount);

            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }

        private static Vector3 ToOpenTK(
            SkeletonPoint point)
        {
            return new Vector3(
                point.X,
                point.Y,
                point.Z);
        }

        private static int CompileShader(
            ShaderType type,
            string source)
        {
            int shader = GL.CreateShader(type);

            GL.ShaderSource(
                shader,
                source);

            GL.CompileShader(shader);

            GL.GetShader(
                shader,
                ShaderParameter.CompileStatus,
                out int compileStatus);

            if (compileStatus == 0)
            {
                string log =
                    GL.GetShaderInfoLog(shader);

                GL.DeleteShader(shader);

                throw new InvalidOperationException(
                    $"Unable to compile {type}:\n{log}");
            }

            return shader;
        }

        private static string GetResourceString(
            string name)
        {
            object resource =
                Properties.Resources.ResourceManager
                    .GetObject(name);

            if (resource is string text &&
                !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            throw new InvalidOperationException(
                $"OpenGL shader resource '{name}' was not found.");
        }

        private void EnsureInitialized()
        {
            if (!initialized)
            {
                throw new InvalidOperationException(
                    "SkeletonRenderer has not been initialized.");
            }

            if (disposed)
            {
                throw new ObjectDisposedException(
                    nameof(SkeletonRenderer));
            }
        }

        /// <summary>
        /// Releases OpenGL resources.
        ///
        /// IMPORTANT:
        /// the owning GLControl context must be current when Dispose()
        /// is called.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            if (initialized)
            {
                if (vertexBuffer != 0)
                {
                    GL.DeleteBuffer(vertexBuffer);
                    vertexBuffer = 0;
                }

                if (vertexArray != 0)
                {
                    GL.DeleteVertexArray(vertexArray);
                    vertexArray = 0;
                }

                if (program != 0)
                {
                    GL.DeleteProgram(program);
                    program = 0;
                }
            }

            initialized = false;
            disposed = true;
        }
    }
}
﻿using Generate.Content;
using Generate.D2D;
using Generate.D3D;
using Generate.Input;
using Generate.Procedure;
using System;

namespace Generate
{
    class Program
    {
        internal static Renderer Renderer;
        internal static ChunkLoader Chunks;
        private static LoopWindow Window;
        internal static Overlay Overlay;

        internal static bool Close = false;
        internal static int VSync = 1;
        internal static bool DebugMode = false;
        private static uint Frames = 0;

        static void Main(string[] args)
        {
            Log("Seed? ");
            Worker.Master = new Master(Console.ReadLine().ASCIIBytes());

            Constants.Load();
            
            using (Window = new LoopWindow())
            using (Renderer = new Renderer(Window))
            using (Overlay = new Overlay(Renderer.Device, Renderer.AntiAliasedBackBuffer))
            using (Chunks = new ChunkLoader())
            using (Sun.Main = new Sun(Constants.SunSeed))
            using (Skybox.Main = new Skybox(Constants.SkySeed))
            using (var Loop = Window.Loop())
            {
                KeyboardMouse.StartCapture();
                Watch = new System.Diagnostics.Stopwatch();
                Watch.Start();

                while (!Close && Loop.NextFrame())
                {
                    Frame();
                }
            }
        }

        static System.Diagnostics.Stopwatch Watch;
        static float FPS = 0f;

        static void Frame()
        {
            Processor.Process();
            
            Model ToLoad;
            for (int i = 0; i < 2 && Model.ModelsToLoad.TryPop(out ToLoad); i++)
            {
                ToLoad.Load();
            }

            Sun.Main.Tick();

            Renderer.PrepareShadow();
            Chunks.RenderVisible();
            Renderer.EndShadow();

            using (Renderer.PrepareCamera(Constants.Background))
            {
                Chunks.RenderVisible();

                ((CameraShader)Renderer.ActiveShader).DisableLighting();

                Skybox.Main.MoveWorld = Camera.Position;
                Skybox.Main.Render();
                Sun.Main.Render();
            }
            
            Overlay?.Start();
            Overlay?.DrawCrosshair();
            Overlay?.Draw($"Coords ({Camera.Position.X}, {Camera.Position.Y}, {Camera.Position.Z})", 10, 10, 500, 20);
            Overlay?.Draw($"Rotation ({Camera.RotationX}, {Camera.RotationY})", 10, 30, 500, 20);
            Overlay?.Draw($"Frames ({FPS}, VSync {VSync})", 10, 50, 500, 20);
            Overlay?.Draw($"Moved Chunks ({ChunkLoader.MovedX}, {ChunkLoader.MovedZ}) - Size {ChunkLoader.ChunkCountSide}", 10, 70, 500, 20);
            Overlay?.Draw($"F1/2/4 Anti Aliasing Count", 10, 90, 500, 20);
            Overlay?.Draw($"F9/10 Chunks, F11 Fullscreen, ESC Exit", 10, 110, 500, 20);
            Overlay?.Draw($"F8 WASD Space/Shift Movement", 10, 130, 500, 20);
            Overlay?.End();

            Frames++;
            if (Watch.ElapsedMilliseconds >= 1000)
            {
                FPS = (float)Frames / Watch.ElapsedMilliseconds * 1000;
                Watch.Restart();
                Frames = 0;
            }

            Renderer.EndCamera(VSync);
        }

        internal static void LogLine(object In, string From = null)
            => Log(In + "\r\n", From);

        internal static void Log(object In, string From = null)
            => Console.Write($"[{DateTime.Now.ToLongTimeString()}] {From ?? "Main"} - {In}");
    }
}

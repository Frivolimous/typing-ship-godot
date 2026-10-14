using Godot;

namespace TypingShip;

// Dev aid: "-- --screenshot=out.png [--frames=N]" saves the viewport after N frames and quits;
// "--at=100,400,900" saves out_100.png, out_400.png, ... instead. "--speed=N" runs the game N times faster.
public static class Screenshot
{
    public static async void CaptureIfRequested(Node owner)
    {
        string? path = null;
        var frames = new List<int> { 10 };
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith("--screenshot=")) path = arg["--screenshot=".Length..];
            else if (arg.StartsWith("--frames=")) frames = new List<int> { int.Parse(arg["--frames=".Length..]) };
            else if (arg.StartsWith("--at=")) frames = arg["--at=".Length..].Split(',').Select(int.Parse).ToList();
            else if (arg.StartsWith("--speed=")) Engine.TimeScale = double.Parse(arg["--speed=".Length..]);
        }
        if (path == null) return;
        if (Engine.TimeScale > 1) Engine.MaxPhysicsStepsPerFrame = 64;

        var tree = owner.GetTree();
        int frame = 0;
        foreach (var target in frames.Order())
        {
            while (frame < target)
            {
                await owner.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                frame++;
            }
            await owner.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var file = frames.Count == 1 ? path : path.Replace(".png", $"_{target}.png");
            owner.GetViewport().GetTexture().GetImage().SavePng(file);
            GD.Print($"Screenshot saved: {file}");
        }
        tree.Quit();
    }
}

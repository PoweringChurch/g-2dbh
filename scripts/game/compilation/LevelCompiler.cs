using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class LevelCompiler
{
    public readonly static TextureData[] BackgroundTextures =
    {
        new() {TextureName = "None", TexturePath = "none"},
        new() {TextureName = "Snow", TexturePath = "res://data/images/backgrounds/snow-scroller.png"},
        new() {TextureName = "Storm", TexturePath = "res://data/images/backgrounds/storm-scroller.png"},
    };
    public readonly static TextureData[] ProjectileTextures =
    {
        new() {TextureName = "Default", TexturePath = "res://data/images/projectiles/circle.png"},
        new() {TextureName = "Black Icicle", TexturePath = "res://data/images/projectiles/black-icicle.png"},
        new() {TextureName = "Blizzard Snowflake", TexturePath = "res://data/images/projectiles/blizzard-snowflake.png"},
        new() {TextureName = "Crystal Comet", TexturePath = "res://data/images/projectiles/crystal-comet.png"},
        new() {TextureName = "Hail", TexturePath = "res://data/images/projectiles/hail.png"},
        new() {TextureName = "Icicle", TexturePath = "res://data/images/projectiles/icicle.png"},
        new() {TextureName = "Large Snowflake", TexturePath = "res://data/images/projectiles/large-snowflake.png"},
        new() {TextureName = "Snow Mine", TexturePath = "res://data/images/projectiles/snow-mine.png"},
        new() {TextureName = "Snow", TexturePath = "res://data/images/projectiles/snow.png"},
        new() {TextureName = "Snowball", TexturePath = "res://data/images/projectiles/snowball.png"},
        new() {TextureName = "Snowflake", TexturePath = "res://data/images/projectiles/snowflake.png"},
    };
    public readonly static SongData[] SongData =
    {
        new() {SongName = "Freezing Adventure", Author = "Seigamantic", StreamPath = "res://data/music/ice-story/freezing-adventure.mp3"},
        new() {SongName = "A Snowy Night", Author = "Seigamantic", StreamPath = "res://data/music/ice-story/a-snowy-night-2026.mp3"},
        new() {SongName = "Raspberry and Purple", Author = "Seigamantic", StreamPath = "res://data/music/ice-story/raspberry-and-purple.mp3"},
        new() {SongName = "Witching Hour", Author = "Seigamantic", StreamPath = "res://data/music/ice-story/witching-hour.mp3"},
        new() {SongName = "The Final Voyage (MANIA Mix)", Author = "Seigamantic", StreamPath = "res://data/music/ice-story/the-final-voyage.mp3"},
    };
    public static CompiledLevel CompileLevel(LevelData level, Node2D gameRoot)
    {
        CompiledLevel compiled = new();
        // set custom variables
        CustomVariableExpr.Definitions.Clear();
        foreach (var kvp in level.CustomVariables)
            CustomVariableExpr.Definitions[kvp.Key] = ExpressionHandler.Parse(kvp.Value);
        // compile background
        var backgroundroot = new Node2D { Position = PlayingField.Resolutions[level.AspectRatio] / 2 };
        gameRoot.AddChild(backgroundroot);
        for (int i = 0; i < level.BackgroundLayers.Count; i++)
        {
            var layer = level.BackgroundLayers[i];
            var instance = new BackgroundLayerInstance();
            var sprite = new Sprite2D();
            instance.AddChild(sprite);
            instance.Sprite = sprite;
            instance.Layer = layer;
            compiled.BackgroundInstances.Add(instance);
            instance.ApplyLayerParams();
            backgroundroot.AddChild(instance);
        }
        // compile projectiles
        for (int i = 0; i < level.ProjectileModels.Length; i++)
        {
            // create projectile
            var m = level.ProjectileModels[i];
            if (m == null)
                continue;
            CompileProjectile(m);
            compiled.Projectiles[i] = m;
        }
        // compile patterns
        for (int i = 0; i < level.PatternModels.Length; i++)
        {
            var m = level.PatternModels[i];
            if (m == null)
                continue;
            CompilePattern(m);
            compiled.Patterns[m.Id] = m;
        }
        // convert editor references into spatial references
        for (int i = 0; i < level.References.Count; i++)
        {
            var r = level.References[i];
            var sr = EditorToSpatialReference(r);
            compiled.Queued.Add(sr);
        }
        // set duration
        compiled.Duration = level.Duration;
        compiled.AspectRatio = level.AspectRatio;
        Console.Inst.Log($"[Level Compiler] Level compiled");
        return compiled;
    }
    public static void CompileProjectile(ProjectileModel model)
    {
        if (model == null)
			return;
        model.fnx = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionX));
		model.fny = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionY));
        model.fnf = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionF));
        model.RuntimeSpawns = [];
        foreach (var spawn in model.Spawns)
            model.RuntimeSpawns.Add(EditorToSpatialReference(spawn));
    }
    public static void CompilePattern(PatternModel model)
    {
        if (model == null)
			return;
		model.fnx = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionX));
		model.fny = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionY));
		model.fnt = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionT));
		model.fnf = ExpressionHandler.Compile(ExpressionHandler.Parse(model.FunctionF));
    }
    public static SpatialReference EditorToSpatialReference(EditorReference r) => 
        new() 
        {
            SpawnPos = new Vector2(r.SpawnX, r.SpawnY),
            Pos = new Vector2(r.SpawnX, r.SpawnY),
            SpawnF = r.SpawnF,
            F = r.SpawnF,
            T = r.T,
            Type = r.Type,
            Id = r.Id,
            Depth = 0, 
        };
}